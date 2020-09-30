using CodenameGenerator;
using CommandLine;
using HttpSequencer.SequenceItemActions;
using NLog;
using Polly;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using YamlExtensions;

namespace HttpSequencer
{
    partial class Program
    {        
        static Program()
        {
            try
            { 
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;                
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        static int Main(string[] args)
        {
            try
            {
                var parsedOptions = Parser.Default.ParseArguments<Options, int>(args);
                var options = parsedOptions.MapResult((o) => o as Options, (o) => null as Options);
                return new HttpSequencer().RunSequence(options);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            return 1;
        }
    }
    public class HttpSequencer
    {
        private static Generator Generator = new Generator("", Casing.PascalCase);

        static HttpSequencer()
        {
            HttpSequencer.Generator.SetParts(WordBank.Adjectives, WordBank.Nouns);
        }

        public int RunSequence(Options obj)
        {
            var log = LogManager.GetLogger(nameof(Program));

            var state = new RunState
            {
                ProgressLog = MakeProgressLogger(),
                Log = log,
                RunId = MakeRunId(),
                CommandLineOptions = null,
                YamlOptions = null
            };

            try
            { 
                YamlOptions yaml = !string.IsNullOrWhiteSpace(obj.YamlFile)
                    ? YamlLoad.Load<YamlOptions>(obj?.YamlFile)
                    : obj?.YamlDirect;

                state.CommandLineOptions = obj;
                state.YamlOptions = yaml ?? throw new InvalidOperationException(nameof(RunSequence));

                var sequenceItem = yaml.sequence_items.First();
                var breadcrumbs = new Stack<ISequenceItemAction>();

                // Pass in the chain of command to do after
                var waitFor = ProcessSequenceItem(state, null, null, sequenceItem, yaml.sequence_items.Skip(1), breadcrumbs);
                var result = waitFor.Result;

                return result ? 0 : 1;
            }
            catch (Exception e)
            {
                log.Error(e);
                return 1;
            }
            //finally
            //{
            //    if (state.Exceptions.Any())
            //    { 
            //        log.Error("\nThe following exceptions were encountered during the run:");
            //        foreach (var exception in state.Exceptions)
            //            log.Error(exception);
            //    }
            //}
        }

        private static IProgressLog MakeProgressLogger()
        {
            return new DebugProgressLog();//ConsoleProgressLog();
        }

        private static string MakeRunId()
        {
            // Random human readable, padded right with guid chars
            var luckyGuid = $"_{Guid.NewGuid().ToString()}";
            var retval = Generator.Generate().Replace(" ", "");
            var candidate = $"{retval}{new string(luckyGuid.Take(6).ToArray())}";
            return candidate.Trim('-').Trim('_');
        }

        private static async Task<bool> ProcessSequenceItem(RunState state, ISequenceItemAction parent, object model, SequenceItem sequenceItem, IEnumerable<SequenceItem> nextSequenceItems, Stack<ISequenceItemAction> breadcrumbs)
        {
            state.ProgressLog.Started(sequenceItem);

            var retryAfter = new Stack<ISequenceItemAction>();

            var result = false;

            if (sequenceItem.is_model_array) 
            {
                var itemsRunning = new List<Task<bool>>();
                foreach (var item in model as IEnumerable<Task<bool>>)
                    itemsRunning.Add(SequenceItemDispatcher(state, parent, model, sequenceItem, nextSequenceItems, retryAfter, breadcrumbs));

                var allResults = await Task<bool []>.WhenAll(itemsRunning);
                result = allResults.All(a => a);
            }
            else result = await SequenceItemDispatcher(state, parent, model, sequenceItem, nextSequenceItems, retryAfter, breadcrumbs);

            var cancelToken = new CancellationToken();
            var retryFailures = new Stack<ISequenceItemAction>();
            if (retryAfter.Any())
                result = await SequenceItemRetryDispatcher(cancelToken, retryAfter, state, retryFailures, breadcrumbs);
           

            return result || retryAfter.Any();
        }

        private static async Task<bool> SequenceItemRetryDispatcher(CancellationToken cancelToken, IEnumerable<ISequenceItemAction> toRetry, RunState state, Stack<ISequenceItemAction> retryAfterList, Stack<ISequenceItemAction> breadcrumbs)
        {
            foreach (var sequenceItemAction in toRetry) 
            try 
            {
                var retriesLeft = (sequenceItemAction.GetSequenceItem().max_retrys ?? 1) - sequenceItemAction.ActionExecuteCount;
                if (retriesLeft <= 0)
                    return false;

                var policy = Policy.Handle<Exception>().Retry(retriesLeft);

                breadcrumbs.Push(sequenceItemAction);

                var result = await policy.Execute( async () => await sequenceItemAction.Action(cancelToken));
            }
            catch (Exception e)
            {
                if (!CanDealWithException(e, sequenceItemAction, sequenceItemAction.GetSequenceItem(), retryAfterList, breadcrumbs))
                    sequenceItemAction.Fail(e);

            }
            return false;
        }

        // Return the result and what to do next
        private static async Task<bool> SequenceItemDispatcher(RunState state, ISequenceItemAction parent, object model, SequenceItem sequenceItem, IEnumerable<SequenceItem> nextSequenceItems, Stack<ISequenceItemAction> retryAfterList, Stack<ISequenceItemAction> breadcrumbs)
        {
            // try catch in here setup the retry after list as appropriate

            // can retry here and now though too, or both

            ISequenceItemAction sequenceItemAction = null;
            try
            {
                var cancelToken = new CancellationToken();

                if (sequenceItem.send != null)
                    sequenceItemAction = new SequenceItemSend(state, sequenceItem, model, nextSequenceItems);
                else if (sequenceItem.run != null)
                    sequenceItemAction = new SequenceItemRun(state, sequenceItem, model, nextSequenceItems);
                else if (sequenceItem.check != null)
                    sequenceItemAction = new SequenceItemChecker(state, sequenceItem, model, nextSequenceItems);
                
                breadcrumbs.Push(sequenceItemAction);

                if (parent == null) 
                    parent = sequenceItemAction;
                else
                { 
                    parent.Children.Add(sequenceItemAction);
                    sequenceItemAction.Parent = parent;
                }

                var result = await sequenceItemAction.Action(cancelToken);

                if (sequenceItemAction.IsFail)
                    return false;

                if (nextSequenceItems.Any())
                    return await ProcessSequenceItem(state, sequenceItemAction, result, nextSequenceItems.First(), nextSequenceItems.Skip(1), breadcrumbs);
                else
                    return true;
            }
            catch (Exception e)
            {
                if (!CanDealWithException(e, sequenceItemAction, sequenceItem, retryAfterList, breadcrumbs))
                    sequenceItemAction.Fail(e);

                return false;
            }
            finally
            {
                if (sequenceItemAction.IsFail)
                    state.FailedSequenceActions.Add(sequenceItemAction);
            }
        }

        private static bool CanDealWithException(Exception e, ISequenceItemAction sequenceItemAction, SequenceItem sequenceItem, Stack<ISequenceItemAction> retryAfterList, Stack<ISequenceItemAction> breadcrumbs)
        {
            if (e is SequenceItemCheckException) return false;

            if (sequenceItemAction == null) return false;

            if (sequenceItem.is_abort_on_exception) return false;

            if (sequenceItem.max_retrys >= sequenceItemAction.ActionExecuteCount) return false;

            retryAfterList.Push(sequenceItemAction);

            return true;
        }
    }

    public interface IProgressLog
    {
        void Started(SequenceItem successWith);

        void Progress(string message);

        void Trace(string message);

        void Success(SequenceItem successWith);

        void Fail(SequenceItem failWith);
    }

    public class ConsoleProgressLog : IProgressLog
    {
        public void Started(SequenceItem message)
        {
            Console.WriteLine(message);
        }

        public void Progress(string message)
        {
            Console.WriteLine(message);
        }

        public void Trace(string message)
        {
            Debug.WriteLine(message);
        }

        public void Success(SequenceItem successWith)
        {
            Console.WriteLine("Success:" + successWith.command);
        }

        public void Fail(SequenceItem failWith)
        {
            Console.Error.WriteLine("Fail:" + failWith.command);
        }
    }

    public class DebugProgressLog : IProgressLog
    {
        public void Started(SequenceItem message)
        {
            Debug.WriteLine(message);
        }

        public void Progress(string message)
        {
            Debug.WriteLine(message);
        }

        public void Trace(string message)
        {
            Debug.WriteLine("Debug: " +message);
        }

        public void Success(SequenceItem successWith)
        {
            Debug.WriteLine("Success:" + successWith.command);
        }

        public void Fail(SequenceItem failWith)
        {
            Debug.WriteLine("Fail:" + failWith.command);
        }
    }


    public class NLogProgressLog : IProgressLog
    {
        public NLog.Logger Logger { get; }

        public NLogProgressLog()
        {
            Logger = LogManager.GetLogger(nameof(Program));
        }

        public void Started(SequenceItem message)
        {
            Logger.Info(message);
        }

        public void Fail(SequenceItem failWith)
        {
            Logger.Error("Success:" + failWith.command);
        }

        public void Progress(string message)
        {
            Logger.Info(message);
        }

        public void Success(SequenceItem successWith)
        {
            Logger.Info("Success:" + successWith.command);
        }

        public void Trace(string message)
        {
            Logger.Debug(message);
        }
    }
}

using CodenameGenerator;
using CommandLine;
using HttpSequencer.SequenceItemActions;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
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

                object modelResult = null;

                //foreach (var sequenceItem in yaml.sequence_items)
                //    modelResult = ProcessSequenceItem(state, modelResult, sequenceItem);

                foreach (var sequenceItem in yaml.sequence_items.Take(1))
                {
                    // pass in the chain of command to do after
                    modelResult = ProcessSequenceItem(state, modelResult, sequenceItem, yaml.sequence_items.Skip(1));

                }

                if (state.Exceptions.Any())
                    return 1;

                return 0;
            }
            catch (Exception e)
            {
                log.Error(e);
                return 1;
            }
            finally
            {
                if (state.Exceptions.Any())
                { 
                    log.Error("\nThe following exceptions were encountered during the run:");
                    foreach (var exception in state.Exceptions)
                        log.Error(exception);
                }
            }
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

        private static object ProcessSequenceItem(RunState state, object model, SequenceItem sequenceItem)
        {
            state.ProgressLog.Started(sequenceItem);

            var retryAfter = new List<SequenceItem>();

            bool isError = false;
            try
            { 
                if (sequenceItem.is_model_array) return ProcessSequenceItemArray(state, model, sequenceItem);
                else return ProcessSequenceItemSingle(state, model, sequenceItem);
            }
            catch (Exception e)
            {
                var addItem = (e.Message, e);
                state.Exceptions.Add(addItem);
                isError = true;
                state.Log.Error(e, $"Error: {sequenceItem.command} failed: {e.Message}");

                if (sequenceItem.is_abort_on_exception)
                    throw;
            }
            finally 
            {
                if (isError) state.ProgressLog.Fail(sequenceItem);
                else state.ProgressLog.Success(sequenceItem);

            }

            return null;
        }

        private static IEnumerable<object> ProcessSequenceItemArray(RunState state, object model, SequenceItem sequenceItem)
        {
            foreach (var item in (IEnumerable<dynamic>)model)
                yield return ProcessSequenceItemSingle(state, item, sequenceItem);
        }

        private static object ProcessSequenceItemSingle(RunState state, object model, SequenceItem sequenceItem)
        {
            object retval = null;

            if (sequenceItem.send != null)
            {
                retval = new SequenceItemSend(state, sequenceItem)
                    .Action(model)
                    .Result;
            }
            else if (sequenceItem.run != null)
            { 
                retval = new SequenceItemRun(state, sequenceItem)
                    .Action(model)
                    .Result;
            }
            else if (sequenceItem.check != null)
            {
                retval = model;

                var sequenceItemChecker = new SequenceItemChecker(state, sequenceItem);

                var isFail = !(bool)(sequenceItemChecker
                    .Action(model)
                    .Result);

                if (isFail)
                    throw new SequenceItemCheckException(sequenceItem, sequenceItemChecker, model);
            }

            return retval;
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

using CodenameGenerator;
using HttpSequencer.SequenceItemActions;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YamlExtensions;

namespace HttpSequencer
{
    public class HttpSequencer
    {
        private static Generator Generator = new Generator("", Casing.PascalCase);

        static HttpSequencer()
        {
            HttpSequencer.Generator.SetParts(WordBank.Adjectives, WordBank.Nouns);
        }

        public async Task<int> RunSequenceAsync(Options obj)
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
                state.YamlOptions = yaml ?? throw new InvalidOperationException(nameof(RunSequenceAsync));

                var errors = CompileSequence(state.YamlOptions);
                //if (errors != null) { log.Error(errors); return 2; }

                var options = new ProcessSequenceItem.Options 
                {
                    state = state, 
                    parent = null, 
                    model = null, 
                    sequenceItem = yaml.sequence_items.First(), 
                    nextSequenceItems = yaml.sequence_items.Skip(1),
                    breadcrumbs = new Stack<KeyValuePair<string, ISequenceItemAction>>()
                };

                var retryAfter = new Stack<ISequenceItemAction>();
                var itemProcessor = new ProcessSequenceItem(options);
                // Pass in the chain of command to do after
                var processResult = await itemProcessor.ProcessSequenceItemAsync(retryAfter);
                //waitFor.Wait();
                var result = processResult && !retryAfter.Any();

                return result ? 0 : 1;
            }
            catch (Exception e)
            {
                log.Error(e);
                return 2;
            }
        }

        private string CompileSequence(YamlOptions yamlOptions)
        {
            //var templateList = Scriban.Template.Parse(JsonConvert.SerializeObject(yamlOptions));
            //if (templateList.HasErrors) return string.Join(Environment.NewLine, templateList.Messages);
            return null;
        }

        private static IProgressLog MakeProgressLogger()
        {
            return new NLogProgressLog(); //DebugProgressLog
        }

        private static string MakeRunId()
        {
            // Random human readable, padded right with guid chars
            var luckyGuid = $"_{Guid.NewGuid().ToString()}";
            var retval = Generator.Generate().Replace(" ", "");
            var candidate = $"{retval}{new string(luckyGuid.Take(6).ToArray())}";
            return candidate.Trim('-').Trim('_');
        }

        //private static async Task<bool> ProcessSequenceItemAsync(RunState state, ISequenceItemAction parent, object model, SequenceItem sequenceItem, IEnumerable<SequenceItem> nextSequenceItems, Stack<KeyValuePair<string, ISequenceItemAction>> breadcrumbs)
        //{
        //    state.ProgressLog?.Started(sequenceItem);

        //    var retryAfter = new Stack<ISequenceItemAction>();

        //    var result = false;

        //    if (sequenceItem.is_model_array) 
        //    {
        //        var itemsRunning = new List<Task<bool>>();
        //        foreach (var item in model as IEnumerable<object> ?? new object[] { })
        //            itemsRunning.Add(SequenceItemDispatcherAsync(state, parent, item, sequenceItem, nextSequenceItems, retryAfter, breadcrumbs));

        //        var allResults = await Task<bool []>.WhenAll(itemsRunning);
        //        result = allResults.All(a => a);
        //    }
        //    else result = await SequenceItemDispatcherAsync(state, parent, model, sequenceItem, nextSequenceItems, retryAfter, breadcrumbs);

        //    var cancelToken = new CancellationToken();
        //    var retryFailures = new Stack<ISequenceItemAction>();
        //    if (retryAfter.Any())
        //        result = await SequenceItemRetryDispatcherAsync(cancelToken, retryAfter, state, retryFailures, breadcrumbs);
           

        //    return result && !retryAfter.Any();
        //}

        //public static async Task<bool> SequenceItemRetryDispatcherAsync(CancellationToken cancelToken, IEnumerable<ISequenceItemAction> toRetry, RunState state, Stack<ISequenceItemAction> retryAfterList, Stack<KeyValuePair<string, ISequenceItemAction>> breadcrumbs)
        //{
        //    foreach (var sequenceItemAction in toRetry) 
        //    try 
        //    {
        //        var retriesLeft = (sequenceItemAction.GetSequenceItem().max_delayed_retrys ?? 0) - sequenceItemAction.ActionExecuteCount;
        //        if (retriesLeft <= 0)
        //            continue;

        //        var policy = Policy.Handle<Exception>().Retry(retriesLeft);

        //        breadcrumbs.Push(new KeyValuePair<string, ISequenceItemAction>(sequenceItemAction.GetSequenceItem().breadcrumb, sequenceItemAction));

        //        var result = await policy.Execute( async () => await sequenceItemAction.ActionAsync(cancelToken));
        //        return !sequenceItemAction.IsFail;
        //    }
        //    catch (Exception e)
        //    {
        //        if (!CanDealWithException(e, sequenceItemAction, sequenceItemAction.GetSequenceItem(), retryAfterList, breadcrumbs))
        //            sequenceItemAction.Fail(e);

        //    }
        //    return false;
        //}

        //// Return the result and what to do next
        //public static async Task<bool> SequenceItemDispatcherAsync(RunState state, ISequenceItemAction parent, object model, SequenceItem sequenceItem, IEnumerable<SequenceItem> nextSequenceItems, Stack<ISequenceItemAction> retryAfterList, Stack<KeyValuePair<string, ISequenceItemAction>> breadcrumbs)
        //{
        //    // try catch in here setup the retry after list as appropriate

        //    // can retry here and now though too, or both

        //    ISequenceItemAction sequenceItemAction = null;
        //    try
        //    {
        //        var cancelToken = new CancellationToken();

        //        if (sequenceItem.send != null)
        //            sequenceItemAction = new SequenceItemSend(state, sequenceItem, model, nextSequenceItems);
        //        else if (sequenceItem.run != null)
        //            sequenceItemAction = new SequenceItemRun(state, sequenceItem, model, nextSequenceItems);
        //        else if (sequenceItem.har != null)
        //            sequenceItemAction = new SequenceItemHar(state, sequenceItem, model, nextSequenceItems);
        //        else if (sequenceItem.check != null)
        //            sequenceItemAction = new SequenceItemCheck(state, sequenceItem, model, nextSequenceItems);

        //        breadcrumbs.Push(new KeyValuePair<string, ISequenceItemAction>(sequenceItem.breadcrumb, sequenceItemAction));

        //        if (parent == null) 
        //            parent = sequenceItemAction;
        //        else
        //        { 
        //            parent.Children.Add(sequenceItemAction);
        //            sequenceItemAction.Parent = parent;
        //        }

        //        var result = await sequenceItemAction.ActionAsync(cancelToken);

        //        if (sequenceItemAction.IsFail)
        //            return false;

        //        if (nextSequenceItems.Any())
        //            return await ProcessSequenceItemAsync(state, sequenceItemAction, result, nextSequenceItems.First(), nextSequenceItems.Skip(1), breadcrumbs);
        //        else
        //            return true;
        //    }
        //    catch (Exception e)
        //    {
        //        if (!CanDealWithException(e, sequenceItemAction, sequenceItem, retryAfterList, breadcrumbs))
        //            sequenceItemAction.Fail(e);

        //        return false;
        //    }
        //    finally
        //    {
        //        if (sequenceItemAction.IsFail && CanRetry(sequenceItem, sequenceItemAction))
        //            retryAfterList.Push(sequenceItemAction);
        //        //state.FailedSequenceActions.Add(sequenceItemAction);
        //    }
        //}

        //private static bool CanDealWithException(Exception e, ISequenceItemAction sequenceItemAction, SequenceItem sequenceItem, Stack<ISequenceItemAction> retryAfterList, Stack<KeyValuePair<string, ISequenceItemAction>> breadcrumbs)
        //{
        //    if (e is SequenceItemCheckException) return false;

        //    if (sequenceItemAction == null) return false;

        //    if (sequenceItem.is_abort_on_exception) return false;

        //    if (!CanRetry(sequenceItem, sequenceItemAction)) return false;

        //    retryAfterList.Push(sequenceItemAction);

        //    return true;
        //}

        //private static bool CanRetry(SequenceItem sequenceItem, ISequenceItemAction sequenceItemAction)
        //{
        //   return !(sequenceItem.max_delayed_retrys < sequenceItemAction.ActionExecuteCount);
        //}
    }
}

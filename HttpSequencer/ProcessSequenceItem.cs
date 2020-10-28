using HttpSequencer.SequenceItemActions;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace HttpSequencer
{
    public class ProcessSequenceItem
    {
        private readonly Options options;

        public class Options
        { 
            public RunState state;
            public ISequenceItemAction parent;
            public object model;
            public SequenceItem sequenceItem;
            public IEnumerable<SequenceItem> nextSequenceItems;
            public Stack<KeyValuePair<string, ISequenceItemAction>> breadcrumbs;

        }

        public ProcessSequenceItem(Options options) { this.options = options; }

        public async Task<bool> ProcessSequenceItemAsync(Stack<ISequenceItemAction> retryAfter)
        {
            options.state.ProgressLog?.Started(options.sequenceItem);

            var ourRetrys = new Stack<ISequenceItemAction>();

            var result = new SequenceItemDispatcherResult();

            if (options.sequenceItem.is_model_array)
            {
                var itemsRunning = new List<Task<SequenceItemDispatcherResult>>();
                foreach (var item in options.model as IEnumerable<object> ?? new object[] { })
                    itemsRunning.Add(new ProcessSequenceItem(new ProcessSequenceItem.Options
                    { 
                        state = options.state, 
                        parent = options.parent, 
                        model = item, 
                        sequenceItem = options.sequenceItem, 
                        nextSequenceItems = options.nextSequenceItems, 
                        breadcrumbs = options.breadcrumbs
                    }).SequenceItemDispatcherAsync(ourRetrys));

                var allResults = await Task<bool[]>.WhenAll(itemsRunning);
                result.IsSuccess = allResults.All(a => a.IsSuccess);
                result.Model = allResults.Select(s => s.Model).ToArray();
            }
            else result = await SequenceItemDispatcherAsync(ourRetrys);

            var cancelToken = new CancellationToken();
            if (ourRetrys.Any())
                result = await SequenceItemRetryDispatcherAsync(cancelToken, false, ourRetrys, options.state, retryAfter, options.breadcrumbs);

            return result.IsSuccess;
        }

        public async Task<SequenceItemDispatcherResult> SequenceItemRetryDispatcherAsync(CancellationToken cancelToken, bool isInstantRetry, IEnumerable<ISequenceItemAction> toRetry, RunState state, Stack<ISequenceItemAction> retryAfterList, Stack<KeyValuePair<string, ISequenceItemAction>> breadcrumbs)
        {
            var allResults = new List<SequenceItemDispatcherResult>();
            foreach (var sequenceItemAction in toRetry)
                try
                {
                    var retriesLeft = isInstantRetry
                        ? (sequenceItemAction.GetSequenceItem().max_instant_retrys ?? 0) - (sequenceItemAction.ActionExecuteCount - 1)
                        : (sequenceItemAction.GetSequenceItem().max_instant_retrys ?? 0 + sequenceItemAction.GetSequenceItem().max_delayed_retrys ?? 0) - (sequenceItemAction.ActionExecuteCount - 1);

                    if (retriesLeft <= 0)
                    {
                        sequenceItemAction.Fail();
                        allResults.Add(new SequenceItemDispatcherResult { IsSuccess = false, Model = null });
                        continue;
                    }

                    var policy = Policy.Handle<Exception>().Retry(retriesLeft);

                    breadcrumbs.Push(new KeyValuePair<string, ISequenceItemAction>(sequenceItemAction.GetSequenceItem().breadcrumb, sequenceItemAction));

                    var result = await policy.Execute(async () => await sequenceItemAction.ActionAsync(cancelToken));

                    allResults.Add(new SequenceItemDispatcherResult { IsSuccess = !sequenceItemAction.IsFail, Model = result });
                }
                catch (Exception e)
                {
                    if (!CanDealWithException(e, isInstantRetry, sequenceItemAction, sequenceItemAction.GetSequenceItem(), /*retryAfterList, */breadcrumbs))
                        sequenceItemAction.Fail(e);

                }
                finally
                {
                    if (sequenceItemAction.IsFail)
                        retryAfterList.Push(sequenceItemAction);
                }

            return new SequenceItemDispatcherResult { IsSuccess = allResults.All(a => a.IsSuccess), Model = allResults.Select(s=>s.Model).ToArray()};
        }

        public class SequenceItemDispatcherResult
        {
            public bool IsSuccess { get; set; }
            public object Model { get; set; }
        }

        public async Task<SequenceItemDispatcherResult> SequenceItemDispatcherAsync(Stack<ISequenceItemAction> retryAfter)
        {
            ISequenceItemAction sequenceItemAction = null;
            try
            {
                var cancelToken = new CancellationToken();

                if (options.sequenceItem.send != null)
                    sequenceItemAction = new SequenceItemSend(options.state, options.sequenceItem, options.model, options.nextSequenceItems);
                else if (options.sequenceItem.run != null)
                    sequenceItemAction = new SequenceItemRun(options.state, options.sequenceItem, options.model, options.nextSequenceItems);
                else if (options.sequenceItem.har != null)
                    sequenceItemAction = new SequenceItemHar(options.state, options.sequenceItem, options.model, options.nextSequenceItems);
                else if (options.sequenceItem.check != null)
                    sequenceItemAction = new SequenceItemCheck(options.state, options.sequenceItem, options.model, options.nextSequenceItems);

                options.breadcrumbs.Push(new KeyValuePair<string, ISequenceItemAction>(options.sequenceItem.breadcrumb, sequenceItemAction));

                if (options.parent == null)
                    options.parent = sequenceItemAction;
                else
                {
                    options.parent.Children.Add(sequenceItemAction);
                    sequenceItemAction.Parent = options.parent;
                }

                var result = await sequenceItemAction.ActionAsync(cancelToken);

                if (sequenceItemAction.IsFail)
                    return new SequenceItemDispatcherResult{ IsSuccess = false, Model = result };

                if (options.nextSequenceItems?.Any() ?? false)
                {
                    var nextOptions = new Options 
                    { 
                        state = options.state, 
                        parent = sequenceItemAction, 
                        model = result, 
                        sequenceItem = options.nextSequenceItems.First(), 
                        nextSequenceItems = options.nextSequenceItems.Skip(1), 
                        breadcrumbs = new Stack<KeyValuePair<string, ISequenceItemAction>>(options.breadcrumbs)
                    };
                    return new SequenceItemDispatcherResult
                        { IsSuccess = await new ProcessSequenceItem(nextOptions).ProcessSequenceItemAsync(retryAfter),
                          Model = result };
                }

                return new SequenceItemDispatcherResult { IsSuccess = true, Model = result };
            }
            catch (Exception e)
            {
                if (sequenceItemAction != null
                && !CanDealWithException(e, true, sequenceItemAction, options.sequenceItem, options.breadcrumbs))
                    sequenceItemAction.Fail(e);

                return new SequenceItemDispatcherResult { IsSuccess = !sequenceItemAction.IsFail };
            }
            finally
            {
                if (sequenceItemAction?.IsFail ?? true)
                    retryAfter.Push(sequenceItemAction);
            }
        }

        private static bool CanDealWithException(Exception e, bool isInstantRetry, ISequenceItemAction sequenceItemAction, SequenceItem sequenceItem, /*Stack<ISequenceItemAction> retryAfterList, */Stack<KeyValuePair<string, ISequenceItemAction>> breadcrumbs)
        {
            if (e is SequenceItemCheckException) return false;

            if (sequenceItemAction == null) return false;

            if (sequenceItem.is_abort_on_exception) return false;

            if (!CanRetry(isInstantRetry, sequenceItem, sequenceItemAction)) return false;

            //retryAfterList.Push(sequenceItemAction);

            return true;
        }

        private static bool CanRetry(bool isInstantRetry, SequenceItem sequenceItem, ISequenceItemAction sequenceItemAction)
        {
            return isInstantRetry
                ? !((sequenceItem.max_instant_retrys ?? 0) < (sequenceItemAction.ActionExecuteCount))
                : !((sequenceItem.max_delayed_retrys ?? 0 + sequenceItem.max_instant_retrys ?? 0) < (sequenceItemAction.ActionExecuteCount));
        }
    }
}

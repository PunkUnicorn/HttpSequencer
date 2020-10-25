using HttpSequencer.SequenceItemActions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using static HttpSequencer.SequenceItemActions.SequenceItemStatic;

namespace HttpSequencer
{
    public class SequenceItemCheckException : Exception
    {
        public SequenceItemCheckException(SequenceItem check, SequenceItemCheck checker, dynamic model, Stack<ISequenceItemAction> breadcrumbs)
            : base($"{check.command} - Failed check '{check.check.pass_template}' with model:\n{JsonConvert.SerializeObject(model, Formatting.Indented)}")
        {
            SequenceItem = check;
            Checker = checker;
            Model = model;
            Breadcrumbs = breadcrumbs;
        }

        public SequenceItem SequenceItem { get; }
        public SequenceItemCheck Checker { get; }
        public dynamic Model { get; }
        public Stack<ISequenceItemAction> Breadcrumbs { get; }
    }

    public class SequenceItemCheck : SequenceItemAbstract, ISequenceItemAction
    {
        public SequenceItemCheck(RunState state, SequenceItem sequenceItem, object model, IEnumerable<SequenceItem> nextSequenceItems)
            : base(state, sequenceItem, model, nextSequenceItems) { }

        public IEnumerable<string> Compile(SequenceItem sequenceItem)
        {
            return new string[] { };
        }

        public async Task<object> ActionAsync(CancellationToken cancelToken)
        {
            return FailableRun<object>(this, delegate { 
                ++this.ActionExecuteCount;

                IsFail = true;

                if (this.sequenceItem.check == null)
                    throw new NullReferenceException($"{nameof(this.sequenceItem)}.{nameof(this.sequenceItem.check)} missing");

                var scribanModel = new
                {
                    run_id = this.state.RunId,
                    command_args = this.state.CommandLineOptions,
                    this.model,
                    sequence_item = this.sequenceItem
                };

                IsFail = !this.sequenceItem.check.IsPass(scribanModel);

                if (IsFail) Fail();

                return this.model;
            });
        }
    }
}
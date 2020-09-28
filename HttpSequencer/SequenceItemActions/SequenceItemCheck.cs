using HttpSequencer.SequenceItemActions;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace HttpSequencer
{
    public class SequenceItemCheckException : Exception
    {
        public SequenceItemCheckException(SequenceItem check, SequenceItemChecker checker, dynamic model)
            : base($"{check.command} - Failed check '{check.check.pass_template}' with model:\n{JsonConvert.SerializeObject(model, Formatting.Indented)}")
        {
            SequenceItem = check;
            Checker = checker;
            Model = model;
        }

        public SequenceItem SequenceItem { get; }
        public SequenceItemChecker Checker { get; }
        public dynamic Model { get; }
    }

    public class SequenceItemChecker : ISequenceItemAction
    {

        private RunState state;
        private SequenceItem sequenceItem;

        public SequenceItemChecker(RunState state, SequenceItem sequenceItem)
        {
            this.state = state;
            this.sequenceItem = sequenceItem;
        }

        //public bool IsFail { get; private set; } = true;

        public async Task<object> Action(object model)
        {
            if (this.sequenceItem.check == null)
                return false;

            var scribanModel = new
            {
                run_id = this.state.RunId,
                command_args = this.state.CommandLineOptions,
                previous_response = model,
                sequence_item = this.sequenceItem
            };

            return this.sequenceItem.check.IsPass(scribanModel);
        }
    }
}
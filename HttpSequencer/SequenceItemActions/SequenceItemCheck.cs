﻿using HttpSequencer.SequenceItemActions;
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
        public SequenceItemCheckException(SequenceItem check, SequenceItemChecker checker, dynamic model, Stack<ISequenceItemAction> breadcrumbs)
            : base($"{check.command} - Failed check '{check.check.pass_template}' with model:\n{JsonConvert.SerializeObject(model, Formatting.Indented)}")
        {
            SequenceItem = check;
            Checker = checker;
            Model = model;
            Breadcrumbs = breadcrumbs;
        }

        public SequenceItem SequenceItem { get; }
        public SequenceItemChecker Checker { get; }
        public dynamic Model { get; }
        public Stack<ISequenceItemAction> Breadcrumbs { get; }
    }

    public class SequenceItemChecker : ISequenceItemAction
    {
        private readonly object model;
        private readonly IEnumerable<SequenceItem> nextSequenceItems;
        private readonly RunState state;
        private readonly SequenceItem sequenceItem;

        public SequenceItemChecker(RunState state, SequenceItem sequenceItem, object model, IEnumerable<SequenceItem> nextSequenceItems)
        {
            this.state = state;
            this.sequenceItem = sequenceItem;
            this.model = Clone(model);
            this.nextSequenceItems = nextSequenceItems;
            Children = new List<ISequenceItemAction>();
        }

        public ISequenceItemAction Create(RunState state, SequenceItem sequenceItem, object model, IEnumerable<SequenceItem> nextSequenceItems)
        {
            return new SequenceItemChecker(state, sequenceItem, model, nextSequenceItems);
        }

        public int ActionExecuteCount { get; set; }

        public ISequenceItemAction Parent { get; set; }

        public List<ISequenceItemAction> Children { get; }

        public SequenceItem GetSequenceItem() => this.sequenceItem;

        public dynamic GetModel() => this.model;

        public ISequenceItemAction Fail(Exception e=null)
        {
            IsFail = true;
            Exception = e ?? Exception;
            return this;
        }

        public bool IsFail { get; set; } 

        public Exception Exception { get; set; }

        public async Task<object> Action(CancellationToken cancelToken)
        {
            return FailableRun<object>(this, delegate { 
                ++this.ActionExecuteCount;

                IsFail = true;

                if (this.sequenceItem.check == null)
                    return this.model;

                var scribanModel = new
                {
                    run_id = this.state.RunId,
                    command_args = this.state.CommandLineOptions,
                    previous_response = this.model,
                    sequence_item = this.sequenceItem
                };

                IsFail = !this.sequenceItem.check.IsPass(scribanModel);

                if (IsFail) Fail();

                return this.model;
            });
        }
    }
}
using HttpSequencer.SequenceItemActions;
using System;
using System.Collections.Generic;
using static HttpSequencer.SequenceItemActions.SequenceItemStatic;

namespace HttpSequencer
{
    public abstract class SequenceItemAbstract
    {
        protected readonly object model;
        protected readonly IEnumerable<SequenceItem> nextSequenceItems;
        protected readonly RunState state;
        protected readonly SequenceItem sequenceItem;

        public SequenceItemAbstract(RunState state, SequenceItem sequenceItem, object model, IEnumerable<SequenceItem> nextSequenceItems)
        {
            this.state = state;
            this.sequenceItem = sequenceItem;
            this.model = Clone(model);
            this.nextSequenceItems = nextSequenceItems;
            Children = new List<ISequenceItemAction>();
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
            return this as ISequenceItemAction;
        }

        public bool IsFail { get; set; } 

        public Exception Exception { get; set; }

        private Scriban.Template BreadcrumbTemplate => Scriban.Template.Parse(this.sequenceItem.breadcrumb);

        public string Breadcrumb { get { return BreadcrumbTemplate.Render(this.model); } }
    }
}
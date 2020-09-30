using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HttpSequencer.SequenceItemActions
{
    public interface ISequenceItemAction
    {
        ISequenceItemAction Create(RunState state, SequenceItem sequenceItem, object model, IEnumerable<SequenceItem> nextSequenceItems);

        Task<object> Action(CancellationToken cancelToken);

        ISequenceItemAction Parent { get; set; }

        List<ISequenceItemAction> Children { get; }

        int ActionExecuteCount { get; set; }

        bool IsFail { get; set; }

        Exception Exception { get; set; }

        SequenceItem GetSequenceItem();

        dynamic GetModel();

        ISequenceItemAction Fail(Exception e=null);
    }
}
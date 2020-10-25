using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HttpSequencer.SequenceItemActions
{
    public interface ISequenceItemAction
    {
        IEnumerable<string> Compile(SequenceItem sequenceItem);

        Task<object> ActionAsync(CancellationToken cancelToken);

        ISequenceItemAction Parent { get; set; }

        List<ISequenceItemAction> Children { get; }

        int ActionExecuteCount { get; set; }

        bool IsFail { get; set; }

        Exception Exception { get; set; }

        SequenceItem GetSequenceItem();

        dynamic GetModel();

        ISequenceItemAction Fail(Exception e=null);

        string Breadcrumb { get; }
    }
}
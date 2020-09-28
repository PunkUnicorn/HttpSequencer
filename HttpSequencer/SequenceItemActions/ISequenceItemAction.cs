using System.Threading.Tasks;

namespace HttpSequencer.SequenceItemActions
{
    public interface ISequenceItemAction
    {
        //bool IsFail { get; }
        Task<object> Action(object model);
    }
}
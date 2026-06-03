using MediatR;

namespace Platform.Application.Messaging
{
    public interface IHasPreCommitEvent
    {
        List<INotification> PreCommitEvents { get; }
    }
}

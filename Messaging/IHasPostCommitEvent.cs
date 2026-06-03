using MediatR;

namespace Platform.Application.Messaging
{
    public interface IHasPostCommitEvent
    {
        List<INotification> PostCommitEvents { get; }
    }
}

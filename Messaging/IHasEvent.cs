using MediatR;

namespace Platform.Application.Messaging
{
    public interface IHasEvent
    {
        List<INotification> Events { get; }
    }
}

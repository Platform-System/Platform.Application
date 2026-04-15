using MediatR;
using Platform.SharedKernel.Responses;

namespace Platform.Application.Messaging
{
    public interface ICommand : IRequest<Result<Unit>>
    {
    }
    public interface ICommand<TResponse> : IRequest<Result<TResponse>>
    {
    }
    public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result<Unit>>
        where TCommand : ICommand
    {
    }
    public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
        where TCommand : ICommand<TResponse>
    {
    }
}

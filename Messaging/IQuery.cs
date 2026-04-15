using MediatR;
using Platform.SharedKernel.Responses;

namespace Platform.Application.Messaging
{
    public interface IQuery<TResponse> : IRequest<Result<TResponse>>
    {
    }
    public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
        where TQuery : IQuery<TResponse>
    {
    }
}

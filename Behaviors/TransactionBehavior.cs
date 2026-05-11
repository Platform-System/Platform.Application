using Platform.Application.Abstractions.Data;
using Platform.Application.Messaging;
using MediatR;

namespace Platform.Application.Behaviors
{
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public TransactionBehavior(IUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            // Nếu là Query (lấy dữ liệu) hoặc đã có Transaction rồi thì bỏ qua, không mở thêm Transaction
            if (request is not ICommand && !request.GetType().GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)) || _unitOfWork.HasActiveTransaction)
            {
                return await next();
            }

            await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next();

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                if (request is IHasEvent hasEvent)
                {
                    try
                    {
                        foreach (var @event in hasEvent.Events)
                        {
                            await _mediator.Publish(@event, cancellationToken);
                        }
                        hasEvent.Events.Clear();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"--- Warning: Event/Redis failed: {ex.Message} ---");
                    }
                }

                return response;
            }
            catch (Exception)
            {
                if (_unitOfWork.HasActiveTransaction)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }
                throw; 
            }
        }
    }
}

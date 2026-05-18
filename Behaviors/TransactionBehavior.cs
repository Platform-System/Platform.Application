using Platform.Application.Abstractions.Data;
using Platform.Application.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using Platform.BuildingBlocks.Responses;
 
namespace Platform.Application.Behaviors
{
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;
        private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

        public TransactionBehavior(
            IUnitOfWork unitOfWork,
            IMediator mediator,
            ILogger<TransactionBehavior<TRequest, TResponse>> logger)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
            _logger = logger;
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

                if (!ShouldCommitResponse(response))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return response;
                }

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
                        _logger.LogWarning(ex, "Post-commit event publishing failed for {RequestType}.", typeof(TRequest).Name);
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

        private static bool ShouldCommitResponse(TResponse response)
        {
            if (response is null)
                return true;

            var responseType = response.GetType();
            if (!responseType.IsGenericType || responseType.GetGenericTypeDefinition() != typeof(Result<>))
                return true;

            var isSuccessProperty = responseType.GetProperty(nameof(Result<Unit>.IsSuccess));
            var isSuccessValue = isSuccessProperty?.GetValue(response);
            return isSuccessValue is bool isSuccess ? isSuccess : true;
        }
    }
}

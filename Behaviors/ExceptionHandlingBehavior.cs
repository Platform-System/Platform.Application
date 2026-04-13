using MediatR;
using Microsoft.Extensions.Logging;
using Platform.Domain.Common;
using System.Collections.Concurrent;
using System.Reflection;

namespace Platform.Application.Behaviors
{
    public class ExceptionHandlingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> _failureMethods = new();
        private readonly ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> _logger;
        
        public ExceptionHandlingBehavior(ILogger<ExceptionHandlingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                return await next();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception for {Request} {@Request}", typeof(TRequest).Name, request);

                var domainError = new Error("System.Exception", "Internal server error");
                var responseType = typeof(TResponse);

                if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var valueType = responseType.GetGenericArguments()[0];
                    var resultType = typeof(Result<>).MakeGenericType(valueType);
                    
                    var failureMethod = _failureMethods.GetOrAdd(resultType, type => 
                        type.GetMethod(nameof(Result<object>.Failure), BindingFlags.Public | BindingFlags.Static)!);

                    var result = failureMethod.Invoke(null, new object[] { domainError });
                    return (TResponse)result!;
                }

                if (responseType == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure(domainError);
                }

                throw;
            }
        }
    }
}

using FluentValidation;
using MediatR;
using Platform.SharedKernel.Responses;
using System.Collections.Concurrent;
using System.Reflection;

namespace Platform.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> 
        where TRequest : IRequest<TResponse>
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> _failureMethods = new();
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (!_validators.Any())
                return await next();

            var context = new ValidationContext<TRequest>(request);

            var validationFailures = _validators
                .Select(v => v.Validate(context))
                .SelectMany(r => r.Errors)
                .Where(e => e != null)
                .ToList();

            if (validationFailures.Count != 0)
            {
                var responseType = typeof(TResponse);
                var errorMessages = validationFailures.Select(f => f.ErrorMessage).ToArray();

                if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var innerType = responseType.GetGenericArguments()[0];
                    var resultType = typeof(Result<>).MakeGenericType(innerType);
                    
                    var failureMethod = _failureMethods.GetOrAdd(resultType, type => 
                        type.GetMethod(nameof(Result<object>.Failure))!);

                    // Truyền toàn bộ danh sách lỗi vào SharedKernel.Result
                    var result = failureMethod.Invoke(null, new object[] { errorMessages });
                    return (TResponse)result!;
                }

                throw new Exception("TResponse must be Result<T>");
            }

            return await next();
        }
    }
}

using Platform.Application.Abstractions.Caching;
using MediatR;

namespace Platform.Application.Behaviors
{
    public class CacheBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IRedisService _redisService;
        public CacheBehavior(IRedisService redisService)
        {
            _redisService = redisService;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is not ICacheable cacheable)
                return await next();

            TResponse response;

            try
            {
                var cache = await _redisService.GetAsync<TResponse>(cacheable.CacheKey);
                if (cache is not null)
                    return cache;
            }
            catch
            {
                // Redis lỗi → bỏ qua
            }

            response = await next();

            try
            {
                await _redisService.SetAsync(cacheable.CacheKey, response, cacheable.Expiration);
            }
            catch
            {
                // Redis lỗi → bỏ qua luôn
            }

            return response;
        }
    }
}

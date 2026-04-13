namespace Platform.Application.Abstractions.Caching
{
    public interface ICacheable
    {
        string CacheKey { get; }
        TimeSpan? Expiration { get; }
    }
}

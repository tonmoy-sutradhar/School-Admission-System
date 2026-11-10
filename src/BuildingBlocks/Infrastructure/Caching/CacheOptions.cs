namespace BuildingBlocks.Infrastructure.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";
    public bool UseRedis { get; set; }
    public string RedisConnection { get; set; } = "localhost:6379";
    public int DefaultExpirationMinutes { get; set; } = 30;
}
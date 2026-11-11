using System.Text.Json;
using BuildingBlocks.Application.Abstractions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Caching;

public sealed class DistributedCacheService(
    IDistributedCache cache,
    IOptions<CacheOptions> options) : ICacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly CacheOptions _options = options.Value;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await cache.GetAsync(key, cancellationToken);
        if (bytes is null) return default;
        return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
                ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes)
        };
        await cache.SetAsync(key, bytes, cacheOptions, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(key, cancellationToken);

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        // IDistributedCache does not support prefix scans. Module-level keys should be explicit.
        // For Redis SCAN-based invalidation, swap in a StackExchange.Redis implementation.
        return Task.CompletedTask;
    }
}
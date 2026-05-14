namespace OffceOs.Infrastructure.Common.Caching;

public static class DistributedCacheJsonExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<T?> GetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        CancellationToken ct = default)
    {
        var json = await cache.GetStringAsync(key, ct);
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public static Task SetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        TimeSpan ttl,
        CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        return cache.SetStringAsync(
            key,
            json,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            ct);
    }
}

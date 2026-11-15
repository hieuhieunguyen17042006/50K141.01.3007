using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using THomeApp.Models;

namespace THomeApp.Services;

public sealed class OrderPreparationStore(IDistributedCache cache)
{
    public const string SessionScopeKey = "SalesPreparationScope";
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(60);
    private static string Key(string owner, string scope, string id) => $"THOME:prepared:{owner}:{scope}:{id}";

    public Task StoreAsync(string scope, PreparedOrder order) => cache.SetStringAsync(Key(order.Owner, scope, order.Id),
        JsonSerializer.Serialize(order), new DistributedCacheEntryOptions { AbsoluteExpiration = order.ExpiresAt });

    public async Task<PreparedOrder?> GetAsync(string owner, string? scope, string? id)
    {
        if (scope == null || !Guid.TryParse(id, out var guid)) return null;
        var json = await cache.GetStringAsync(Key(owner, scope, guid.ToString("N")));
        if (json == null) return null;
        var order = JsonSerializer.Deserialize<PreparedOrder>(json);
        return order?.Owner == owner && order.ExpiresAt > DateTimeOffset.UtcNow ? order : null;
    }
}

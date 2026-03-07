# CachedOrderRepository

Decorator do `IOrderRepository`. Adiciona cache transparentemente sem que o Domain ou Application saibam.

```csharp
namespace Infra.Cache;

public class CachedOrderRepository : IOrderRepository
{
    private readonly IOrderRepository _inner;
    private readonly ICacheService _cache;
    private static readonly TimeSpan Expiration = TimeSpan.FromMinutes(5);

    public CachedOrderRepository(IOrderRepository inner, ICacheService cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var key = $"order:{id}";
        var cached = await _cache.GetAsync<Order>(key, ct);

        if (cached is not null)
            return cached;

        var order = await _inner.GetByIdAsync(id, ct);

        if (order is not null)
            await _cache.SetAsync(key, order, Expiration, ct);

        return order;
    }

    public Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default) =>
        _inner.GetByCustomerIdAsync(customerId, ct);

    public async Task AddAsync(Order order, CancellationToken ct = default)
    {
        await _inner.AddAsync(order, ct);
        await _cache.RemoveAsync($"order:{order.Id}", ct);
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        await _inner.UpdateAsync(order, ct);
        await _cache.RemoveAsync($"order:{order.Id}", ct);
    }
}
```

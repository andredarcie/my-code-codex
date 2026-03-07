# ICacheService

Interface de cache. A Application define o contrato, a Infra implementa com Redis ou MemoryCache.

```csharp
namespace Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}
```

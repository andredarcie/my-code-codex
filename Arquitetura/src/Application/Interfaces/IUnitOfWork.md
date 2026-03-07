# IUnitOfWork

Interface que garante atomicidade entre múltiplos repositórios. Um único `CommitAsync` persiste tudo ou nada.

```csharp
namespace Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> CommitAsync(CancellationToken ct = default);
}
```

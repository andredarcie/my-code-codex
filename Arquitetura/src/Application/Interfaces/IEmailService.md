# IEmailService

Interface de serviço externo. A Application define o contrato, a Infra implementa com o provedor real.

```csharp
namespace Application.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
```

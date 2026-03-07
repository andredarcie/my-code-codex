# IEventPublisher

Interface para publicação de Integration Events. A implementação (Kafka, RabbitMQ, etc.) fica na Infra.

```csharp
namespace Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default);
}
```

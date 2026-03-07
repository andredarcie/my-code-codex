# OrderCreatedIntegrationEvent

Integration Event: traduz o Domain Event para um evento publicado externamente para outros serviços.

```csharp
namespace Application.Events;

public record OrderCreatedIntegrationEvent(Guid OrderId, string CustomerId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
```

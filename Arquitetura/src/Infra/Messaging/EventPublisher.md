# EventPublisher

Implementação do `IEventPublisher` usando Kafka via `Confluent.Kafka`.

```csharp
namespace Infra.Messaging;

public class EventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly string _topic;

    public EventPublisher(IProducer<string, string> producer, string topic)
    {
        _producer = producer;
        _topic = topic;
    }

    public async Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(integrationEvent);
        var message = new Message<string, string>
        {
            Key = typeof(T).Name,
            Value = payload
        };

        await _producer.ProduceAsync(_topic, message, ct);
    }
}
```

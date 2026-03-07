namespace Application.Events;

// Integration Event: traduz o Domain Event para um evento publicado externamente
public record OrderCreatedIntegrationEvent(Guid OrderId, string CustomerId)
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

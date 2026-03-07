namespace Domain.Events;

public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}

public sealed class OrderCreatedEvent : IDomainEvent
{
    public Guid OrderId { get; }
    public string CustomerId { get; }
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public OrderCreatedEvent(Guid orderId, string customerId)
    {
        OrderId = orderId;
        CustomerId = customerId;
    }
}

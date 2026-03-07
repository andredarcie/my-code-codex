# Order

Agregado raiz. Contém as invariantes de negócio do pedido.

```csharp
namespace Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public string CustomerId { get; private set; }
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    private Order() { }

    // Invariante: um pedido precisa de cliente e ao menos um item
    public static Order Create(string customerId, List<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new DomainException("Cliente é obrigatório.");

        if (items == null || items.Count == 0)
            throw new DomainException("Pedido deve ter ao menos um item.");

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending
        };

        foreach (var item in items)
            order._items.Add(item);

        order.Total = order._items.Sum(i => i.Price * i.Quantity);
        order._domainEvents.Add(new OrderCreatedEvent(order.Id, customerId));

        return order;
    }

    public void Approve()
    {
        if (Status != OrderStatus.Pending)
            throw new DomainException("Apenas pedidos pendentes podem ser aprovados.");

        Status = OrderStatus.Approved;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

# OrderFactory

Factory: centraliza a criação de Agregados complexos, mantendo o código do caso de uso limpo.

```csharp
namespace Domain.Factories;

public class OrderFactory
{
    public Order Create(string customerId, IEnumerable<(string ProductId, decimal Price, int Quantity)> items)
    {
        var orderItems = items
            .Select(i => OrderItem.Create(i.ProductId, i.Price, i.Quantity))
            .ToList();

        return Order.Create(customerId, orderItems);
    }
}
```

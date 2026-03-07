# CreateOrderRequest

DTOs de entrada da API. Separados dos DTOs da Application para que a Presentation não vaze para o domínio.

```csharp
namespace Presentation.DTOs;

public record CreateOrderRequest(string CustomerId, List<OrderItemRequest> Items);
public record OrderItemRequest(string ProductId, decimal Price, int Quantity);
```

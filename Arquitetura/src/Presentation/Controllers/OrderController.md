# OrderController

Controller da API. Recebe a requisição HTTP, mapeia para o input do caso de uso e retorna o resultado.

```csharp
namespace Presentation.Controllers;

[ApiController]
[Route("api/orders")]
public class OrderController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrderUseCase;

    public OrderController(CreateOrderUseCase createOrderUseCase)
    {
        _createOrderUseCase = createOrderUseCase;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var input = new CreateOrderInput(
            request.CustomerId,
            request.Items.Select(i => new OrderItemInput(i.ProductId, i.Price, i.Quantity)).ToList()
        );

        var result = await _createOrderUseCase.ExecuteAsync(input, ct);

        if (result.IsFailed)
            return BadRequest(result.Errors.Select(e => e.Message));

        return Created($"api/orders/{result.Value.OrderId}", result.Value);
    }
}
```

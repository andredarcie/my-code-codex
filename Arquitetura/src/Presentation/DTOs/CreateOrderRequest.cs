namespace Presentation.DTOs;

public record CreateOrderRequest(string CustomerId, List<OrderItemRequest> Items);
public record OrderItemRequest(string ProductId, decimal Price, int Quantity);

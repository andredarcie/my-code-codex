namespace Domain.Services;

// Domain Service: regra que envolve múltiplas entidades e não pertence a nenhuma delas
public class OrderDomainService
{
    // Verifica se o cliente pode fazer um novo pedido com base em pedidos existentes
    public bool CustomerCanPlaceOrder(IEnumerable<Order> existingOrders, int maxPendingOrders = 3)
    {
        var pendingCount = existingOrders.Count(o => o.Status == OrderStatus.Pending);
        return pendingCount < maxPendingOrders;
    }
}

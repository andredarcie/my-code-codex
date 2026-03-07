namespace Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public string ProductId { get; private set; }
    public decimal Price { get; private set; }
    public int Quantity { get; private set; }

    private OrderItem() { }

    public static OrderItem Create(string productId, decimal price, int quantity)
    {
        if (price <= 0)
            throw new DomainException("Preço deve ser maior que zero.");

        if (quantity <= 0)
            throw new DomainException("Quantidade deve ser maior que zero.");

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Price = price,
            Quantity = quantity
        };
    }
}

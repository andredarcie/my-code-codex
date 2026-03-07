namespace Infra.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IEnumerable<Order>> GetByCustomerIdAsync(string customerId, CancellationToken ct = default) =>
        await _context.Orders
            .Include(o => o.Items)
            .Where(o => o.CustomerId == customerId)
            .ToListAsync(ct);

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await _context.Orders.AddAsync(order, ct);

    public Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _context.Orders.Update(order);
        return Task.CompletedTask;
    }
}

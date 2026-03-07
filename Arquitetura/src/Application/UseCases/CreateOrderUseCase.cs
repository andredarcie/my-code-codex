namespace Application.UseCases;

public record CreateOrderInput(string CustomerId, List<OrderItemInput> Items);
public record OrderItemInput(string ProductId, decimal Price, int Quantity);
public record CreateOrderOutput(Guid OrderId, decimal Total);

public class CreateOrderUseCase
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDomainService _domainService;
    private readonly OrderFactory _factory;
    private readonly IEventPublisher _eventPublisher;

    public CreateOrderUseCase(
        IOrderRepository orderRepository,
        IUnitOfWork unitOfWork,
        OrderDomainService domainService,
        OrderFactory factory,
        IEventPublisher eventPublisher)
    {
        _orderRepository = orderRepository;
        _unitOfWork = unitOfWork;
        _domainService = domainService;
        _factory = factory;
        _eventPublisher = eventPublisher;
    }

    public async Task<Result<CreateOrderOutput>> ExecuteAsync(CreateOrderInput input, CancellationToken ct = default)
    {
        var existingOrders = await _orderRepository.GetByCustomerIdAsync(input.CustomerId, ct);

        if (!_domainService.CustomerCanPlaceOrder(existingOrders))
            return Result.Fail("Cliente atingiu o limite de pedidos pendentes.");

        var order = _factory.Create(
            input.CustomerId,
            input.Items.Select(i => (i.ProductId, i.Price, i.Quantity))
        );

        await _orderRepository.AddAsync(order, ct);

        // Um único commit — tudo persiste junto ou nada persiste
        await _unitOfWork.CommitAsync(ct);

        foreach (var domainEvent in order.DomainEvents)
        {
            var integrationEvent = new OrderCreatedIntegrationEvent(order.Id, order.CustomerId);
            await _eventPublisher.PublishAsync(integrationEvent, ct);
        }

        order.ClearDomainEvents();

        return Result.Ok(new CreateOrderOutput(order.Id, order.Total));
    }
}

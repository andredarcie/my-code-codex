namespace Infra.Messaging;

// Consumer: recebe mensagem do Kafka e chama o CasoDeUso correspondente
public class KafkaConsumer : BackgroundService
{
    private readonly IConsumer<string, string> _consumer;
    private readonly IServiceScopeFactory _scopeFactory;

    public KafkaConsumer(IConsumer<string, string> consumer, IServiceScopeFactory scopeFactory)
    {
        _consumer = consumer;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _consumer.Subscribe("orders-topic");

        while (!ct.IsCancellationRequested)
        {
            var result = _consumer.Consume(ct);
            var input = JsonSerializer.Deserialize<CreateOrderInput>(result.Message.Value);

            if (input is null) continue;

            using var scope = _scopeFactory.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<CreateOrderUseCase>();

            await useCase.ExecuteAsync(input, ct);
        }
    }
}

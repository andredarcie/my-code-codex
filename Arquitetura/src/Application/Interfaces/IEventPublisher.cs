namespace Application.Interfaces;

public interface IEventPublisher
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default);
}

using OT.Assessment.Shared.DTOs;

namespace OT.Assessment.Shared.Services;

public interface IRabbitMQPublisher
{
    Task PublishCasinoWagerAsync(CasinoWagerDto wager);
}

public interface IRabbitMQConsumer
{
    Task StartConsumingAsync(Func<CasinoWagerDto, Task> messageHandler, CancellationToken cancellationToken);
    void StopConsuming();
}
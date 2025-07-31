using Microsoft.Extensions.DependencyInjection;
using OT.Assessment.Shared.Services;

namespace OT.Assessment.Consumer.Services;

public class CasinoWagerConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQConsumer _rabbitMQConsumer;
    private readonly ILogger<CasinoWagerConsumerService> _logger;

    public CasinoWagerConsumerService(
        IServiceProvider serviceProvider,
        IRabbitMQConsumer rabbitMQConsumer,
        ILogger<CasinoWagerConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _rabbitMQConsumer = rabbitMQConsumer;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Casino Wager Consumer Service started");

        try
        {
            await _rabbitMQConsumer.StartConsumingAsync(async wager =>
            {
                using var scope = _serviceProvider.CreateScope();
                var casinoService = scope.ServiceProvider.GetRequiredService<ICasinoService>();
                
                try
                {
                    _logger.LogDebug("Processing casino wager: {WagerId}", wager.WagerId);
                    
                    var id = await casinoService.ProcessCasinoWagerAsync(wager);
                    
                    _logger.LogDebug("Successfully processed casino wager: {WagerId}, Database ID: {Id}", 
                        wager.WagerId, id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process casino wager: {WagerId}", wager.WagerId);
                    throw; // Re-throw to trigger message requeue
                }
            }, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Casino Wager Consumer Service encountered an error");
            throw;
        }
        finally
        {
            _logger.LogInformation("Casino Wager Consumer Service stopped");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Casino Wager Consumer Service is stopping");
        
        _rabbitMQConsumer.StopConsuming();
        
        await base.StopAsync(cancellationToken);
    }
}
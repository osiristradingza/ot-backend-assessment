using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OT.Assessment.Shared.DTOs;
using RabbitMQ.Client;

namespace OT.Assessment.Shared.Services;

public class RabbitMQPublisher : IRabbitMQPublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly string _queueName;

    public RabbitMQPublisher(IConfiguration configuration, ILogger<RabbitMQPublisher> logger)
    {
        _logger = logger;
        _queueName = configuration.GetValue<string>("RabbitMQ:QueueName", "casino_wagers");
        
        var factory = new ConnectionFactory
        {
            HostName = configuration.GetValue<string>("RabbitMQ:HostName", "localhost"),
            UserName = configuration.GetValue<string>("RabbitMQ:UserName", "guest"),
            Password = configuration.GetValue<string>("RabbitMQ:Password", "guest"),
            Port = configuration.GetValue<int>("RabbitMQ:Port", 5672)
        };

        try
        {
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            // Declare queue
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);
                
            _logger.LogInformation("RabbitMQ Publisher initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Publisher");
            throw;
        }
    }

    public async Task PublishCasinoWagerAsync(CasinoWagerDto wager)
    {
        try
        {
            var message = JsonSerializer.Serialize(wager);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;

            _channel.BasicPublish(
                exchange: "",
                routingKey: _queueName,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Published casino wager message: {WagerId}", wager.WagerId);
            
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish casino wager message: {WagerId}", wager.WagerId);
            throw;
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
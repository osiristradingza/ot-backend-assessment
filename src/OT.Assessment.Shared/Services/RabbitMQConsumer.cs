using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OT.Assessment.Shared.DTOs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OT.Assessment.Shared.Services;

public class RabbitMQConsumer : IRabbitMQConsumer, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQConsumer> _logger;
    private readonly string _queueName;
    private string _consumerTag;

    public RabbitMQConsumer(IConfiguration configuration, ILogger<RabbitMQConsumer> logger)
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
                
            // Set QoS to process one message at a time
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                
            _logger.LogInformation("RabbitMQ Consumer initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ Consumer");
            throw;
        }
    }

    public async Task StartConsumingAsync(Func<CasinoWagerDto, Task> messageHandler, CancellationToken cancellationToken)
    {
        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var wager = JsonSerializer.Deserialize<CasinoWagerDto>(message);
                
                if (wager != null)
                {
                    await messageHandler(wager);
                    _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                    _logger.LogDebug("Processed casino wager message: {WagerId}", wager.WagerId);
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize message, rejecting...");
                    _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing casino wager message");
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _consumerTag = _channel.BasicConsume(
            queue: _queueName,
            autoAck: false,
            consumer: consumer);
            
        _logger.LogInformation("Started consuming messages from queue: {QueueName}", _queueName);
        
        // Keep the method alive while cancellation is not requested
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(1000, cancellationToken);
        }
    }

    public void StopConsuming()
    {
        if (!string.IsNullOrEmpty(_consumerTag))
        {
            _channel.BasicCancel(_consumerTag);
            _logger.LogInformation("Stopped consuming messages");
        }
    }

    public void Dispose()
    {
        StopConsuming();
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
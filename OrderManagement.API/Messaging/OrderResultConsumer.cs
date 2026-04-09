using MediatR;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using OrderManagement.API.CQRS.Commands;
using Shared.Contracts.Events;
using Shared.Contracts.Messages;
using Serilog;

namespace OrderManagement.API.Messaging;

public class OrderResultConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _hostName;
    private IConnection? _connection;
    private IChannel? _channel;

    public OrderResultConsumer(IServiceScopeFactory scopeFactory, IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _hostName = config["RabbitMQ:Host"] ?? "localhost";
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Retry connecting to RabbitMQ
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = _hostName };
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                break;
            }
            catch
            {
                Log.Warning("OrderResultConsumer: RabbitMQ not ready, retry {Attempt}/10", i + 1);
                await Task.Delay(3000, stoppingToken);
            }
        }

        if (_channel == null) { Log.Warning("OrderResultConsumer: Could not connect to RabbitMQ"); return; }

        // Declare all queues we consume
        var queues = new[]
        {
            RabbitMqQueues.InventoryCheckCompleted,
            RabbitMqQueues.PaymentProcessed,
            RabbitMqQueues.ShippingCreated,
            RabbitMqQueues.ShippingFailed,
            RabbitMqQueues.OrderFailed
        };

        foreach (var q in queues)
            await _channel.QueueDeclareAsync(q, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var queue = ea.RoutingKey;
            Log.Information("OrderResultConsumer received message on queue {Queue}", queue);

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                if (queue == RabbitMqQueues.InventoryCheckCompleted)
                {
                    var evt = JsonSerializer.Deserialize<InventoryCheckCompletedEvent>(body)!;
                    await mediator.Send(new ProcessInventoryResultCommand(evt.OrderId, evt.CorrelationId, evt.Success, evt.FailureReason));
                }
                else if (queue == RabbitMqQueues.PaymentProcessed)
                {
                    var evt = JsonSerializer.Deserialize<PaymentProcessedEvent>(body)!;
                    await mediator.Send(new ProcessPaymentResultCommand(evt.OrderId, evt.CorrelationId, evt.Success, evt.TransactionId, evt.FailureReason));
                }
                else if (queue == RabbitMqQueues.ShippingCreated)
                {
                    var evt = JsonSerializer.Deserialize<ShippingCreatedEvent>(body)!;
                    await mediator.Send(new CreateShipmentCommand(evt.OrderId, evt.CorrelationId, evt.TrackingNumber, evt.EstimatedDispatch));
                }
                else if (queue == RabbitMqQueues.ShippingFailed || queue == RabbitMqQueues.OrderFailed)
                {
                    var evt = JsonSerializer.Deserialize<OrderFailedEvent>(body)!;
                    await mediator.Send(new FailOrderCommand(evt.OrderId, evt.CorrelationId, evt.Reason, evt.FailedStage));
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error processing message from queue {Queue}", queue);
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        foreach (var q in queues)
            await _channel.BasicConsumeAsync(q, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        Log.Information("OrderResultConsumer started, listening on {Count} queues", queues.Length);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

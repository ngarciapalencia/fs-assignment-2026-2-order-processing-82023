using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Shared.Contracts.Events;
using Shared.Contracts.Messages;
using System.Text;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [ShippingService] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/shipping-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices((ctx, services) =>
    {
        services.AddHostedService<ShippingConsumer>();
    })
    .Build();

host.Run();

public class ShippingConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;
    private static readonly Random _rng = new();

    public ShippingConsumer(IConfiguration config) => _config = config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitHost = _config["RabbitMQ:Host"] ?? "localhost";
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = rabbitHost };
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                break;
            }
            catch
            {
                Log.Warning("ShippingService: RabbitMQ not ready, retry {Attempt}/10", i + 1);
                await Task.Delay(3000, stoppingToken);
            }
        }

        if (_channel == null) { Log.Error("ShippingService: Cannot connect to RabbitMQ"); return; }

        await _channel.QueueDeclareAsync(RabbitMqQueues.ShippingRequested, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(RabbitMqQueues.ShippingCreated, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(RabbitMqQueues.ShippingFailed, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                var evt = JsonSerializer.Deserialize<ShippingRequestedEvent>(body)!;
                Log.Information("ShippingService: Creating shipment for Order {OrderId}. Customer: {Customer}. CorrelationId: {CorrelationId}",
                    evt.OrderId, evt.CustomerName, evt.CorrelationId);

                await Task.Delay(600, stoppingToken); // simulate processing

                // 5% random shipping failure
                if (_rng.NextDouble() < 0.05)
                {
                    var failEvt = new ShippingFailedEvent(evt.OrderId, evt.CorrelationId, "Shipping carrier unavailable for destination");
                    var failBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(failEvt));
                    var failProps = new BasicProperties { Persistent = true };
                    await _channel.BasicPublishAsync("", RabbitMqQueues.ShippingFailed, true, failProps, failBody, stoppingToken);
                    Log.Warning("ShippingService: Shipment failed for Order {OrderId}", evt.OrderId);
                }
                else
                {
                    var tracking = $"SS-{DateTime.UtcNow:yyyyMMdd}-{_rng.Next(100000, 999999)}";
                    var dispatch = DateTime.UtcNow.AddDays(_rng.Next(2, 6));

                    var successEvt = new ShippingCreatedEvent(evt.OrderId, evt.CorrelationId, tracking, dispatch);
                    var successBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(successEvt));
                    var props = new BasicProperties { Persistent = true };
                    await _channel.BasicPublishAsync("", RabbitMqQueues.ShippingCreated, true, props, successBody, stoppingToken);
                    Log.Information("ShippingService: Shipment created for Order {OrderId}. Tracking: {Tracking}. Dispatch: {Dispatch}",
                        evt.OrderId, tracking, dispatch);
                }

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "ShippingService: Error processing shipping message");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(RabbitMqQueues.ShippingRequested, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        Log.Information("ShippingService started, listening on {Queue}", RabbitMqQueues.ShippingRequested);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

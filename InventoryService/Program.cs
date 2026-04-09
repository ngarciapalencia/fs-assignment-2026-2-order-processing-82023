using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Shared.Contracts.Events;
using Shared.Contracts.Messages;
using System.Text;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [InventoryService] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/inventory-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices((ctx, services) =>
    {
        services.AddHostedService<InventoryConsumer>();
    })
    .Build();

host.Run();

public class InventoryConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;

    // Simulated in-memory stock
    private static readonly Dictionary<long, int> _stock = new()
    {
        {1, 50}, {2, 100}, {3, 200}, {4, 75}, {5, 5},
        {6, 150}, {7, 80}, {8, 30}, {9, 10}
    };

    public InventoryConsumer(IConfiguration config) => _config = config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _config["RabbitMQ:Host"] ?? "localhost";
        for (int i = 0; i < 10; i++)
        {
            try
            {
                var factory = new ConnectionFactory { HostName = host };
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                break;
            }
            catch
            {
                Log.Warning("InventoryService: RabbitMQ not ready, retry {Attempt}/10", i + 1);
                await Task.Delay(3000, stoppingToken);
            }
        }

        if (_channel == null) { Log.Error("InventoryService: Cannot connect to RabbitMQ"); return; }

        await _channel.QueueDeclareAsync(RabbitMqQueues.OrderSubmitted, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(RabbitMqQueues.InventoryCheckCompleted, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                var evt = JsonSerializer.Deserialize<OrderSubmittedEvent>(body)!;
                Log.Information("InventoryService: Checking stock for Order {OrderId}. CorrelationId: {CorrelationId}", evt.OrderId, evt.CorrelationId);

                // Simulate inventory check
                await Task.Delay(500, stoppingToken); // simulate processing

                bool success = true;
                string? failReason = null;

                foreach (var item in evt.Items)
                {
                    var available = _stock.GetValueOrDefault(item.ProductId, 0);
                    if (available < item.Quantity)
                    {
                        success = false;
                        failReason = $"Insufficient stock for product '{item.ProductName}': requested {item.Quantity}, available {available}";
                        Log.Warning("InventoryService: Stock insufficient for product {ProductId}", item.ProductId);
                        break;
                    }
                }

                if (success)
                {
                    // Reserve stock
                    foreach (var item in evt.Items)
                        _stock[item.ProductId] = _stock.GetValueOrDefault(item.ProductId, 0) - item.Quantity;
                    Log.Information("InventoryService: Stock confirmed for Order {OrderId}", evt.OrderId);
                }

                var result = new InventoryCheckCompletedEvent(evt.OrderId, evt.CorrelationId, success, failReason);
                var resultBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));
                var props = new BasicProperties { Persistent = true };
                await _channel.BasicPublishAsync("", RabbitMqQueues.InventoryCheckCompleted, true, props, resultBody, stoppingToken);
                Log.Information("InventoryService: Published InventoryCheckCompleted for Order {OrderId}. Success={Success}", evt.OrderId, success);

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "InventoryService: Error processing message");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(RabbitMqQueues.OrderSubmitted, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        Log.Information("InventoryService started, listening on {Queue}", RabbitMqQueues.OrderSubmitted);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

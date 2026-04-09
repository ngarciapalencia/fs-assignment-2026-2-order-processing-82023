using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Shared.Contracts.Events;
using Shared.Contracts.Messages;
using System.Text;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [PaymentService] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/payment-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices((ctx, services) =>
    {
        services.AddHostedService<PaymentConsumer>();
    })
    .Build();

host.Run();

public class PaymentConsumer : BackgroundService
{
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IChannel? _channel;
    private static readonly Random _rng = new();

    // Test card numbers that will always be rejected
    private static readonly HashSet<string> _rejectedCustomers = new() { "customer-rejected", "test-fail" };

    public PaymentConsumer(IConfiguration config) => _config = config;

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
                Log.Warning("PaymentService: RabbitMQ not ready, retry {Attempt}/10", i + 1);
                await Task.Delay(3000, stoppingToken);
            }
        }

        if (_channel == null) { Log.Error("PaymentService: Cannot connect to RabbitMQ"); return; }

        await _channel.QueueDeclareAsync(RabbitMqQueues.PaymentProcessingRequested, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
        await _channel.QueueDeclareAsync(RabbitMqQueues.PaymentProcessed, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            try
            {
                var evt = JsonSerializer.Deserialize<PaymentProcessingRequestedEvent>(body)!;
                Log.Information("PaymentService: Processing payment for Order {OrderId}. Amount: {Amount}. CorrelationId: {CorrelationId}",
                    evt.OrderId, evt.Amount, evt.CorrelationId);

                await Task.Delay(800, stoppingToken); // simulate processing delay

                // Payment logic: reject known test customers, randomly reject 10% of others
                bool success;
                string? txId = null;
                string? failReason = null;

                if (_rejectedCustomers.Contains(evt.CustomerId))
                {
                    success = false;
                    failReason = "Card declined: test rejection customer";
                }
                else if (_rng.NextDouble() < 0.10) // 10% random rejection
                {
                    success = false;
                    failReason = "Payment gateway declined: insufficient funds";
                }
                else
                {
                    success = true;
                    txId = $"TXN-{Guid.NewGuid():N}"[..16].ToUpper();
                }

                var result = new PaymentProcessedEvent(evt.OrderId, evt.CorrelationId, success, txId, failReason);
                var resultBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(result));
                var props = new BasicProperties { Persistent = true };
                await _channel.BasicPublishAsync("", RabbitMqQueues.PaymentProcessed, true, props, resultBody, stoppingToken);

                if (success)
                    Log.Information("PaymentService: Payment approved for Order {OrderId}. TxId: {TxId}", evt.OrderId, txId);
                else
                    Log.Warning("PaymentService: Payment rejected for Order {OrderId}. Reason: {Reason}", evt.OrderId, failReason);

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "PaymentService: Error processing payment message");
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
            }
        };

        await _channel.BasicConsumeAsync(RabbitMqQueues.PaymentProcessingRequested, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        Log.Information("PaymentService started, listening on {Queue}", RabbitMqQueues.PaymentProcessingRequested);
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null) await _channel.CloseAsync(cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

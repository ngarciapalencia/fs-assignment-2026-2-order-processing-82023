using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Serilog;

namespace OrderManagement.API.Messaging;

public interface IRabbitMqPublisher
{
    void Publish<T>(string queue, T message);
}

public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
{
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly string _hostName;
    private bool _initialized = false;

    public RabbitMqPublisher(IConfiguration config)
    {
        _hostName = config["RabbitMQ:Host"] ?? "localhost";
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        try
        {
            var factory = new ConnectionFactory { HostName = _hostName, RequestedHeartbeat = TimeSpan.FromSeconds(60) };
            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();
            _initialized = true;
            Log.Information("RabbitMQ publisher connected to {Host}", _hostName);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "RabbitMQ not available on {Host} — messages will be skipped", _hostName);
        }
    }

    public void Publish<T>(string queue, T message)
    {
        Task.Run(async () =>
        {
            await EnsureInitializedAsync();
            if (_channel == null) { Log.Warning("RabbitMQ channel not available, skipping publish to {Queue}", queue); return; }
            try
            {
                await _channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false);
                var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                var props = new BasicProperties { Persistent = true };
                await _channel.BasicPublishAsync("", queue, true, props, body);
                Log.Information("Published {EventType} to queue {Queue}", typeof(T).Name, queue);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to publish {EventType} to queue {Queue}", typeof(T).Name, queue);
            }
        }).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
    }
}

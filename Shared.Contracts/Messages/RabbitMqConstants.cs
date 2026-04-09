namespace Shared.Contracts.Messages;

public static class RabbitMqQueues
{
    public const string OrderSubmitted = "order.submitted";
    public const string InventoryCheckRequested = "inventory.check.requested";
    public const string InventoryCheckCompleted = "inventory.check.completed";
    public const string PaymentProcessingRequested = "payment.processing.requested";
    public const string PaymentProcessed = "payment.processed";
    public const string ShippingRequested = "shipping.requested";
    public const string ShippingCreated = "shipping.created";
    public const string ShippingFailed = "shipping.failed";
    public const string OrderCompleted = "order.completed";
    public const string OrderFailed = "order.failed";
}

public static class RabbitMqExchanges
{
    public const string Orders = "orders.exchange";
}

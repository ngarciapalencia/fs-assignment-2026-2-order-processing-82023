namespace Shared.Contracts.Events;

public record OrderSubmittedEvent(
    Guid OrderId,
    string CustomerId,
    string CustomerName,
    string CustomerEmail,
    string ShippingAddress,
    decimal TotalAmount,
    List<OrderItemContract> Items,
    DateTime SubmittedAt,
    string CorrelationId
);

public record InventoryCheckRequestedEvent(
    Guid OrderId,
    string CorrelationId,
    List<OrderItemContract> Items
);

public record InventoryCheckCompletedEvent(
    Guid OrderId,
    string CorrelationId,
    bool Success,
    string? FailureReason = null
);

public record PaymentProcessingRequestedEvent(
    Guid OrderId,
    string CorrelationId,
    string CustomerId,
    decimal Amount
);

public record PaymentProcessedEvent(
    Guid OrderId,
    string CorrelationId,
    bool Success,
    string? TransactionId = null,
    string? FailureReason = null
);

public record ShippingRequestedEvent(
    Guid OrderId,
    string CorrelationId,
    string CustomerName,
    string ShippingAddress,
    List<OrderItemContract> Items
);

public record ShippingCreatedEvent(
    Guid OrderId,
    string CorrelationId,
    string TrackingNumber,
    DateTime EstimatedDispatch
);

public record ShippingFailedEvent(
    Guid OrderId,
    string CorrelationId,
    string Reason
);

public record OrderCompletedEvent(
    Guid OrderId,
    string CorrelationId,
    DateTime CompletedAt
);

public record OrderFailedEvent(
    Guid OrderId,
    string CorrelationId,
    string Reason,
    string FailedStage
);

public record OrderItemContract(
    long ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

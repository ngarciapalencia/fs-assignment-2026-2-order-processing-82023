namespace OrderManagement.API.DTOs;

public record CheckoutRequest(
    string CustomerId,
    string CustomerName,
    string CustomerEmail,
    string ShippingAddress,
    List<CartItemRequest> Items
);

public record CartItemRequest(
    long ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice
);

public record OrderDto(
    Guid Id,
    string CustomerId,
    string CustomerName,
    string CustomerEmail,
    string ShippingAddress,
    string Status,
    decimal TotalAmount,
    string? FailureReason,
    string? TrackingNumber,
    string? PaymentTransactionId,
    DateTime? EstimatedDispatch,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<OrderItemDto> Items,
    InventoryRecordDto? InventoryRecord,
    PaymentRecordDto? PaymentRecord,
    ShipmentRecordDto? ShipmentRecord
);

public record OrderItemDto(
    long ProductId,
    string ProductName,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);

public record InventoryRecordDto(
    bool Success,
    string? FailureReason,
    DateTime ProcessedAt
);

public record PaymentRecordDto(
    bool Success,
    string? TransactionId,
    string? FailureReason,
    decimal Amount,
    DateTime ProcessedAt
);

public record ShipmentRecordDto(
    string TrackingNumber,
    DateTime EstimatedDispatch,
    DateTime CreatedAt
);

public record OrderStatusDto(Guid OrderId, string Status, string? FailureReason, string? TrackingNumber);

public record ProductDto(long ProductId, string Name, string Description, decimal Price, string Category, int StockQuantity);

public record DashboardSummaryDto(
    int TotalOrders,
    int CompletedOrders,
    int FailedOrders,
    int PendingOrders,
    decimal TotalRevenue,
    Dictionary<string, int> OrdersByStatus
);

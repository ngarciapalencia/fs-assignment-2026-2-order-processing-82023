namespace OrderManagement.API.Models;

public enum OrderStatus
{
    Cart,
    Submitted,
    InventoryPending,
    InventoryConfirmed,
    InventoryFailed,
    PaymentPending,
    PaymentApproved,
    PaymentFailed,
    ShippingPending,
    ShippingCreated,
    Completed,
    Failed
}

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Submitted;
    public decimal TotalAmount { get; set; }
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string? FailureReason { get; set; }
    public string? TrackingNumber { get; set; }
    public string? PaymentTransactionId { get; set; }
    public DateTime? EstimatedDispatch { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<OrderItem> Items { get; set; } = new();
    public InventoryRecord? InventoryRecord { get; set; }
    public PaymentRecord? PaymentRecord { get; set; }
    public ShipmentRecord? ShipmentRecord { get; set; }
}

public class OrderItem
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public class Product
{
    public long ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int StockQuantity { get; set; } = 100;
}

public class Customer
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<Order> Orders { get; set; } = new();
}

public class InventoryRecord
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class PaymentRecord
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public string? FailureReason { get; set; }
    public decimal Amount { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class ShipmentRecord
{
    public int Id { get; set; }
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public DateTime EstimatedDispatch { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

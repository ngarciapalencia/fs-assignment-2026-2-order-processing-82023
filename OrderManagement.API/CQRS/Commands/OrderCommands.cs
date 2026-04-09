using MediatR;
using OrderManagement.API.DTOs;

namespace OrderManagement.API.CQRS.Commands;

public record CheckoutOrderCommand(
    string CustomerId,
    string CustomerName,
    string CustomerEmail,
    string ShippingAddress,
    List<CartItemRequest> Items
) : IRequest<OrderDto>;

public record CancelOrderCommand(Guid OrderId) : IRequest<bool>;

public record ProcessInventoryResultCommand(
    Guid OrderId,
    string CorrelationId,
    bool Success,
    string? FailureReason
) : IRequest;

public record ProcessPaymentResultCommand(
    Guid OrderId,
    string CorrelationId,
    bool Success,
    string? TransactionId,
    string? FailureReason
) : IRequest;

public record CreateShipmentCommand(
    Guid OrderId,
    string CorrelationId,
    string TrackingNumber,
    DateTime EstimatedDispatch
) : IRequest;

public record FailOrderCommand(
    Guid OrderId,
    string CorrelationId,
    string Reason,
    string FailedStage
) : IRequest;

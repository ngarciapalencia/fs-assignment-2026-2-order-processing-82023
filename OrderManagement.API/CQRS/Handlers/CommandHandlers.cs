using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.API.CQRS.Commands;
using OrderManagement.API.Data;
using OrderManagement.API.DTOs;
using OrderManagement.API.Messaging;
using OrderManagement.API.Models;
using Serilog;
using Shared.Contracts.Events;

namespace OrderManagement.API.CQRS.Handlers;

public class CheckoutOrderHandler : IRequestHandler<CheckoutOrderCommand, OrderDto>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;
    private readonly IRabbitMqPublisher _publisher;

    public CheckoutOrderHandler(OrderDbContext db, IMapper mapper, IRabbitMqPublisher publisher)
    {
        _db = db;
        _mapper = mapper;
        _publisher = publisher;
    }

    public async Task<OrderDto> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            ShippingAddress = request.ShippingAddress,
            Status = OrderStatus.Submitted,
            CorrelationId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        Log.Information("Order {OrderId} submitted by customer {CustomerId}. CorrelationId: {CorrelationId}",
            order.Id, order.CustomerId, order.CorrelationId);

        var evt = new OrderSubmittedEvent(
            order.Id,
            order.CustomerId,
            order.CustomerName,
            order.CustomerEmail,
            order.ShippingAddress,
            order.TotalAmount,
            order.Items.Select(i => new OrderItemContract(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList(),
            order.CreatedAt,
            order.CorrelationId
        );

        _publisher.Publish(Shared.Contracts.Messages.RabbitMqQueues.OrderSubmitted, evt);
        Log.Information("Published OrderSubmittedEvent for Order {OrderId}", order.Id);

        return _mapper.Map<OrderDto>(order);
    }
}

public class ProcessInventoryResultHandler : IRequestHandler<ProcessInventoryResultCommand>
{
    private readonly OrderDbContext _db;
    private readonly IRabbitMqPublisher _publisher;

    public ProcessInventoryResultHandler(OrderDbContext db, IRabbitMqPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Handle(ProcessInventoryResultCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order == null) return;

        var record = new InventoryRecord
        {
            OrderId = order.Id,
            Success = request.Success,
            FailureReason = request.FailureReason,
            ProcessedAt = DateTime.UtcNow
        };
        _db.InventoryRecords.Add(record);

        if (request.Success)
        {
            order.Status = OrderStatus.InventoryConfirmed;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            Log.Information("Inventory confirmed for Order {OrderId}. Publishing payment request.", request.OrderId);
            _publisher.Publish(Shared.Contracts.Messages.RabbitMqQueues.PaymentProcessingRequested,
                new PaymentProcessingRequestedEvent(order.Id, order.CorrelationId, order.CustomerId, order.TotalAmount));
            order.Status = OrderStatus.PaymentPending;
        }
        else
        {
            order.Status = OrderStatus.InventoryFailed;
            order.FailureReason = request.FailureReason;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            Log.Warning("Inventory failed for Order {OrderId}: {Reason}", request.OrderId, request.FailureReason);
            _publisher.Publish(Shared.Contracts.Messages.RabbitMqQueues.OrderFailed,
                new OrderFailedEvent(order.Id, order.CorrelationId, request.FailureReason ?? "Inventory check failed", "Inventory"));
            order.Status = OrderStatus.Failed;
        }
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class ProcessPaymentResultHandler : IRequestHandler<ProcessPaymentResultCommand>
{
    private readonly OrderDbContext _db;
    private readonly IRabbitMqPublisher _publisher;

    public ProcessPaymentResultHandler(OrderDbContext db, IRabbitMqPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Handle(ProcessPaymentResultCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order == null) return;

        var record = new PaymentRecord
        {
            OrderId = order.Id,
            Success = request.Success,
            TransactionId = request.TransactionId,
            FailureReason = request.FailureReason,
            Amount = order.TotalAmount,
            ProcessedAt = DateTime.UtcNow
        };
        _db.PaymentRecords.Add(record);

        if (request.Success)
        {
            order.Status = OrderStatus.PaymentApproved;
            order.PaymentTransactionId = request.TransactionId;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            Log.Information("Payment approved for Order {OrderId}. TxId: {TxId}. Publishing shipping request.", request.OrderId, request.TransactionId);
            _publisher.Publish(Shared.Contracts.Messages.RabbitMqQueues.ShippingRequested,
                new ShippingRequestedEvent(order.Id, order.CorrelationId, order.CustomerName, order.ShippingAddress,
                    order.Items.Select(i => new OrderItemContract(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice)).ToList()));
            order.Status = OrderStatus.ShippingPending;
        }
        else
        {
            order.Status = OrderStatus.PaymentFailed;
            order.FailureReason = request.FailureReason;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            Log.Warning("Payment rejected for Order {OrderId}: {Reason}", request.OrderId, request.FailureReason);
            _publisher.Publish(Shared.Contracts.Messages.RabbitMqQueues.OrderFailed,
                new OrderFailedEvent(order.Id, order.CorrelationId, request.FailureReason ?? "Payment rejected", "Payment"));
            order.Status = OrderStatus.Failed;
        }
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class CreateShipmentHandler : IRequestHandler<CreateShipmentCommand>
{
    private readonly OrderDbContext _db;
    private readonly IRabbitMqPublisher _publisher;

    public CreateShipmentHandler(OrderDbContext db, IRabbitMqPublisher publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task Handle(CreateShipmentCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order == null) return;

        var shipment = new ShipmentRecord
        {
            OrderId = order.Id,
            TrackingNumber = request.TrackingNumber,
            EstimatedDispatch = request.EstimatedDispatch,
            CreatedAt = DateTime.UtcNow
        };
        _db.ShipmentRecords.Add(shipment);

        order.Status = OrderStatus.ShippingCreated;
        order.TrackingNumber = request.TrackingNumber;
        order.EstimatedDispatch = request.EstimatedDispatch;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        Log.Information("Shipment created for Order {OrderId}. Tracking: {Tracking}", request.OrderId, request.TrackingNumber);
        _publisher.Publish(Shared.Contracts.Messages.RabbitMqQueues.OrderCompleted,
            new OrderCompletedEvent(order.Id, order.CorrelationId, DateTime.UtcNow));

        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class FailOrderHandler : IRequestHandler<FailOrderCommand>
{
    private readonly OrderDbContext _db;

    public FailOrderHandler(OrderDbContext db) => _db = db;

    public async Task Handle(FailOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order == null) return;

        order.Status = OrderStatus.Failed;
        order.FailureReason = $"[{request.FailedStage}] {request.Reason}";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        Log.Warning("Order {OrderId} marked as Failed at stage {Stage}: {Reason}", request.OrderId, request.FailedStage, request.Reason);
    }
}

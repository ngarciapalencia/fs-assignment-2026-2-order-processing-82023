using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.API.CQRS.Commands;
using OrderManagement.API.Data;
using OrderManagement.API.Models;
using Serilog;

namespace OrderManagement.API.CQRS.Handlers;

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly OrderDbContext _db;

    public CancelOrderHandler(OrderDbContext db) => _db = db;

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        if (order == null) return false;

        // Only allow cancellation of orders not yet shipped
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.ShippingCreated)
        {
            Log.Warning("Cannot cancel Order {OrderId} - already in status {Status}", request.OrderId, order.Status);
            return false;
        }

        order.Status = OrderStatus.Failed;
        order.FailureReason = "Cancelled by user";
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        Log.Information("Order {OrderId} cancelled", request.OrderId);
        return true;
    }
}

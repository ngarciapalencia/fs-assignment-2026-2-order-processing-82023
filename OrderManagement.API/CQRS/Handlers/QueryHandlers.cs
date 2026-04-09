using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderManagement.API.CQRS.Queries;
using OrderManagement.API.Data;
using OrderManagement.API.DTOs;
using OrderManagement.API.Models;

namespace OrderManagement.API.CQRS.Handlers;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderDto?>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrderByIdHandler(OrderDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<OrderDto?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.InventoryRecord)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        return order == null ? null : _mapper.Map<OrderDto>(order);
    }
}

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrdersHandler(OrderDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<OrderDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.InventoryRecord)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<OrderDto>>(orders);
    }
}

public class GetCustomerOrdersHandler : IRequestHandler<GetCustomerOrdersQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetCustomerOrdersHandler(OrderDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<OrderDto>> Handle(GetCustomerOrdersQuery request, CancellationToken cancellationToken)
    {
        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.InventoryRecord)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .Where(o => o.CustomerId == request.CustomerId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<OrderDto>>(orders);
    }
}

public class GetOrdersByStatusHandler : IRequestHandler<GetOrdersByStatusQuery, List<OrderDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrdersByStatusHandler(OrderDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<OrderDto>> Handle(GetOrdersByStatusQuery request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var status))
            return new List<OrderDto>();

        var orders = await _db.Orders
            .Include(o => o.Items)
            .Include(o => o.InventoryRecord)
            .Include(o => o.PaymentRecord)
            .Include(o => o.ShipmentRecord)
            .Where(o => o.Status == status)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
        return _mapper.Map<List<OrderDto>>(orders);
    }
}

public class GetOrderStatusHandler : IRequestHandler<GetOrderStatusQuery, OrderStatusDto?>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetOrderStatusHandler(OrderDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<OrderStatusDto?> Handle(GetOrderStatusQuery request, CancellationToken cancellationToken)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);
        return order == null ? null : _mapper.Map<OrderStatusDto>(order);
    }
}

public class GetProductsHandler : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    private readonly OrderDbContext _db;
    private readonly IMapper _mapper;

    public GetProductsHandler(OrderDbContext db, IMapper mapper) { _db = db; _mapper = mapper; }

    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _db.Products.ToListAsync(cancellationToken);
        return _mapper.Map<List<ProductDto>>(products);
    }
}

public class GetDashboardSummaryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly OrderDbContext _db;

    public GetDashboardSummaryHandler(OrderDbContext db) => _db = db;

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var orders = await _db.Orders.ToListAsync(cancellationToken);
        var byStatus = orders.GroupBy(o => o.Status.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        return new DashboardSummaryDto(
            TotalOrders: orders.Count,
            CompletedOrders: orders.Count(o => o.Status == OrderStatus.Completed),
            FailedOrders: orders.Count(o => o.Status == OrderStatus.Failed),
            PendingOrders: orders.Count(o => o.Status != OrderStatus.Completed && o.Status != OrderStatus.Failed),
            TotalRevenue: orders.Where(o => o.Status == OrderStatus.Completed).Sum(o => o.TotalAmount),
            OrdersByStatus: byStatus
        );
    }
}

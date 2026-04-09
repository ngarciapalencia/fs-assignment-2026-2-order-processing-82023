using MediatR;
using OrderManagement.API.DTOs;

namespace OrderManagement.API.CQRS.Queries;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<OrderDto?>;

public record GetOrdersQuery(int Page = 1, int PageSize = 20) : IRequest<List<OrderDto>>;

public record GetCustomerOrdersQuery(string CustomerId) : IRequest<List<OrderDto>>;

public record GetOrdersByStatusQuery(string Status) : IRequest<List<OrderDto>>;

public record GetOrderStatusQuery(Guid OrderId) : IRequest<OrderStatusDto?>;

public record GetProductsQuery() : IRequest<List<ProductDto>>;

public record GetDashboardSummaryQuery() : IRequest<DashboardSummaryDto>;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.API.CQRS.Commands;
using OrderManagement.API.CQRS.Queries;
using OrderManagement.API.DTOs;
using Serilog;

namespace OrderManagement.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request)
    {
        Log.Information("Checkout request received for customer {CustomerId}", request.CustomerId);
        var result = await _mediator.Send(new CheckoutOrderCommand(
            request.CustomerId, request.CustomerName, request.CustomerEmail,
            request.ShippingAddress, request.Items));
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var orders = await _mediator.Send(new GetOrdersQuery(page, pageSize));
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id));
        return order == null ? NotFound() : Ok(order);
    }

    [HttpGet("{id:guid}/status")]
    public async Task<IActionResult> GetOrderStatus(Guid id)
    {
        var status = await _mediator.Send(new GetOrderStatusQuery(id));
        return status == null ? NotFound() : Ok(status);
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetOrdersByStatus(string status)
    {
        var orders = await _mediator.Send(new GetOrdersByStatusQuery(status));
        return Ok(orders);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> CancelOrder(Guid id)
    {
        var result = await _mediator.Send(new CancelOrderCommand(id));
        return result ? NoContent() : NotFound();
    }
}

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetProducts()
    {
        var products = await _mediator.Send(new GetProductsQuery());
        return Ok(products);
    }
}

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomersController(IMediator mediator) => _mediator = mediator;

    [HttpGet("{customerId}/orders")]
    public async Task<IActionResult> GetCustomerOrders(string customerId)
    {
        var orders = await _mediator.Send(new GetCustomerOrdersQuery(customerId));
        return Ok(orders);
    }
}

[ApiController]
[Route("api/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var summary = await _mediator.Send(new GetDashboardSummaryQuery());
        return Ok(summary);
    }
}

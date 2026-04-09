using System.Net;
using System.Net.Http.Json;
using OrderManagement.API.DTOs;
using Xunit;

namespace Integration.Tests;

public class OrdersApiTests : IClassFixture<OrderApiFactory>
{
    private readonly HttpClient _client;

    public OrdersApiTests(OrderApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsSeededProducts()
    {
        var response = await _client.GetAsync("/api/products");
        response.EnsureSuccessStatusCode();
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.NotNull(products);
        Assert.True(products.Count > 0);
    }

    [Fact]
    public async Task GetProducts_ContainsKayak()
    {
        var response = await _client.GetAsync("/api/products");
        var products = await response.Content.ReadFromJsonAsync<List<ProductDto>>();
        Assert.Contains(products!, p => p.Name == "Kayak");
    }

    [Fact]
    public async Task Checkout_ValidRequest_ReturnsCreated()
    {
        var request = new CheckoutRequest(
            CustomerId: "customer-1",
            CustomerName: "Test User",
            CustomerEmail: "test@test.com",
            ShippingAddress: "123 Main St, Dublin",
            Items: new List<CartItemRequest>
            {
                new(ProductId: 1, ProductName: "Kayak", Quantity: 1, UnitPrice: 275m)
            }
        );

        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal("Submitted", order.Status);
        Assert.Equal(275m, order.TotalAmount);
    }

    [Fact]
    public async Task Checkout_MultipleItems_CorrectTotal()
    {
        var request = new CheckoutRequest(
            CustomerId: "customer-1",
            CustomerName: "Test User",
            CustomerEmail: "test@test.com",
            ShippingAddress: "123 Main St",
            Items: new List<CartItemRequest>
            {
                new(1, "Kayak", 2, 275m),
                new(3, "Soccer Ball", 3, 19.50m)
            }
        );

        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal(275m * 2 + 19.50m * 3, order.TotalAmount);
        Assert.Equal(2, order.Items.Count);
    }

    [Fact]
    public async Task GetOrderById_AfterCheckout_ReturnsOrder()
    {
        var request = new CheckoutRequest(
            "customer-1", "Test User", "test@test.com", "Dublin",
            new List<CartItemRequest> { new(2, "Lifejacket", 1, 48.95m) }
        );
        var createResp = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        var created = await createResp.Content.ReadFromJsonAsync<OrderDto>();

        var getResp = await _client.GetAsync($"/api/orders/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
        var fetched = await getResp.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task GetOrderStatus_AfterCheckout_ReturnsStatus()
    {
        var request = new CheckoutRequest(
            "customer-1", "Test", "t@t.com", "Addr",
            new List<CartItemRequest> { new(1, "Kayak", 1, 275m) }
        );
        var createResp = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        var created = await createResp.Content.ReadFromJsonAsync<OrderDto>();

        var statusResp = await _client.GetAsync($"/api/orders/{created!.Id}/status");
        Assert.Equal(HttpStatusCode.OK, statusResp.StatusCode);
        var status = await statusResp.Content.ReadFromJsonAsync<OrderStatusDto>();
        Assert.Equal("Submitted", status!.Status);
    }

    [Fact]
    public async Task GetOrders_ReturnsAllOrders()
    {
        // Place two orders first
        for (int i = 0; i < 2; i++)
        {
            await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutRequest(
                "customer-1", $"User{i}", $"u{i}@t.com", "Addr",
                new List<CartItemRequest> { new(1, "Kayak", 1, 275m) }
            ));
        }
        var response = await _client.GetAsync("/api/orders");
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.NotNull(orders);
        Assert.True(orders.Count >= 2);
    }

    [Fact]
    public async Task GetCustomerOrders_ReturnsOnlyThatCustomersOrders()
    {
        await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutRequest(
            "customer-1", "Special", "s@t.com", "Addr",
            new List<CartItemRequest> { new(1, "Kayak", 1, 275m) }
        ));

        var response = await _client.GetAsync("/api/customers/customer-1/orders");
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.NotNull(orders);
        Assert.All(orders, o => Assert.Equal("customer-1", o.CustomerId));
    }

    [Fact]
    public async Task GetOrdersByStatus_Submitted_ReturnsMatchingOrders()
    {
        await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutRequest(
            "customer-1", "User", "u@t.com", "Addr",
            new List<CartItemRequest> { new(1, "Kayak", 1, 275m) }
        ));

        var response = await _client.GetAsync("/api/orders/status/Submitted");
        response.EnsureSuccessStatusCode();
        var orders = await response.Content.ReadFromJsonAsync<List<OrderDto>>();
        Assert.NotNull(orders);
        Assert.All(orders, o => Assert.Equal("Submitted", o.Status));
    }

    [Fact]
    public async Task GetOrder_NotFound_Returns404()
    {
        var response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboardSummary_ReturnsValidData()
    {
        var response = await _client.GetAsync("/api/dashboard/summary");
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary.TotalOrders >= 0);
        Assert.NotNull(summary.OrdersByStatus);
    }
}

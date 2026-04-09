using CustomerPortal.Blazor.Models;
using System.Net.Http.Json;
using Serilog;

namespace CustomerPortal.Blazor.Services;

public class OrderApiService
{
    private readonly HttpClient _http;

    public OrderApiService(HttpClient http) => _http = http;

    public async Task<List<ProductDto>> GetProductsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<ProductDto>>("api/products") ?? new();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch products");
            return new();
        }
    }

    public async Task<OrderDto?> CheckoutAsync(CheckoutRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/orders/checkout", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<OrderDto>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Checkout failed");
            return null;
        }
    }

    public async Task<List<OrderDto>> GetCustomerOrdersAsync(string customerId)
    {
        try
        {
            return await _http.GetFromJsonAsync<List<OrderDto>>($"api/customers/{customerId}/orders") ?? new();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch customer orders");
            return new();
        }
    }

    public async Task<OrderStatusDto?> GetOrderStatusAsync(Guid orderId)
    {
        try
        {
            return await _http.GetFromJsonAsync<OrderStatusDto>($"api/orders/{orderId}/status");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch order status for {OrderId}", orderId);
            return null;
        }
    }

    public async Task<OrderDto?> GetOrderAsync(Guid orderId)
    {
        try
        {
            return await _http.GetFromJsonAsync<OrderDto>($"api/orders/{orderId}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to fetch order {OrderId}", orderId);
            return null;
        }
    }
}

public class CartService
{
    private readonly List<CartItem> _items = new();
    public IReadOnlyList<CartItem> Items => _items.AsReadOnly();
    public decimal Total => _items.Sum(i => i.LineTotal);
    public int Count => _items.Sum(i => i.Quantity);
    public event Action? OnChange;

    public void AddItem(ProductDto product, int quantity = 1)
    {
        var existing = _items.FirstOrDefault(i => i.ProductId == product.ProductId);
        if (existing != null)
            existing.Quantity += quantity;
        else
            _items.Add(new CartItem { ProductId = product.ProductId, ProductName = product.Name, Quantity = quantity, UnitPrice = product.Price });
        OnChange?.Invoke();
    }

    public void RemoveItem(long productId)
    {
        _items.RemoveAll(i => i.ProductId == productId);
        OnChange?.Invoke();
    }

    public void UpdateQuantity(long productId, int quantity)
    {
        var item = _items.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            if (quantity <= 0) _items.Remove(item);
            else item.Quantity = quantity;
            OnChange?.Invoke();
        }
    }

    public void Clear()
    {
        _items.Clear();
        OnChange?.Invoke();
    }
}

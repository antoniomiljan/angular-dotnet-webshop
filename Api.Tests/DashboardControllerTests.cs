using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs;
using Api.Models;

namespace Api.Tests;

public class DashboardControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public DashboardControllerTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateAdminToken());
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetDashboard_ReturnsUnauthorized_WithoutAdminToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetDashboard_TotalRevenue_OnlyCountsPaidShippedOrDelivered()
    {
        var (_, product) = await _factory.SeedProductAsync(price: 100m);

        await _factory.SeedOrderAsync(OrderStatus.Pending, (product, 1));
        await _factory.SeedOrderAsync(OrderStatus.Cancelled, (product, 1));
        await _factory.SeedOrderAsync(OrderStatus.Paid, (product, 2));
        await _factory.SeedOrderAsync(OrderStatus.Shipped, (product, 1));
        await _factory.SeedOrderAsync(OrderStatus.Delivered, (product, 1));

        var dashboard = await _client.GetFromJsonAsync<DashboardDto>("/api/dashboard", Json);

        Assert.NotNull(dashboard);
        Assert.Equal(400m, dashboard.TotalRevenue); // (2 + 1 + 1) * 100, Pending/Cancelled excluded
        Assert.Equal(5, dashboard.TotalOrders);
        Assert.Equal(1, dashboard.PendingOrdersCount);
    }

    [Fact]
    public async Task GetDashboard_ListsOnlyLowStockActiveProducts()
    {
        var (_, lowStock) = await _factory.SeedProductAsync(stock: 3);
        var (_, healthyStock) = await _factory.SeedProductAsync(stock: 50);
        var (_, inactiveLowStock) = await _factory.SeedProductAsync(stock: 1, active: false);

        var dashboard = await _client.GetFromJsonAsync<DashboardDto>("/api/dashboard", Json);

        Assert.NotNull(dashboard);
        Assert.Contains(dashboard.LowStockProducts, p => p.Id == lowStock.Id);
        Assert.DoesNotContain(dashboard.LowStockProducts, p => p.Id == healthyStock.Id);
        Assert.DoesNotContain(dashboard.LowStockProducts, p => p.Id == inactiveLowStock.Id);
    }

    [Fact]
    public async Task GetDashboard_RanksTopProductsByQuantitySold()
    {
        var (_, popular) = await _factory.SeedProductAsync(price: 10m);
        var (_, unpopular) = await _factory.SeedProductAsync(price: 10m);

        await _factory.SeedOrderAsync(OrderStatus.Paid, (popular, 10));
        await _factory.SeedOrderAsync(OrderStatus.Paid, (unpopular, 1));
        // Pending sales shouldn't count toward "sold" yet.
        await _factory.SeedOrderAsync(OrderStatus.Pending, (unpopular, 100));

        var dashboard = await _client.GetFromJsonAsync<DashboardDto>("/api/dashboard", Json);

        Assert.NotNull(dashboard);
        Assert.Equal(popular.Id, dashboard.TopProducts.First().ProductId);
        Assert.Equal(10, dashboard.TopProducts.First().QuantitySold);
    }
}

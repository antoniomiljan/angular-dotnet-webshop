using System.Net;
using System.Net.Http.Headers;
using Api.Models;

namespace Api.Tests;

public class DebugControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public DebugControllerTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateAdminToken());
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ResetOrders_ReturnsUnauthorized_WithoutAdminToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.DeleteAsync("/api/debug/reset-orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetOrders_ClearsOrdersAndOrderItems_ButNotProducts()
    {
        var (_, product) = await _factory.SeedProductAsync();
        await _factory.SeedOrderAsync(OrderStatus.Paid, (product, 1));
        await _factory.SeedOrderAsync(OrderStatus.Pending, (product, 2));

        var response = await _client.DeleteAsync("/api/debug/reset-orders");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var productStillThere = await _client.GetAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.OK, productStillThere.StatusCode);

        var dashboard = await _client.GetAsync("/api/dashboard");
        var body = await dashboard.Content.ReadAsStringAsync();
        Assert.Contains("\"totalOrders\":0", body);
    }

    // The safety-critical case: this must 404 in Production no matter what, even
    // with a valid admin token, since nothing should be able to wipe real order data.
    [Fact]
    public async Task ResetOrders_ReturnsNotFound_InProduction()
    {
        using var prodFactory = new TestWebApplicationFactory(environmentName: "Production");
        var prodClient = prodFactory.CreateClient();
        prodClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", prodFactory.CreateAdminToken());

        var response = await prodClient.DeleteAsync("/api/debug/reset-orders");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

public class OrdersControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public OrdersControllerTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private static object OrderPayload(int productId, int quantity, string email = "buyer@example.com") => new
    {
        items = new[] { new { productId, quantity } },
        guestEmail = email
    };

    [Fact]
    public async Task CreateOrder_Succeeds_AndDecrementsStock()
    {
        var (_, product) = await _factory.SeedProductAsync(stock: 5);

        var response = await _client.PostAsJsonAsync("/api/orders", OrderPayload(product.Id, 2));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var productAfter = await _client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}", Json);
        Assert.Equal(3, productAfter.GetProperty("stock").GetInt32());
    }

    [Fact]
    public async Task CreateOrder_Fails_WhenQuantityExceedsStock()
    {
        var (_, product) = await _factory.SeedProductAsync(stock: 1);

        var response = await _client.PostAsJsonAsync("/api/orders", OrderPayload(product.Id, 2));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var productAfter = await _client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}", Json);
        Assert.Equal(1, productAfter.GetProperty("stock").GetInt32());
    }

    [Fact]
    public async Task CreateOrder_Fails_WhenProductIdInvalid()
    {
        var response = await _client.PostAsJsonAsync("/api/orders", OrderPayload(productId: 999_999, quantity: 1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_Fails_WhenGuestEmailMissing()
    {
        var (_, product) = await _factory.SeedProductAsync();

        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            items = new[] { new { productId = product.Id, quantity = 1 } },
            guestEmail = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_PreventsOverselling_UnderConcurrentRequests()
    {
        var (_, product) = await _factory.SeedProductAsync(stock: 1);
        var payload = OrderPayload(product.Id, 1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 5)
            .Select(_ => _client.PostAsJsonAsync("/api/orders", payload)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.Equal(4, responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest));

        var productAfter = await _client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}", Json);
        Assert.Equal(0, productAfter.GetProperty("stock").GetInt32());
    }

    [Fact]
    public async Task GetOrder_Succeeds_WithCorrectToken()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var created = await CreateOrderAsync(product.Id);

        var response = await _client.GetAsync($"/api/orders/{created.Id}?token={created.AccessToken}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_ReturnsNotFound_WithWrongToken()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var created = await CreateOrderAsync(product.Id);

        var response = await _client.GetAsync($"/api/orders/{created.Id}?token={Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOrder_ReturnsNotFound_WithMissingToken()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var created = await CreateOrderAsync(product.Id);

        var response = await _client.GetAsync($"/api/orders/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_RestoresStock_AndMarksCancelled()
    {
        var (_, product) = await _factory.SeedProductAsync(stock: 5);
        var created = await CreateOrderAsync(product.Id, quantity: 2);

        var cancelResponse = await _client.PostAsync(
            $"/api/orders/{created.Id}/cancel?token={created.AccessToken}", content: null);
        Assert.Equal(HttpStatusCode.NoContent, cancelResponse.StatusCode);

        var productAfter = await _client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}", Json);
        Assert.Equal(5, productAfter.GetProperty("stock").GetInt32());
    }

    [Fact]
    public async Task CancelOrder_ReturnsNotFound_WithWrongToken()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var created = await CreateOrderAsync(product.Id);

        var response = await _client.PostAsync(
            $"/api/orders/{created.Id}/cancel?token={Guid.NewGuid()}", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CancelOrder_Fails_WhenOrderAlreadyCancelled()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var created = await CreateOrderAsync(product.Id);

        await _client.PostAsync($"/api/orders/{created.Id}/cancel?token={created.AccessToken}", content: null);
        var secondAttempt = await _client.PostAsync(
            $"/api/orders/{created.Id}/cancel?token={created.AccessToken}", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, secondAttempt.StatusCode);
    }

    private async Task<(int Id, Guid AccessToken)> CreateOrderAsync(int productId, int quantity = 1)
    {
        var response = await _client.PostAsJsonAsync("/api/orders", OrderPayload(productId, quantity));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return (body.GetProperty("id").GetInt32(), body.GetProperty("accessToken").GetGuid());
    }
}

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

// Only covers the access-token gate on CreatePaymentIntent, which runs before any
// Stripe API call. The success path is out of scope here - it would require a real
// Stripe test-mode network call, which doesn't belong in this test suite.
public class PaymentsControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public PaymentsControllerTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task CreatePaymentIntent_ReturnsNotFound_WithWrongToken()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var created = await CreateOrderAsync(product.Id);

        var response = await _client.PostAsync(
            $"/api/payments/create-intent/{created.Id}?token={Guid.NewGuid()}", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreatePaymentIntent_ReturnsNotFound_WithMissingToken()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var created = await CreateOrderAsync(product.Id);

        var response = await _client.PostAsync(
            $"/api/payments/create-intent/{created.Id}", content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(int Id, Guid AccessToken)> CreateOrderAsync(int productId)
    {
        var response = await _client.PostAsJsonAsync("/api/orders", new
        {
            items = new[] { new { productId, quantity = 1 } },
            guestEmail = "buyer@example.com"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        return (body.GetProperty("id").GetInt32(), body.GetProperty("accessToken").GetGuid());
    }
}

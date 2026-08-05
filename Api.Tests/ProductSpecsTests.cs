using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs;

namespace Api.Tests;

public class ProductSpecsTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ProductSpecsTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateAdminToken());
    }

    public void Dispose() => _factory.Dispose();

    private async Task<ProductSpecDto> AddSpecAsync(int productId, string label, string value)
    {
        var response = await _client.PostAsJsonAsync($"/api/products/{productId}/specs", new { label, value });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductSpecDto>(Json))!;
    }

    [Fact]
    public async Task AddSpec_Succeeds_AndAssignsIncreasingSortOrder()
    {
        var (_, product) = await _factory.SeedProductAsync();

        var first = await AddSpecAsync(product.Id, "Wattage", "400W");
        var second = await AddSpecAsync(product.Id, "Weight", "22kg");

        Assert.Equal(0, first.SortOrder);
        Assert.Equal(1, second.SortOrder);
        Assert.Equal("Wattage", first.Label);
        Assert.Equal("400W", first.Value);
    }

    [Fact]
    public async Task AddSpec_ReturnsUnauthorized_WithoutAdminToken()
    {
        var (_, product) = await _factory.SeedProductAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync($"/api/products/{product.Id}/specs", new { label = "Wattage", value = "400W" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSpec_ChangesLabelAndValue()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var spec = await AddSpecAsync(product.Id, "Wattage", "400W");

        var response = await _client.PutAsJsonAsync(
            $"/api/products/{product.Id}/specs/{spec.Id}", new { label = "Rated Power", value = "450W" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var productResponse = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", Json);
        var updated = Assert.Single(productResponse!.Specs);
        Assert.Equal("Rated Power", updated.Label);
        Assert.Equal("450W", updated.Value);
    }

    [Fact]
    public async Task RemoveSpec_Succeeds()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var spec = await AddSpecAsync(product.Id, "Wattage", "400W");

        var response = await _client.DeleteAsync($"/api/products/{product.Id}/specs/{spec.Id}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var productResponse = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", Json);
        Assert.Empty(productResponse!.Specs);
    }

    [Fact]
    public async Task RemoveSpec_ReturnsNotFound_WhenSpecBelongsToADifferentProduct()
    {
        var (_, productA) = await _factory.SeedProductAsync();
        var (_, productB) = await _factory.SeedProductAsync();
        var spec = await AddSpecAsync(productA.Id, "Wattage", "400W");

        var response = await _client.DeleteAsync($"/api/products/{productB.Id}/specs/{spec.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReorderSpecs_UpdatesSortOrderToMatchGivenOrder()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var first = await AddSpecAsync(product.Id, "Wattage", "400W");
        var second = await AddSpecAsync(product.Id, "Weight", "22kg");
        var third = await AddSpecAsync(product.Id, "Warranty", "12 years");

        var response = await _client.PutAsJsonAsync(
            $"/api/products/{product.Id}/specs/reorder",
            new { specIds = new[] { third.Id, first.Id, second.Id } });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var productResponse = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", Json);
        Assert.NotNull(productResponse);
        Assert.Equal([third.Id, first.Id, second.Id], productResponse.Specs.Select(s => s.Id));
    }

    [Fact]
    public async Task ReorderSpecs_Fails_WhenSpecIdsDoNotMatchProductsSpecs()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var spec = await AddSpecAsync(product.Id, "Wattage", "400W");

        var response = await _client.PutAsJsonAsync(
            $"/api/products/{product.Id}/specs/reorder",
            new { specIds = new[] { spec.Id, 999_999 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

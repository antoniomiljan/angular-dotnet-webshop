using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs;

namespace Api.Tests;

public class ProductsControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ProductsControllerTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetProducts_FiltersByCategoryId()
    {
        var (categoryA, _) = await _factory.SeedProductAsync();
        var (categoryB, _) = await _factory.SeedProductAsync();

        var products = await _client.GetFromJsonAsync<List<ProductDto>>(
            $"/api/products?categoryId={categoryA.Id}", Json);

        Assert.NotNull(products);
        Assert.All(products, p => Assert.Equal(categoryA.Id, p.CategoryId));
        Assert.DoesNotContain(products, p => p.CategoryId == categoryB.Id);
    }

    // Regression test: search used to be case-sensitive (EF's default Contains()
    // translation), so a lowercase search for an exact-cased product name like
    // "Wireless Mouse" returned nothing.
    [Fact]
    public async Task GetProducts_SearchIsCaseInsensitive()
    {
        await _factory.SeedProductAsync(name: "Wireless Mouse");

        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products?search=mouse", Json);

        Assert.NotNull(products);
        Assert.Contains(products, p => p.Name == "Wireless Mouse");
    }

    [Fact]
    public async Task GetProducts_ExcludesInactiveProducts()
    {
        var (_, product) = await _factory.SeedProductAsync(active: false);

        var products = await _client.GetFromJsonAsync<List<ProductDto>>("/api/products", Json);

        Assert.NotNull(products);
        Assert.DoesNotContain(products, p => p.Id == product.Id);
    }
}

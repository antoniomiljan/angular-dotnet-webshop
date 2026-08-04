using System.Net;
using System.Net.Http.Headers;

namespace Api.Tests;

public class CategoriesControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public CategoriesControllerTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateAdminToken());
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task DeleteCategory_Succeeds_WhenNoProductsAssigned()
    {
        var category = await _factory.SeedCategoryAsync();

        var response = await _client.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_Fails_WhenActiveProductAssigned()
    {
        var (category, _) = await _factory.SeedProductAsync(active: true);

        var response = await _client.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Regression test for the cascade-delete fix: a category delete must never be
    // allowed to reach a soft-deleted product and cascade into its OrderItem history.
    [Fact]
    public async Task DeleteCategory_Fails_WhenOnlyInactiveProductAssigned()
    {
        var (category, product) = await _factory.SeedProductAsync(active: true);

        var softDelete = await _client.DeleteAsync($"/api/products/{product.Id}");
        Assert.Equal(HttpStatusCode.NoContent, softDelete.StatusCode);

        var response = await _client.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_ReturnsUnauthorized_WithoutAdminToken()
    {
        var category = await _factory.SeedCategoryAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.DeleteAsync($"/api/categories/{category.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Api.DTOs;

namespace Api.Tests;

public class ProductImagesTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ProductImagesTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateAdminToken());
    }

    public void Dispose() => _factory.Dispose();

    private async Task<ProductImageDto> AddImageAsync(int productId, string imageUrl)
    {
        var response = await _client.PostAsJsonAsync($"/api/products/{productId}/images", new { imageUrl });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductImageDto>(Json))!;
    }

    [Fact]
    public async Task AddImage_Succeeds_AndAssignsIncreasingSortOrder()
    {
        var (_, product) = await _factory.SeedProductAsync();

        var first = await AddImageAsync(product.Id, "/images/one.jpg");
        var second = await AddImageAsync(product.Id, "/images/two.jpg");

        Assert.Equal(0, first.SortOrder);
        Assert.Equal(1, second.SortOrder);
    }

    [Fact]
    public async Task AddImage_ReturnsUnauthorized_WithoutAdminToken()
    {
        var (_, product) = await _factory.SeedProductAsync();
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync($"/api/products/{product.Id}/images", new { imageUrl = "/images/one.jpg" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RemoveImage_DeletesTheLocalFile()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var fileName = $"{Guid.NewGuid()}.jpg";
        var filePath = Path.Combine(_factory.WebRootPath, "images", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3 });

        var image = await AddImageAsync(product.Id, $"/images/{fileName}");
        Assert.True(File.Exists(filePath));

        var response = await _client.DeleteAsync($"/api/products/{product.Id}/images/{image.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task RemoveImage_WithExternalUrl_DoesNotThrow()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var image = await AddImageAsync(product.Id, "https://example.com/photo.jpg");

        var response = await _client.DeleteAsync($"/api/products/{product.Id}/images/{image.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RemoveImage_ReturnsNotFound_WhenImageBelongsToADifferentProduct()
    {
        var (_, productA) = await _factory.SeedProductAsync();
        var (_, productB) = await _factory.SeedProductAsync();
        var image = await AddImageAsync(productA.Id, "/images/one.jpg");

        var response = await _client.DeleteAsync($"/api/products/{productB.Id}/images/{image.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReorderImages_UpdatesSortOrderToMatchGivenOrder()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var first = await AddImageAsync(product.Id, "/images/one.jpg");
        var second = await AddImageAsync(product.Id, "/images/two.jpg");
        var third = await AddImageAsync(product.Id, "/images/three.jpg");

        var response = await _client.PutAsJsonAsync(
            $"/api/products/{product.Id}/images/reorder",
            new { imageIds = new[] { third.Id, first.Id, second.Id } });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var productResponse = await _client.GetFromJsonAsync<ProductDto>($"/api/products/{product.Id}", Json);
        Assert.NotNull(productResponse);
        Assert.Equal([third.Id, first.Id, second.Id], productResponse.Images.Select(i => i.Id));
    }

    [Fact]
    public async Task ReorderImages_Fails_WhenImageIdsDoNotMatchProductsImages()
    {
        var (_, product) = await _factory.SeedProductAsync();
        var image = await AddImageAsync(product.Id, "/images/one.jpg");

        var response = await _client.PutAsJsonAsync(
            $"/api/products/{product.Id}/images/reorder",
            new { imageIds = new[] { image.Id, 999_999 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

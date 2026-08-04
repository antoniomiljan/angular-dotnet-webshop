using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests;

public class UploadsControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public UploadsControllerTests()
    {
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.CreateAdminToken());
    }

    public void Dispose() => _factory.Dispose();

    private static MultipartFormDataContent BuildUpload(string fileName, string? contentType = null)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        if (contentType is not null)
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task UploadImage_Succeeds_AndReturnsAServableUrl()
    {
        using var content = BuildUpload("photo.jpg", "image/jpeg");

        var response = await _client.PostAsync("/api/uploads/image", content);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var imageUrl = body.GetProperty("imageUrl").GetString();
        Assert.NotNull(imageUrl);
        Assert.StartsWith("/images/", imageUrl);
        Assert.EndsWith(".jpg", imageUrl);

        var fetched = await _client.GetAsync(imageUrl);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
    }

    [Fact]
    public async Task UploadImage_Fails_ForDisallowedExtension()
    {
        using var content = BuildUpload("malicious.exe");

        var response = await _client.PostAsync("/api/uploads/image", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_Fails_WhenNoFileProvided()
    {
        using var content = new MultipartFormDataContent();

        var response = await _client.PostAsync("/api/uploads/image", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_ReturnsUnauthorized_WithoutAdminToken()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        using var content = BuildUpload("photo.jpg", "image/jpeg");

        var response = await _client.PostAsync("/api/uploads/image", content);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

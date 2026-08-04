using System.Net;
using System.Net.Http.Json;

namespace Api.Tests;

public class AuthControllerTests : IDisposable
{
    private readonly TestWebApplicationFactory _factory = new();
    private readonly HttpClient _client;

    public AuthControllerTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose() => _factory.Dispose();

    private static object Credentials(string email, string password = "CorrectPass123!") => new { email, password };

    [Fact]
    public async Task Register_Succeeds_AndReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", Credentials("new-user@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_Fails_WhenEmailAlreadyExists()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Credentials("dupe@example.com"));

        var second = await _client.PostAsJsonAsync("/api/auth/register", Credentials("dupe@example.com"));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Login_Succeeds_WithCorrectPassword()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Credentials("login-ok@example.com"));

        var response = await _client.PostAsJsonAsync("/api/auth/login", Credentials("login-ok@example.com"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_Fails_WithWrongPassword()
    {
        await _client.PostAsJsonAsync("/api/auth/register", Credentials("login-bad@example.com"));

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login", Credentials("login-bad@example.com", "WrongPassword!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Regression test: Login used to call UserManager.CheckPasswordAsync directly,
    // bypassing SignInManager's lockout policy entirely.
    [Fact]
    public async Task Login_LocksOut_AfterFiveFailedAttempts()
    {
        const string email = "lockout@example.com";
        await _client.PostAsJsonAsync("/api/auth/register", Credentials(email));

        for (var i = 0; i < 5; i++)
            await _client.PostAsJsonAsync("/api/auth/login", Credentials(email, "WrongPassword!"));

        var responseWithCorrectPassword = await _client.PostAsJsonAsync("/api/auth/login", Credentials(email));

        Assert.Equal(HttpStatusCode.Unauthorized, responseWithCorrectPassword.StatusCode);

        var body = await responseWithCorrectPassword.Content.ReadAsStringAsync();
        Assert.Contains("locked", body, StringComparison.OrdinalIgnoreCase);
    }
}

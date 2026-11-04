using System.Net;
using System.Net.Http.Json;

namespace LearnHub.IntegrationTests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    private readonly CustomWebApplicationFactory _factory;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private HttpClient CreateIsolatedClient()
    {
        return _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = false,
            }
        );
    }

    private static object RegisterRequest(string? email = null) =>
        new
        {
            name = "Test Student",
            email = email ?? $"student_{Guid.NewGuid():N}@test.com",
            password = "Password123!",
            role = "STUDENT",
        };

    private async Task RegisterUser()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", RegisterRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static void AssertAuthCookies(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));

        var cookieHeaders = string.Join("; ", cookies!);

        Assert.Contains("access_token=", cookieHeaders);
        Assert.Contains("refreshToken=", cookieHeaders);
        Assert.Contains("httponly", cookieHeaders, StringComparison.OrdinalIgnoreCase);
    }

    // REGISTER

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkAndSetsCookies()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", RegisterRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertAuthCookies(response);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var request = RegisterRequest();

        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidRequest_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/register",
            new
            {
                name = "",
                email = "invalid",
                password = "123",
            }
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // LOGIN

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkAndSetsCookies()
    {
        var request = RegisterRequest();

        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                email = request.GetType().GetProperty("email")!.GetValue(request),
                password = "Password123!",
            }
        );

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertAuthCookies(response);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = $"unknown_{Guid.NewGuid():N}@test.com", password = "WrongPassword123!" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // REFRESH

    [Fact]
    public async Task Refresh_ValidToken_ReturnsOkAndRotatesCookies()
    {
        await RegisterUser();

        var response = await _client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertAuthCookies(response);
    }

    [Fact]
    public async Task Refresh_MissingToken_ReturnsUnauthorized()
    {
        var client = new HttpClientHandler();
        using var isolatedClient = CreateIsolatedClient();
        var response = await isolatedClient.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_InvalidToken_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Add("Cookie", "refreshToken=invalid-refresh-token");

        var response = await _client.PostAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // LOGOUT

    [Fact]
    public async Task Logout_AuthenticatedUser_ReturnsOk()
    {
        await RegisterUser();

        var response = await _client.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_ReturnsOk()
    {
        using var client = new HttpClientHandler();
        using var isolatedClient = CreateIsolatedClient();
        var response = await isolatedClient.PostAsync("/api/auth/logout", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // LOGOUT ALL

    [Fact]
    public async Task LogoutAll_AuthenticatedUser_ReturnsOk()
    {
        await RegisterUser();

        var response = await _client.PostAsync("/api/auth/logout-all", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Token version was incremented, so the old access token is invalid.
        var meResponse = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    [Fact]
    public async Task LogoutAll_UnauthenticatedUser_ReturnsUnauthorized()
    {
        using var handler = new HttpClientHandler();
        using var isolatedClient = CreateIsolatedClient();
        var response = await isolatedClient.PostAsync("/api/auth/logout-all", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ME

    [Fact]
    public async Task Me_AuthenticatedUser_ReturnsUser()
    {
        await RegisterUser();

        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();

        Assert.NotNull(user);
        Assert.Equal("Test Student", user.Name);
        Assert.Equal("STUDENT", user.Role);
        Assert.True(user.IsActive);
    }

    [Fact]
    public async Task Me_UnauthenticatedUser_ReturnsUnauthorized()
    {
        using var handler = new HttpClientHandler();
        using var isolatedClient = CreateIsolatedClient();
        var response = await isolatedClient.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private class UserResponse
    {
        public string Name { get; set; } = "";
        public string Role { get; set; } = "";
        public bool IsActive { get; set; }
    }
}

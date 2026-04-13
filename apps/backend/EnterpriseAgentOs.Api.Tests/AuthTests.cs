using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;

namespace EnterpriseAgentOs.Api.Tests;

public sealed class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task GetMe_WithoutCookie_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Not authenticated", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task GetMe_WithInvalidSessionToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "eaos-session=totally-bogus-token");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithExpiredSession_Returns401()
    {
        var client = await TestHelpers.CreateExpiredSessionClientAsync(_factory);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidSession_ReturnsUserInfo()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory, "authed@example.com", "Auth User");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("authed@example.com", body.GetProperty("email").GetString());
        Assert.Equal("Auth User", body.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Logout_DeletesSession_SubsequentMeReturns401()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory, "logout@example.com");

        // Verify logged in
        var meResp = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);

        // Logout
        var logoutResp = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.OK, logoutResp.StatusCode);

        // The server deletes the session from DB. Even with the old cookie,
        // the middleware won't find the session.
        var meAfter = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfter.StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_RedirectsToGoogleOAuth()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/api/auth/google");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var location = response.Headers.Location!.ToString();
        Assert.Contains("accounts.google.com/o/oauth2/v2/auth", location);
        Assert.Contains("client_id=", location);
        Assert.Contains("scope=openid", location);
    }
}

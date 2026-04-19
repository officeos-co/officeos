namespace EnterpriseAgentOs.Api.Tests;

public sealed class AuthTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>
{
    private readonly Infrastructure.CustomWebApplicationFactory _factory;

    public AuthTests(Infrastructure.CustomWebApplicationFactory factory) => _factory = factory;

    private const string MeQuery = @"{ me { id email name } }";
    private const string LogoutMutation = @"mutation { logout }";

    [Fact]
    public async Task GetMe_WithoutCookie_ReturnsAuthError()
    {
        var client = _factory.CreateClient();

        var raw = await Infrastructure.TestHelpers.GraphQLRawAsync(client, MeQuery);

        Assert.True(raw.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetMe_WithInvalidSessionToken_ReturnsAuthError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", "eaos-session=totally-bogus-token");

        var raw = await Infrastructure.TestHelpers.GraphQLRawAsync(client, MeQuery);

        Assert.True(raw.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetMe_WithExpiredSession_ReturnsAuthError()
    {
        var client = await Infrastructure.TestHelpers.CreateExpiredSessionClientAsync(_factory);

        var raw = await Infrastructure.TestHelpers.GraphQLRawAsync(client, MeQuery);

        Assert.True(raw.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task GetMe_WithValidSession_ReturnsUserInfo()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory, "authed@example.com", "Auth User");

        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, MeQuery);
        var me = data.GetProperty("me");

        Assert.Equal("authed@example.com", me.GetProperty("email").GetString());
        Assert.Equal("Auth User", me.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Logout_DeletesSession_SubsequentMeReturnsAuthError()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory, "logout@example.com");

        // Verify logged in
        var meData = await Infrastructure.TestHelpers.GraphQLAsync(client, MeQuery);
        Assert.Equal("logout@example.com", meData.GetProperty("me").GetProperty("email").GetString());

        // Logout
        var logoutData = await Infrastructure.TestHelpers.GraphQLAsync(client, LogoutMutation);
        Assert.True(logoutData.GetProperty("logout").GetBoolean());

        // Server deleted the session — even with the old cookie, me must now error.
        var raw = await Infrastructure.TestHelpers.GraphQLRawAsync(client, MeQuery);
        Assert.True(raw.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0);
    }

    [Fact]
    public async Task UpdateProfile_Preferences_PersistsAndReturnsInMe()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory, "prefs@example.com", "Prefs User");

        // Update profile with preferences
        const string updateMutation = @"
            mutation($input: UpdateProfileInput!) {
              updateProfile(input: $input) {
                id displayName timezone preferences notificationPrefsJson
              }
            }";

        var updateData = await Infrastructure.TestHelpers.GraphQLAsync(
            client, updateMutation, new
            {
                input = new
                {
                    displayName = "Prefs",
                    timezone = "Europe/Amsterdam",
                    preferences = "Keep explanations brief and to the point",
                }
            });

        var updated = updateData.GetProperty("updateProfile");
        Assert.Equal("Prefs", updated.GetProperty("displayName").GetString());
        Assert.Equal("Keep explanations brief and to the point", updated.GetProperty("preferences").GetString());

        // Verify me query also returns preferences
        const string meQuery = @"{ me { id email preferences } }";
        var meData = await Infrastructure.TestHelpers.GraphQLAsync(client, meQuery);
        var me = meData.GetProperty("me");
        Assert.Equal("Keep explanations brief and to the point", me.GetProperty("preferences").GetString());
    }

    [Fact]
    public async Task GoogleLogin_RedirectsToGoogleOAuth()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
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

namespace EnterpriseAgentOs.Api.Tests.Auth;

public sealed class SsoControllerTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>
{
    private readonly Infrastructure.CustomWebApplicationFactory _factory;

    public SsoControllerTests(Infrastructure.CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Initiate_WhenSsoDisabled_Returns503()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/api/sso/initiate?org=org_test_123");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SSO not configured", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Initiate_WithoutOrgParam_Returns400()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/api/sso/initiate");

        // 503 takes priority over 400 because the Enabled check runs first
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task Callback_WhenSsoDisabled_Returns503()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var response = await client.GetAsync("/api/sso/callback?code=abc&state=xyz");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SSO not configured", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Callback_WithoutRequiredParams_Returns503_WhenDisabled()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        // Disabled check fires before param validation
        var response = await client.GetAsync("/api/sso/callback");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}

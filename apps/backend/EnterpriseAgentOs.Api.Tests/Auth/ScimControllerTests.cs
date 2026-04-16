namespace EnterpriseAgentOs.Api.Tests.Auth;

public sealed class ScimControllerTests : IClassFixture<EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory>
{
    private readonly EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory _factory;

    public ScimControllerTests(EnterpriseAgentOs.Api.Tests.Infrastructure.CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ProvisionUser_WhenScimDisabled_Returns503()
    {
        var client = _factory.CreateClient();

        var payload = new { externalId = "ext_001", email = "user@corp.com", displayName = "Corp User" };
        var response = await client.PostAsJsonAsync("/api/scim/v2/Users", payload);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SSO not configured", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task DeprovisionUser_WhenScimDisabled_Returns503()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/scim/v2/Users/ext_001");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("SSO not configured", body.GetProperty("error").GetString());
    }
}

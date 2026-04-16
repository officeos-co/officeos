namespace EnterpriseAgentOs.Api.Tests.Auth;

public sealed class WorkOsAuthServiceTests
{
    private static EnterpriseAgentOs.Api.Entities.Sso.WorkOsAuthService BuildService(bool enabled = true)
    {
        var config = new EnterpriseAgentOs.Api.Properties.WorkOsConfig
        {
            ApiKey = "sk_test_placeholder",
            ClientId = "client_placeholder",
            RedirectUri = "https://api.officeos.co/api/sso/callback",
            Enabled = enabled,
        };

        return new EnterpriseAgentOs.Api.Entities.Sso.WorkOsAuthService(config, NullLogger<EnterpriseAgentOs.Api.Entities.Sso.WorkOsAuthService>.Instance);
    }

    [Fact]
    public async Task InitiateSsoAsync_ThrowsNotImplementedException()
    {
        var service = BuildService();

        await Assert.ThrowsAsync<NotImplementedException>(
            () => service.InitiateSsoAsync("org_test_123"));
    }

    [Fact]
    public async Task HandleCallbackAsync_ThrowsNotImplementedException()
    {
        var service = BuildService();

        await Assert.ThrowsAsync<NotImplementedException>(
            () => service.HandleCallbackAsync("code_abc", "state_xyz"));
    }

    [Fact]
    public async Task HandleScimProvisionAsync_ThrowsNotImplementedException()
    {
        var service = BuildService();
        var evt = new EnterpriseAgentOs.Api.Entities.Sso.ScimEvent("provision", "ext_123", "user@example.com", "Test User");

        await Assert.ThrowsAsync<NotImplementedException>(
            () => service.HandleScimProvisionAsync(evt));
    }

    [Fact]
    public async Task HandleScimDeprovisionAsync_ThrowsNotImplementedException()
    {
        var service = BuildService();
        var evt = new EnterpriseAgentOs.Api.Entities.Sso.ScimEvent("deprovision", "ext_123", null, null);

        await Assert.ThrowsAsync<NotImplementedException>(
            () => service.HandleScimProvisionAsync(evt));
    }
}

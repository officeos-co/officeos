namespace EnterpriseAgentOs.Infrastructure.Auth;

internal sealed class WorkOsAuthService : IWorkOsAuthService
{
    private readonly WorkOsConfig _workOsConfig;
    private readonly ILogger<WorkOsAuthService> _logger;

    public WorkOsAuthService(WorkOsConfig config, ILogger<WorkOsAuthService> logger)
    {
        _workOsConfig = config;
        _logger = logger;
    }

    public Task<string> InitiateSsoAsync(string organizationId, CancellationToken ct = default)
    {
        // TODO: Call WorkOS /authorize endpoint with organizationId and return the redirect URL.
        // Requires WorkOs.ApiKey and WorkOs.ClientId to be set.
        throw new NotImplementedException(
            "WorkOS SSO initiation is not implemented yet. Set WorkOs.Enabled = true and configure API credentials.");
    }

    public Task<WorkOsUserInfo> HandleCallbackAsync(string code, string state, CancellationToken ct = default)
    {
        // TODO: Exchange the authorization code for a WorkOS profile via POST /sso/token.
        // Validate state, extract profile, upsert the local user record, and issue a session.
        throw new NotImplementedException(
            "WorkOS SSO callback handling is not implemented yet.");
    }

    public Task HandleScimProvisionAsync(ScimEvent evt, CancellationToken ct = default)
    {
        // TODO: Handle SCIM 2.0 user lifecycle events (provision / deprovision).
        // Verify the SCIM bearer token against WorkOs.ApiKey before processing.
        _logger.LogInformation("SCIM event received: {EventType} for external ID {ExternalId}",
            evt.EventType, evt.ExternalId);

        throw new NotImplementedException(
            "WorkOS SCIM provisioning is not implemented yet.");
    }
}

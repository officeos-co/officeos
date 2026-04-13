namespace EnterpriseAgentOs.Api.Entities.Sso;

public interface IWorkOsAuthService
{
    Task<string> InitiateSsoAsync(string organizationId, CancellationToken ct = default);
    Task<WorkOsUserInfo> HandleCallbackAsync(string code, string state, CancellationToken ct = default);
    Task HandleScimProvisionAsync(ScimEvent evt, CancellationToken ct = default);
}

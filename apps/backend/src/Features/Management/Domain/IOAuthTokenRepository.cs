namespace EnterpriseAgentOs.Domain.Features.Management;

public interface IOAuthTokenRepository
{
    Task<OAuthTokenRecord?> GetByAsync(OAuthTokenFilter filter, CancellationToken ct = default);
    Task UpsertAsync(OAuthTokenRecord token, CancellationToken ct = default);
}

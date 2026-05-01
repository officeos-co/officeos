namespace EnterpriseAgentOs.Domain.Features.Management;

public interface IOAuthTokenRepository
{
    Task<OAuthTokenRecord?> GetByProviderAsync(string provider, CancellationToken ct = default);
    Task UpsertAsync(OAuthTokenRecord token, CancellationToken ct = default);
}

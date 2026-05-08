namespace EnterpriseAgentOs.Domain.Features.Management;

public sealed record OAuthTokenFilter
{
    public Guid? Id { get; init; }
    public Guid? UserId { get; init; }
    public string? Provider { get; init; }
    public string? Email { get; init; }
}

public interface IOAuthTokenRepository
{
    Task<OAuthTokenRecord?> GetByAsync(OAuthTokenFilter filter, CancellationToken ct = default);
    Task UpsertAsync(OAuthTokenRecord token, CancellationToken ct = default);
}

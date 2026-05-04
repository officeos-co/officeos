namespace EnterpriseAgentOs.Domain.Features.Management;

public sealed record SessionFilter
{
    public Guid? Id { get; init; }
    public Guid? UserId { get; init; }
    public string? TokenHash { get; init; }
}

public interface ISessionRepository
{
    Task<SessionRecord> CreateAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken ct = default);
    Task<SessionRecord?> GetByAsync(SessionFilter filter, CancellationToken ct = default);
    Task DeleteAsync(string tokenHash, CancellationToken ct = default);
    Task PurgeExpiredAsync(CancellationToken ct = default);
    Task DeleteByUserIdAsync(Guid userId, CancellationToken ct = default);
}

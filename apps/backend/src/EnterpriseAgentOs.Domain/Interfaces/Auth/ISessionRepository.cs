namespace EnterpriseAgentOs.Domain.Interfaces.Auth;

public interface ISessionRepository
{
    Task<EnterpriseAgentOs.Domain.Models.SessionRecord> CreateAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.SessionRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task DeleteAsync(string tokenHash, CancellationToken ct = default);
    Task PurgeExpiredAsync(CancellationToken ct = default);
}

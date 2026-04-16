namespace EnterpriseAgentOs.Api.Entities.Auth;

public interface ISessionRepository
{
    Task<EnterpriseAgentOs.Api.Database.Models.SessionRecord> CreateAsync(Guid userId, string tokenHash, DateTime expiresAt, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.SessionRecord?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task DeleteAsync(string tokenHash, CancellationToken ct = default);
    Task PurgeExpiredAsync(CancellationToken ct = default);
}

namespace EnterpriseAgentOs.Domain.Interfaces.Auth;

public interface IUserRepository
{
    Task<EnterpriseAgentOs.Domain.Models.UserRecord> UpsertByGoogleSubjectAsync(string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.UserRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.UserRecord> UpdateProfileAsync(
        Guid id,
        string? name,
        string? displayName,
        string? timezone,
        string? notificationPrefsJson,
        string? preferences,
        CancellationToken ct = default);
}

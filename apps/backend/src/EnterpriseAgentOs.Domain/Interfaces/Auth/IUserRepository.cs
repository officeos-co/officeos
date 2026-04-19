namespace EnterpriseAgentOs.Domain.Interfaces.Auth;

public interface IUserRepository
{
    Task<UserRecord> UpsertByGoogleSubjectAsync(string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct = default);
    Task<UserRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserRecord> UpdateProfileAsync(
        Guid id,
        string? name,
        string? displayName,
        string? timezone,
        string? notificationPrefsJson,
        string? preferences,
        CancellationToken ct = default);
}

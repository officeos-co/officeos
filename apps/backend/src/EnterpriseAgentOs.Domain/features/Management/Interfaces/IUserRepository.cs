namespace EnterpriseAgentOs.Domain.Features.Management;

public interface IUserRepository
{
    Task<UserRecord> UpsertByGoogleSubjectAsync(string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct = default);
    Task<UserRecord> UpsertByGitHubSubjectAsync(string gitHubSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct = default);
    Task<UserRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserRecord> UpdateProfileAsync(
        Guid id,
        string? name,
        string? displayName,
        string? timezone,
        string? notificationPrefsJson,
        string? preferences,
        CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

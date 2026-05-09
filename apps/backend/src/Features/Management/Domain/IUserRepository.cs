namespace EnterpriseAgentOs.Domain.Features.Management;

public sealed record UserFilter
{
    public Guid? Id { get; init; }
    public string? Email { get; init; }
    public string? GoogleSubjectId { get; init; }
    public string? GitHubSubjectId { get; init; }
}

public interface IUserRepository
{
    Task<UserRecord> UpsertByGoogleSubjectAsync(string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct = default);
    Task<UserRecord> UpsertByGitHubSubjectAsync(string gitHubSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct = default);
    Task<UserRecord?> GetByAsync(UserFilter filter, CancellationToken ct = default);
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

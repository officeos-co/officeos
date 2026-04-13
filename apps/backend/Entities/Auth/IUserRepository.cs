namespace EnterpriseAgentOs.Api.Entities.Auth;

public interface IUserRepository
{
    Task<UserRecord> UpsertByGoogleSubjectAsync(string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct = default);
    Task<UserRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

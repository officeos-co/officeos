namespace EnterpriseAgentOs.Api.Entities.Auth;

public interface IUserRepository
{
    Task<EnterpriseAgentOs.Api.Database.Models.UserRecord> UpsertByGoogleSubjectAsync(string googleSubjectId, string email, string? name, string? avatarUrl, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Database.Models.UserRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
}

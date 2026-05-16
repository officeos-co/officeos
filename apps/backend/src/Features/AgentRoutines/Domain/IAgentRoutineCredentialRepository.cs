namespace OffceOs.Domain.Features.AgentRoutines;

public interface IAgentRoutineCredentialRepository
{
    Task<IReadOnlyList<AgentRoutineCredentialRecord>> ListAsync(Guid workspaceId, CancellationToken ct = default);
    Task<AgentRoutineCredentialRecord?> GetByNameAsync(Guid workspaceId, string name, CancellationToken ct = default);
    Task<AgentRoutineCredentialRecord> UpsertAsync(AgentRoutineCredentialRecord record, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid workspaceId, string name, CancellationToken ct = default);
    Task MarkUsedAsync(Guid id, DateTime usedAt, CancellationToken ct = default);
}

public sealed class AgentRoutineCredentialRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OwnerId { get; init; }
    public Guid WorkspaceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty;
    public string AuthKind { get; init; } = string.Empty;
    public string EncryptedSecret { get; init; } = string.Empty;
    public string? PublicMetadataJson { get; init; }
    public string? ScopesJson { get; init; }
    public DateTime? ExpiresAtUtc { get; init; }
    public DateTime? LastUsedAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

public static class AgentRoutineCredentialAuthKinds
{
    public const string OAuth = "oauth";
    public const string PersonalAccessToken = "personal_access_token";
}

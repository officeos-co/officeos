namespace OffceOs.Application.Features.Agents;

public sealed record DeclarativeManifestRequest(string Manifest);

public sealed record DeclarativeManifestValidationResult(
    bool Valid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Resources);

public sealed record DeclarativeManifestDiffResult(
    IReadOnlyList<DeclarativeAgentChangeItem> Changes);

public sealed record DeclarativeManifestApplyResult(
    IReadOnlyList<DeclarativeAgentChangeItem> Changes);

public sealed record DeclarativeAgentChangeItem(
    string Kind,
    string Name,
    string Action,
    string? AgentId,
    string? Message);

public interface IDeclarativeAgentService
{
    Task<DeclarativeManifestValidationResult> ValidateAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<DeclarativeManifestDiffResult> DiffAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<DeclarativeManifestApplyResult> ApplyAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<string?> ExportAgentAsync(string name, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
}

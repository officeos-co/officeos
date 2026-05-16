namespace OffceOs.Application.Features.AgentDefinitions;

public sealed record DeclarativeManifestValidationResult(
    bool Valid,
    IReadOnlyList<DeclarativeValidationErrorItem> Errors,
    IReadOnlyList<string> Resources);

public sealed record DeclarativeManifestDiffResult(
    IReadOnlyList<DeclarativeResourceChangeItem> Changes);

public sealed record DeclarativeManifestApplyResult(
    IReadOnlyList<DeclarativeResourceChangeItem> Changes);

public sealed record DeclarativeValidationErrorItem(
    string Kind,
    string Name,
    string Message);

public sealed record DeclarativeResourceChangeItem(
    string Kind,
    string Name,
    string Action,
    string? ResourceId,
    string? Message);

public interface IDeclarativeAgentService
{
    Task<DeclarativeManifestValidationResult> ValidateAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<DeclarativeManifestDiffResult> DiffAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<DeclarativeManifestApplyResult> ApplyAsync(string manifest, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<string> ExportWorkspaceAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<string?> ExportAgentAsync(string name, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
}

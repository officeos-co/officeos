using System.Text.Json;

namespace EnterpriseAgentOs.Domain.Features.Atlas;

public sealed record CreateAtlasGitHubConnectionRequest(
    string WorkspaceName,
    string DisplayName,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Entities,
    Guid CreatedById);

public sealed record UpdateAtlasGitHubConnectionRequest(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Entities);

public sealed record AtlasConnectorExecuteRequest(
    Guid SourceId,
    string Entity,
    string Action,
    JsonElement Params,
    IReadOnlyList<string>? SelectFields);

public interface IAtlasService
{
    Task<IReadOnlyList<AtlasConnectorTypeRecord>> ListConnectorTypesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AtlasConnectorConnectionRecord>> ListAsync(AtlasConnectionFilter filter, CancellationToken ct = default);
    Task<AtlasConnectorConnectionRecord?> GetByAsync(AtlasConnectionFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AtlasActivityRecord>> ListAsync(AtlasActivityFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AtlasRequestHistoryRecord>> ListAsync(AtlasRequestHistoryFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<AtlasIndexJobRecord>> ListAsync(AtlasIndexJobFilter filter, CancellationToken ct = default);
    Task<AtlasIndexedRecordRecord?> GetByAsync(AtlasIndexedRecordFilter filter, CancellationToken ct = default);
    Task<AtlasIndexedRecordPage> SearchAsync(AtlasIndexedRecordFilter filter, CancellationToken ct = default);
    Task<AtlasConnectorConnectionRecord> CreateGitHubConnectionAsync(CreateAtlasGitHubConnectionRequest request, CancellationToken ct = default);
    Task<AtlasConnectorConnectionRecord> UpdateGitHubConnectionAsync(UpdateAtlasGitHubConnectionRequest request, CancellationToken ct = default);
    Task DeleteConnectionAsync(Guid id, CancellationToken ct = default);
    Task<AtlasIndexJobRecord> StartIndexAsync(Guid connectionId, CancellationToken ct = default);
}

public interface IAtlasConnectorExecutionService
{
    Task<JsonElement> ExecuteAsync(AtlasConnectorExecuteRequest request, CancellationToken ct = default);
}

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
    Task<IReadOnlyList<AtlasConnectorConnectionRecord>> ListConnectionsAsync(CancellationToken ct = default);
    Task<AtlasConnectorConnectionRecord?> GetConnectionAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<AtlasActivityRecord>> ListActivityAsync(Guid? connectionId, CancellationToken ct = default);
    Task<IReadOnlyList<AtlasRequestHistoryRecord>> ListHistoryAsync(Guid? connectionId, CancellationToken ct = default);
    Task<IReadOnlyList<AtlasIndexJobRecord>> ListIndexJobsAsync(Guid connectionId, int limit = 20, CancellationToken ct = default);
    Task<AtlasIndexedRecordPage> SearchRecordsAsync(AtlasIndexedRecordFilter filter, CancellationToken ct = default);
    Task<AtlasConnectorConnectionRecord> CreateGitHubConnectionAsync(CreateAtlasGitHubConnectionRequest request, CancellationToken ct = default);
    Task<AtlasConnectorConnectionRecord> UpdateGitHubConnectionAsync(UpdateAtlasGitHubConnectionRequest request, CancellationToken ct = default);
    Task DeleteConnectionAsync(Guid id, CancellationToken ct = default);
    Task<AtlasIndexJobRecord> StartIndexAsync(Guid connectionId, CancellationToken ct = default);
}

public interface IAtlasConnectorExecutionService
{
    Task<JsonElement> ExecuteAsync(AtlasConnectorExecuteRequest request, CancellationToken ct = default);
}

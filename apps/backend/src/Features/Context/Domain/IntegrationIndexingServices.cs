using System.Text.Json;

namespace EnterpriseAgentOs.Domain.Features.Context;

public sealed record CreateGitHubIntegrationConnectionRequest(
    string WorkspaceName,
    string DisplayName,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Entities,
    Guid CreatedById);

public sealed record UpdateGitHubIntegrationConnectionRequest(
    Guid Id,
    string DisplayName,
    IReadOnlyList<string> Repositories,
    IReadOnlyList<string> Entities);

public sealed record IntegrationExecuteRequest(
    Guid SourceId,
    string Entity,
    string Action,
    JsonElement Params,
    IReadOnlyList<string>? SelectFields);

public interface IIntegrationConnectionService
{
    Task<IReadOnlyList<IntegrationDefinitionRecord>> ListIntegrationDefinitionsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationConnectionRecord>> ListAsync(IntegrationConnectionFilter filter, CancellationToken ct = default);
    Task<IntegrationConnectionRecord?> GetByAsync(IntegrationConnectionFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationActivityRecord>> ListAsync(IntegrationActivityFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationRequestHistoryRecord>> ListAsync(IntegrationRequestHistoryFilter filter, CancellationToken ct = default);
    Task<IReadOnlyList<IntegrationIndexJobRecord>> ListAsync(IntegrationIndexJobFilter filter, CancellationToken ct = default);
    Task<IntegrationIndexedRecordRecord?> GetByAsync(IntegrationIndexedRecordFilter filter, CancellationToken ct = default);
    Task<IntegrationIndexedRecordPage> SearchAsync(IntegrationIndexedRecordFilter filter, CancellationToken ct = default);
    Task<IntegrationConnectionRecord> CreateGitHubConnectionAsync(CreateGitHubIntegrationConnectionRequest request, CancellationToken ct = default);
    Task<IntegrationConnectionRecord> UpdateGitHubConnectionAsync(UpdateGitHubIntegrationConnectionRequest request, CancellationToken ct = default);
    Task DeleteConnectionAsync(Guid id, CancellationToken ct = default);
    Task<IntegrationIndexJobRecord> StartIndexAsync(Guid connectionId, CancellationToken ct = default);
}

public interface IIntegrationExecutionService
{
    Task<JsonElement> ExecuteAsync(IntegrationExecuteRequest request, CancellationToken ct = default);
}

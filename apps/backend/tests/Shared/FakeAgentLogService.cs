using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Observability;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Observability;

namespace OffceOs.Tests.Shared;

public sealed class FakeAgentLogService : IAgentLogService
{
    public IQueryable<AgentLogProjection> AgentLogs(Guid agentId, Guid? workspaceId = null) =>
        Enumerable.Empty<AgentLogProjection>().AsQueryable();

    public IQueryable<AgentLogProjection> ChannelLogs(Guid channelConnectionId, Guid? workspaceId = null) =>
        Enumerable.Empty<AgentLogProjection>().AsQueryable();

    public IQueryable<AgentLogProjection> GlobalLogs(GlobalLogFiltersRequest filters, Guid? workspaceId = null) =>
        Enumerable.Empty<AgentLogProjection>().AsQueryable();

    public IQueryable<AuditEntry> AuditLog(Guid agentId, Guid? workspaceId = null) =>
        Enumerable.Empty<AuditEntry>().AsQueryable();

    public Task<List<AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default) =>
        Task.FromResult(new List<AgentLogRecord>());

    public Task<List<AgentLogRecord>> ListForRunAsync(Guid runId, Guid workspaceId, int limit, CancellationToken ct = default) =>
        Task.FromResult(new List<AgentLogRecord>());

    public Task<List<AgentLogRecord>> ListForChannelConnectionAsync(Guid channelConnectionId, DateTime? before, int limit, CancellationToken ct = default) =>
        Task.FromResult(new List<AgentLogRecord>());

    public Task<List<AgentLogRecord>> ListForResourceAsync(ResourceLogQueryRequest request, CancellationToken ct = default) =>
        Task.FromResult(new List<AgentLogRecord>());

    public Task<string?> GetLastRelevantMessageForAgentAsync(Guid agentId, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);

    public Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesForAgentsAsync(IReadOnlyCollection<Guid> agentIds, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string?>>(
            agentIds.Distinct().ToDictionary(agentId => agentId, _ => (string?)null));

    public Task<string?> GetLastRelevantMessageForChannelConnectionAsync(Guid channelConnectionId, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);

    public Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesForChannelConnectionsAsync(IReadOnlyCollection<Guid> channelConnectionIds, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, string?>>(
            channelConnectionIds.Distinct().ToDictionary(channelConnectionId => channelConnectionId, _ => (string?)null));

    public Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersRequest filters, CancellationToken ct = default) =>
        Task.FromResult(new GlobalLogsPage([], 0));

    public Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default) =>
        Task.FromResult(record);

    public Task<AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default) =>
        Task.FromResult(AgentLogRecord.MessageIn(agentId, content));

    public Task RecordToolCallAsync(
        Guid agentId,
        Guid? userId,
        string skillName,
        string action,
        string paramsJson,
        string? resultSummary,
        long durationMs,
        CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<(List<AgentLogRecord> Items, int Total)> GetAuditLogAsync(Guid agentId, int limit, int offset, CancellationToken ct = default) =>
        Task.FromResult((new List<AgentLogRecord>(), 0));

    public Task<Dictionary<string, AgentLogRecord>> GetResultsByCorrelationAsync(Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default) =>
        Task.FromResult(new Dictionary<string, AgentLogRecord>());
}

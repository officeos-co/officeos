using OffceOs.Application.Features.Observability;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Observability;

namespace OffceOs.Tests.Shared;

public sealed class FakeAgentLogService : IAgentLogService
{
    private readonly List<AgentLogRecord> _records = [];

    public IReadOnlyList<AgentLogRecord> Records => _records;

    public Task<AgentLogPage> ListAsync(AgentLogQueryRequest request, CancellationToken ct = default) =>
        Task.FromResult(new AgentLogPage([], 0));

    public Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(LastRelevantLogQueryRequest request, CancellationToken ct = default)
    {
        var ids = (request.AgentIds ?? []).Concat(request.ChannelConnectionIds ?? []).Distinct();
        return Task.FromResult<IReadOnlyDictionary<Guid, string?>>(
            ids.ToDictionary(id => id, _ => (string?)null));
    }

    public Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default)
    {
        _records.Add(record);
        return Task.FromResult(record);
    }

    public Task<AgentLogRecord> QueueWorkAsync(QueueAgentWorkRequest request, CancellationToken ct = default)
    {
        var existing = _records.FirstOrDefault(record =>
            record.AgentId == request.AgentId &&
            record.CorrelationId == request.CorrelationId &&
            record.Type == AgentLogType.MessageIn);
        if (existing is not null)
            return Task.FromResult(existing);

        var record = new AgentLogRecord
        {
            AgentId = request.AgentId,
            WorkspaceId = request.WorkspaceId,
            Type = AgentLogType.MessageIn,
            Content = request.Content,
            CorrelationId = request.CorrelationId,
            WorkStatus = AgentWorkStatusKinds.Queued,
            WorkPurpose = AgentWorkPurposeKinds.Normalize(request.Purpose),
            DefinitionId = request.DefinitionId,
            Time = request.Time ?? DateTime.UtcNow,
        };
        _records.Add(record);
        return Task.FromResult(record);
    }

    public Task<AgentLogRecord?> ClaimNextQueuedWorkAsync(CancellationToken ct = default)
    {
        var runningAgentIds = _records
            .Where(record => record.WorkStatus == AgentWorkStatusKinds.Running && record.AgentId.HasValue)
            .Select(record => record.AgentId!.Value)
            .ToHashSet();
        var queued = _records
            .Where(record => record.Type == AgentLogType.MessageIn
                && record.WorkStatus == AgentWorkStatusKinds.Queued
                && record.AgentId.HasValue
                && !runningAgentIds.Contains(record.AgentId.Value))
            .OrderBy(record => record.Time)
            .FirstOrDefault();
        if (queued is null)
            return Task.FromResult<AgentLogRecord?>(null);

        var claimed = CopyWork(queued, AgentWorkStatusKinds.Running, null);
        _records.Remove(queued);
        _records.Add(claimed);
        return Task.FromResult<AgentLogRecord?>(claimed);
    }

    public Task CompleteWorkAsync(Guid workLogId, CancellationToken ct = default)
    {
        MarkWork(workLogId, AgentWorkStatusKinds.Completed, null);
        return Task.CompletedTask;
    }

    public Task FailWorkAsync(Guid workLogId, string error, CancellationToken ct = default)
    {
        MarkWork(workLogId, AgentWorkStatusKinds.Failed, error);
        return Task.CompletedTask;
    }

    private void MarkWork(Guid workLogId, string status, string? error)
    {
        var existing = _records.FirstOrDefault(record => record.Id == workLogId);
        if (existing is null)
            return;

        var updated = CopyWork(existing, status, error);
        _records.Remove(existing);
        _records.Add(updated);
    }

    private static AgentLogRecord CopyWork(AgentLogRecord record, string status, string? error) => new()
    {
        Id = record.Id,
        ResourceKind = record.ResourceKind,
        ResourceId = record.ResourceId,
        ResourceName = record.ResourceName,
        ParentResourceKind = record.ParentResourceKind,
        ParentResourceId = record.ParentResourceId,
        AgentId = record.AgentId,
        WorkspaceId = record.WorkspaceId,
        Agent = record.Agent,
        Time = record.Time,
        Type = record.Type,
        Severity = record.Severity,
        Tool = record.Tool,
        Integration = record.Integration,
        Channel = record.Channel,
        ChannelConnectionId = record.ChannelConnectionId,
        Content = record.Content,
        Usage = record.Usage,
        MetadataJson = record.MetadataJson,
        CorrelationId = record.CorrelationId,
        WorkStatus = status,
        WorkPurpose = record.WorkPurpose,
        DefinitionId = record.DefinitionId,
        StartedAt = record.StartedAt ?? DateTime.UtcNow,
        CompletedAt = status is AgentWorkStatusKinds.Completed or AgentWorkStatusKinds.Failed ? DateTime.UtcNow : record.CompletedAt,
        WorkError = error,
    };
}

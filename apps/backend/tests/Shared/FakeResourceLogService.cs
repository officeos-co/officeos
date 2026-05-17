using OffceOs.Features.AgentHarness.Application;
using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.ResourceLogs.Domain;

namespace OffceOs.Tests.Shared;

public sealed class FakeResourceLogService : IResourceLogService, IAgentWorkQueueService
{
    private readonly List<ResourceLogRecord> _records = [];

    public IReadOnlyList<ResourceLogRecord> Records => _records;

    public Task<ResourceLogPage> ListAsync(ResourceLogQueryRequest request, CancellationToken ct = default) =>
        Task.FromResult(new ResourceLogPage([], 0));

    public Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(LastRelevantLogQueryRequest request, CancellationToken ct = default)
    {
        var ids = (request.AgentIds ?? []).Concat(request.ChannelConnectionIds ?? []).Distinct();
        return Task.FromResult<IReadOnlyDictionary<Guid, string?>>(
            ids.ToDictionary(id => id, _ => (string?)null));
    }

    public Task<ResourceLogRecord> AppendAsync(ResourceLogRecord record, CancellationToken ct = default)
    {
        _records.Add(record);
        return Task.FromResult(record);
    }

    public Task<ResourceLogRecord> QueueWorkAsync(QueueAgentWorkRequest request, CancellationToken ct = default)
    {
        var existing = _records.FirstOrDefault(record =>
            record.AgentId == request.AgentId &&
            record.CorrelationId == request.CorrelationId &&
            record.Type == ResourceLogType.MessageIn);
        if (existing is not null)
            return Task.FromResult(existing);

        var record = new ResourceLogRecord
        {
            AgentId = request.AgentId,
            SessionId = request.SessionId,
            WorkspaceId = request.WorkspaceId,
            Type = ResourceLogType.MessageIn,
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

    public Task<ResourceLogRecord?> GetAsync(Guid logId, CancellationToken ct = default) =>
        Task.FromResult(_records.FirstOrDefault(record => record.Id == logId));

    public Task<ResourceLogRecord?> StartWorkAsync(Guid workLogId, CancellationToken ct = default)
    {
        MarkWork(workLogId, AgentWorkStatusKinds.Running, null);
        return GetAsync(workLogId, ct);
    }

    public Task<ResourceLogRecord?> ClaimNextQueuedWorkAsync(CancellationToken ct = default)
    {
        var runningSessionIds = _records
            .Where(record => record.WorkStatus == AgentWorkStatusKinds.Running && record.SessionId.HasValue)
            .Select(record => record.SessionId!.Value)
            .ToHashSet();
        var queued = _records
            .Where(record => record.Type == ResourceLogType.MessageIn
                && record.WorkStatus == AgentWorkStatusKinds.Queued
                && record.SessionId.HasValue
                && !runningSessionIds.Contains(record.SessionId.Value))
            .OrderBy(record => record.Time)
            .FirstOrDefault();
        if (queued is null)
            return Task.FromResult<ResourceLogRecord?>(null);

        var claimed = CopyWork(queued, AgentWorkStatusKinds.Running, null);
        _records.Remove(queued);
        _records.Add(claimed);
        return Task.FromResult<ResourceLogRecord?>(claimed);
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

    private static ResourceLogRecord CopyWork(ResourceLogRecord record, string status, string? error) => new()
    {
        Id = record.Id,
        ResourceKind = record.ResourceKind,
        ResourceId = record.ResourceId,
        ResourceName = record.ResourceName,
        ParentResourceKind = record.ParentResourceKind,
        ParentResourceId = record.ParentResourceId,
        AgentId = record.AgentId,
        SessionId = record.SessionId,
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

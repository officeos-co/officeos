using OffceOs.Features.ResourceLogs.Domain;
using OffceOs.Features.Agents.Domain;
namespace OffceOs.Features.AgentHarness.Application;

internal sealed class AgentWorkQueueService : IAgentWorkQueueService
{
    private readonly IResourceLogRepository _resourceLogRepository;

    public AgentWorkQueueService(IResourceLogRepository resourceLogRepository)
        => _resourceLogRepository = resourceLogRepository;

    public Task<ResourceLogRecord> QueueWorkAsync(QueueAgentWorkRequest request, CancellationToken ct = default)
        => _resourceLogRepository.UpsertQueuedWorkAsync(new ResourceLogRecord
        {
            AgentId = request.AgentId,
            WorkspaceId = request.WorkspaceId,
            ResourceKind = ResourceLogKinds.Agent,
            ResourceId = request.AgentId,
            Type = ResourceLogType.MessageIn,
            Content = request.Content,
            CorrelationId = request.CorrelationId,
            Time = request.Time ?? DateTime.UtcNow,
            WorkStatus = AgentWorkStatusKinds.Queued,
            WorkPurpose = AgentWorkPurposeKinds.Normalize(request.Purpose),
            DefinitionId = request.DefinitionId,
        }, ct);

    public async Task<ResourceLogRecord?> StartWorkAsync(Guid workLogId, CancellationToken ct = default)
    {
        await _resourceLogRepository.MarkWorkAsync(workLogId, AgentWorkStatusKinds.Running, null, ct);
        return await _resourceLogRepository.GetByAsync(new ResourceLogFilter { Id = workLogId }, ct);
    }

    public Task<ResourceLogRecord?> ClaimNextQueuedWorkAsync(CancellationToken ct = default)
        => _resourceLogRepository.ClaimNextQueuedWorkAsync(ct);

    public Task CompleteWorkAsync(Guid workLogId, CancellationToken ct = default)
        => _resourceLogRepository.MarkWorkAsync(workLogId, AgentWorkStatusKinds.Completed, null, ct);

    public Task FailWorkAsync(Guid workLogId, string error, CancellationToken ct = default)
        => _resourceLogRepository.MarkWorkAsync(workLogId, AgentWorkStatusKinds.Failed, error, ct);
}

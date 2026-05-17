using OffceOs.Features.ResourceLogs.Domain;

namespace OffceOs.Features.AgentHarness.Application;

public interface IAgentWorkQueueService
{
    Task<ResourceLogRecord> QueueWorkAsync(QueueAgentWorkRequest request, CancellationToken ct = default);
    Task<ResourceLogRecord?> StartWorkAsync(Guid workLogId, CancellationToken ct = default);
    Task<ResourceLogRecord?> ClaimNextQueuedWorkAsync(CancellationToken ct = default);
    Task CompleteWorkAsync(Guid workLogId, CancellationToken ct = default);
    Task FailWorkAsync(Guid workLogId, string error, CancellationToken ct = default);
}

public sealed record QueueAgentWorkRequest(
    Guid AgentId,
    Guid SessionId,
    Guid? WorkspaceId,
    string Content,
    string CorrelationId,
    string Purpose,
    Guid? DefinitionId = null,
    DateTime? Time = null);

using OffceOs.Application.Features.AgentHarness;
using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Domain.Features.AgentRoutines;
using OffceOs.Domain.Features.ResourceLogs;
using OffceOs.EventHandlers.Features.ResourceLogs;
using Xunit;

namespace OffceOs.Tests.ResourceLogs;

public sealed class RoutineLoggingHandlerTests
{
    [Fact]
    public async Task Fired_routine_trigger_writes_routine_scoped_resource_log()
    {
        var logs = new RecordingResourceLogService();
        var handler = new RoutineLoggingHandler(logs);
        var routineId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var triggerId = Guid.NewGuid();

        await handler.Handle(new RoutineTriggerFiredEvent(
            routineId,
            "Daily triage",
            agentId,
            workspaceId,
            triggerId,
            "Every minute",
            AgentRoutineTriggerKinds.Schedule,
            "correlation",
            null), CancellationToken.None);

        var log = Assert.Single(logs.Records);
        Assert.Equal(ResourceLogKinds.Routine, log.ResourceKind);
        Assert.Equal(routineId, log.ResourceId);
        Assert.Equal(routineId.ToString(), log.ResourceName);
        Assert.Equal(ResourceLogKinds.Agent, log.ParentResourceKind);
        Assert.Equal(agentId, log.ParentResourceId);
        Assert.Equal(agentId, log.AgentId);
        Assert.Equal(workspaceId, log.WorkspaceId);
        Assert.Equal("correlation", log.CorrelationId);
        Assert.Contains("Every minute", log.Content);
        Assert.Contains(triggerId.ToString(), log.MetadataJson ?? string.Empty);
    }

    private sealed class RecordingResourceLogService : IResourceLogService
    {
        public List<ResourceLogRecord> Records { get; } = [];

        public Task<ResourceLogPage> ListAsync(ResourceLogQueryRequest request, CancellationToken ct = default) =>
            Task.FromResult(new ResourceLogPage([], 0));

        public Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(LastRelevantLogQueryRequest request, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, string?>>(new Dictionary<Guid, string?>());

        public Task<ResourceLogRecord> AppendAsync(ResourceLogRecord record, CancellationToken ct = default)
        {
            Records.Add(record);
            return Task.FromResult(record);
        }

        public Task<ResourceLogRecord> QueueWorkAsync(QueueAgentWorkRequest request, CancellationToken ct = default) =>
            Task.FromResult(ResourceLogRecord.MessageIn(request.AgentId, request.Content, request.CorrelationId));

        public Task<ResourceLogRecord?> GetAsync(Guid logId, CancellationToken ct = default) =>
            Task.FromResult<ResourceLogRecord?>(null);

        public Task<ResourceLogRecord?> StartWorkAsync(Guid workLogId, CancellationToken ct = default) =>
            Task.FromResult<ResourceLogRecord?>(null);

        public Task<ResourceLogRecord?> ClaimNextQueuedWorkAsync(CancellationToken ct = default) =>
            Task.FromResult<ResourceLogRecord?>(null);

        public Task CompleteWorkAsync(Guid workLogId, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task FailWorkAsync(Guid workLogId, string error, CancellationToken ct = default) =>
            Task.CompletedTask;
    }
}

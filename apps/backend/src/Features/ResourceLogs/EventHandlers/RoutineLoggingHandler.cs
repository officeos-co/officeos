namespace OffceOs.EventHandlers.Features.ResourceLogs;

internal sealed class RoutineLoggingHandler :
    INotificationHandler<RoutineTriggerFiredEvent>,
    INotificationHandler<RoutineTriggerFailedEvent>
{
    private readonly IResourceLogService _resourceLogService;

    public RoutineLoggingHandler(IResourceLogService resourceLogService)
        => _resourceLogService = resourceLogService;

    public async Task Handle(RoutineTriggerFiredEvent notification, CancellationToken ct)
    {
        await _resourceLogService.AppendAsync(new ResourceLogRecord
        {
            ResourceKind = ResourceLogKinds.Routine,
            ResourceId = notification.RoutineId,
            ResourceName = notification.RoutineId.ToString(),
            ParentResourceKind = ResourceLogKinds.Agent,
            ParentResourceId = notification.AgentId,
            AgentId = notification.AgentId,
            WorkspaceId = notification.WorkspaceId,
            Type = ResourceLogType.System,
            Content = $"Routine trigger fired: {notification.TriggerName}",
            CorrelationId = notification.CorrelationId,
            Time = notification.OccurredAt,
            MetadataJson = JsonSerializer.Serialize(new
            {
                notification.RoutineId,
                notification.RoutineName,
                notification.TriggerId,
                notification.TriggerName,
                notification.TriggerKind,
                notification.PayloadLength,
            }),
        }, ct);
    }

    public async Task Handle(RoutineTriggerFailedEvent notification, CancellationToken ct)
    {
        await _resourceLogService.AppendAsync(new ResourceLogRecord
        {
            ResourceKind = ResourceLogKinds.Routine,
            ResourceId = notification.RoutineId,
            ResourceName = notification.RoutineId.ToString(),
            ParentResourceKind = ResourceLogKinds.Agent,
            ParentResourceId = notification.AgentId,
            AgentId = notification.AgentId,
            WorkspaceId = notification.WorkspaceId,
            Type = ResourceLogType.ErrorTurnOrchestration,
            Severity = ResourceLogSeverityKinds.Error,
            Content = $"Routine trigger failed: {notification.TriggerName}: {notification.Error}",
            Time = notification.OccurredAt,
            MetadataJson = JsonSerializer.Serialize(new
            {
                notification.RoutineId,
                notification.RoutineName,
                notification.TriggerId,
                notification.TriggerName,
                notification.TriggerKind,
            }),
        }, ct);
    }
}

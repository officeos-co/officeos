namespace OffceOs.EventHandlers.Features.Observability;

internal sealed class RoutineLoggingHandler :
    INotificationHandler<RoutineTriggerFiredEvent>,
    INotificationHandler<RoutineTriggerFailedEvent>
{
    private readonly IAgentLogService _agentLogService;

    public RoutineLoggingHandler(IAgentLogService agentLogService)
        => _agentLogService = agentLogService;

    public async Task Handle(RoutineTriggerFiredEvent notification, CancellationToken ct)
    {
        await _agentLogService.AppendAsync(new AgentLogRecord
        {
            ResourceKind = ResourceLogKinds.Routine,
            ResourceId = notification.RoutineId,
            ResourceName = notification.RoutineId.ToString(),
            ParentResourceKind = ResourceLogKinds.Agent,
            ParentResourceId = notification.AgentId,
            AgentId = notification.AgentId,
            WorkspaceId = notification.WorkspaceId,
            Type = AgentLogType.System,
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
        await _agentLogService.AppendAsync(new AgentLogRecord
        {
            ResourceKind = ResourceLogKinds.Routine,
            ResourceId = notification.RoutineId,
            ResourceName = notification.RoutineId.ToString(),
            ParentResourceKind = ResourceLogKinds.Agent,
            ParentResourceId = notification.AgentId,
            AgentId = notification.AgentId,
            WorkspaceId = notification.WorkspaceId,
            Type = AgentLogType.ErrorTurnOrchestration,
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

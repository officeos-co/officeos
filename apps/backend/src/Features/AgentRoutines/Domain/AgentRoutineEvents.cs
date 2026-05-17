using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentRoutines.Domain;

public sealed record RoutineTriggerFiredEvent(
    Guid RoutineId,
    string RoutineName,
    Guid AgentId,
    Guid SessionId,
    Guid? WorkspaceId,
    Guid TriggerId,
    string TriggerName,
    string TriggerKind,
    string? CorrelationId,
    int? PayloadLength) : DomainEvent;

public sealed record RoutineTriggerFailedEvent(
    Guid RoutineId,
    string RoutineName,
    Guid AgentId,
    Guid? WorkspaceId,
    Guid TriggerId,
    string TriggerName,
    string TriggerKind,
    string Error) : DomainEvent;

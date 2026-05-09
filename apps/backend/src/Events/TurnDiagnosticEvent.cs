namespace OffceOs.Domain.Events;

public sealed record TurnDiagnosticEvent(Guid AgentId, string CorrelationId, string Message, int DurationMs) : DomainEvent;

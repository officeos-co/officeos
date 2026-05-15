namespace OffceOs.Domain.Features.AgentHarness;

public sealed record TurnDiagnosticEvent(Guid AgentId, string CorrelationId, string Message, int DurationMs) : DomainEvent;

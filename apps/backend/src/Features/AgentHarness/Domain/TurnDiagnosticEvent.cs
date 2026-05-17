using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record TurnDiagnosticEvent(Guid AgentId, Guid SessionId, string CorrelationId, string Message, int DurationMs) : DomainEvent;

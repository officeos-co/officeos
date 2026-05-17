using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record ToolCallCompletedEvent(Guid AgentId, Guid SessionId, string CorrelationId, string ToolName, bool Success, string Output, int DurationMs) : DomainEvent;

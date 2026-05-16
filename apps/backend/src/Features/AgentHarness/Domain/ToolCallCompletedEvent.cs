using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record ToolCallCompletedEvent(Guid AgentId, string CorrelationId, string ToolName, bool Success, string Output, int DurationMs) : DomainEvent;

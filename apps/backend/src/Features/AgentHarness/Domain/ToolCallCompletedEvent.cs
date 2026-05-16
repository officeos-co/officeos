using OffceOs.Domain.Common;

namespace OffceOs.Domain.Features.AgentHarness;

public sealed record ToolCallCompletedEvent(Guid AgentId, string CorrelationId, string ToolName, bool Success, string Output, int DurationMs) : DomainEvent;

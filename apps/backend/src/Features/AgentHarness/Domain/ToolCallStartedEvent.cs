using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record ToolCallStartedEvent(Guid AgentId, string CorrelationId, string ToolName, string ArgsJson) : DomainEvent;

namespace OffceOs.Domain.Features.AgentHarness;

public sealed record ToolCallStartedEvent(Guid AgentId, string CorrelationId, string ToolName, string ArgsJson) : DomainEvent;

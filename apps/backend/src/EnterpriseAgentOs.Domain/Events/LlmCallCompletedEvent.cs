namespace EnterpriseAgentOs.Domain.Events;

public sealed record LlmCallCompletedEvent(Guid AgentId, string CorrelationId, int DurationMs, int? InputTokens = null, int? OutputTokens = null) : DomainEvent;

namespace OffceOs.Domain.Events;

public sealed record LlmCallCompletedEvent(Guid AgentId, string CorrelationId, string Model, int DurationMs, int? InputTokens = null, int? OutputTokens = null) : DomainEvent;

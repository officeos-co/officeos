namespace EnterpriseAgentOs.Domain.Events;

public sealed record ConversationCompactedEvent(
    Guid AgentId,
    string CorrelationId,
    Guid LastCompactedLogId,
    int PreCompactTokens,
    int PostCompactTokens) : DomainEvent;

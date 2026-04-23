namespace EnterpriseAgentOs.Domain.Events;

public sealed record MessageReceivedEvent(Guid AgentId, string Content, string CorrelationId, string? PodName) : DomainEvent;

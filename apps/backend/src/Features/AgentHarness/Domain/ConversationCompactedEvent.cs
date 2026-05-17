using OffceOs.Common.Domain;

namespace OffceOs.Features.AgentHarness.Domain;

public sealed record ConversationCompactedEvent(
    Guid AgentId,
    Guid SessionId,
    string CorrelationId,
    Guid LastCompactedLogId,
    int PreCompactTokens,
    int PostCompactTokens) : DomainEvent;

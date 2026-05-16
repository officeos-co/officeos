using OffceOs.Domain.Common;

namespace OffceOs.Domain.Features.AgentHarness;

public sealed record ConversationCompactedEvent(
    Guid AgentId,
    string CorrelationId,
    Guid LastCompactedLogId,
    int PreCompactTokens,
    int PostCompactTokens) : DomainEvent;

namespace OffceOs.Domain.Events;

public sealed record LlmCallCompletedEvent(
    Guid AgentId,
    string CorrelationId,
    string Provider,
    string Model,
    int DurationMs,
    int? InputTokens = null,
    int? OutputTokens = null,
    int? CacheReadTokens = null,
    int? CacheWriteTokens = null,
    int? ReasoningTokens = null,
    bool EstimatedTokens = false,
    IReadOnlyList<LlmUsageContextPartMessage>? ContextParts = null,
    Guid? RunId = null,
    Guid? ParentRunId = null) : DomainEvent;

public sealed record LlmUsageContextPartMessage(
    string Kind,
    string Label,
    string? Role,
    string? Tool,
    string? Integration,
    long Tokens,
    bool EstimatedTokens,
    int CharacterCount);

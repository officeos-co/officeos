using OffceOs.Domain.Common;

namespace OffceOs.Domain.Features.AgentHarness;

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
    IReadOnlyList<LlmUsageContextPartMessage>? ContextParts = null) : DomainEvent;

public sealed record LlmUsageContextPartMessage(
    string Kind,
    string Label,
    string? Role,
    string? Tool,
    string? Integration,
    long Tokens,
    bool EstimatedTokens,
    int CharacterCount);

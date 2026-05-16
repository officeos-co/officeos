namespace OffceOs.Domain.Features.ResourceLogs;

/// <summary>
/// Captures token consumption and latency for a single LLM interaction.
/// </summary>
public readonly record struct TokenUsage(int? InputTokens, int? OutputTokens, int? DurationMs)
{
    public int TotalTokens => (InputTokens ?? 0) + (OutputTokens ?? 0);
    public bool HasData => InputTokens.HasValue || OutputTokens.HasValue;

    public static TokenUsage Empty => new(null, null, null);
}

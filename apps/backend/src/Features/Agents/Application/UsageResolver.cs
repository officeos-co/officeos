namespace OffceOs.Application.Features.Agents;

/// <summary>
/// Resolves billable LLM usage from provider-reported tokens or deterministic estimates.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> choosing reported token counts when present and falling back
/// to local estimates when providers omit usage.</para>
/// <para><strong>Responsible only for:</strong> token usage normalization. It does not price usage,
/// persist credits, enforce quota, parse SSE, or dispatch providers.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when token estimation,
/// reported-vs-estimated precedence, or usage normalization rules change.</para>
/// </remarks>
internal sealed class UsageResolver
{
    public ResolvedLlmUsage Resolve(
        JsonElement requestBody,
        string? assistantContent,
        IReadOnlyList<ParsedToolCall> toolCalls,
        int? reportedInputTokens,
        int? reportedOutputTokens)
    {
        var estimatedInputTokens = EstimateTokens(requestBody.GetRawText());
        var estimatedOutputTokens = EstimateTokens(
            $"{assistantContent ?? string.Empty}\n{string.Join('\n', toolCalls.Select(tc => $"{tc.Name} {tc.Arguments}"))}");

        var inputTokens = reportedInputTokens is > 0 ? reportedInputTokens.Value : estimatedInputTokens;
        var outputTokens = reportedOutputTokens is > 0 ? reportedOutputTokens.Value : estimatedOutputTokens;
        var isEstimated = reportedInputTokens is not > 0 || reportedOutputTokens is not > 0;

        return new ResolvedLlmUsage(inputTokens, outputTokens, isEstimated);
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        return Math.Max(1, (text.Length + 3) / 4);
    }
}

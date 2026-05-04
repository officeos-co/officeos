namespace EnterpriseAgentOs.Application.Features.Agents;

/// <summary>
/// Parsed assistant tool-call data returned by the streamed LLM response.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> carrying the stable tool-call id, tool name, and JSON arguments.</para>
/// <para><strong>Responsible only for:</strong> data transfer between LLM parsing, orchestration, and tool execution.</para>
/// <para><strong>Acceptance criteria:</strong> this type should change only when the internal parsed tool-call shape changes.</para>
/// </remarks>
internal sealed record ParsedToolCall(string Id, string Name, string Arguments);

/// <summary>
/// Parsed result of an OpenAI-compatible SSE response.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> carrying assistant text, parsed tool calls, and provider-reported usage.</para>
/// <para><strong>Responsible only for:</strong> data transfer from stream parsing to LLM turn execution.</para>
/// <para><strong>Acceptance criteria:</strong> this type should change only when the parser output shape changes.</para>
/// </remarks>
internal sealed record SseResult(string? Content, IReadOnlyList<ParsedToolCall> ToolCalls, int? InputTokens, int? OutputTokens);

/// <summary>
/// Normalized LLM usage for billing and logging.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> carrying input/output token counts and whether any value was estimated.</para>
/// <para><strong>Responsible only for:</strong> usage data transfer. It does not estimate, price, or persist usage.</para>
/// <para><strong>Acceptance criteria:</strong> this type should change only when normalized usage shape changes.</para>
/// </remarks>
internal sealed record ResolvedLlmUsage(int InputTokens, int OutputTokens, bool IsEstimated)
{
    public long TotalTokens => (long)InputTokens + OutputTokens;
}

/// <summary>
/// Result of one LLM call inside a turn.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> carrying assistant output, requested tool calls, resolved model,
/// duration, and normalized usage.</para>
/// <para><strong>Responsible only for:</strong> LLM turn data transfer from `LlmTurnExecutor` to orchestration.</para>
/// <para><strong>Acceptance criteria:</strong> this type should change only when the orchestrator needs a different LLM-call result shape.</para>
/// </remarks>
internal sealed record LlmTurnResult(
    string? AssistantContent,
    IReadOnlyList<ParsedToolCall> ToolCalls,
    string Model,
    int DurationMs,
    ResolvedLlmUsage Usage);

/// <summary>
/// Result of executing assistant-requested tools for one LLM iteration.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> carrying whether the turn should stop, the stop reason, and updated tool-call count.</para>
/// <para><strong>Responsible only for:</strong> tool loop outcome data transfer. It does not dispatch tools or publish events.</para>
/// <para><strong>Acceptance criteria:</strong> this type should change only when orchestration needs different tool-loop outcome data.</para>
/// </remarks>
internal sealed record ToolLoopResult(bool ShouldStop, string? ErrorMessage, int TotalToolCalls)
{
    public static ToolLoopResult Continue(int totalToolCalls) => new(false, null, totalToolCalls);
    public static ToolLoopResult Stop(string errorMessage, int totalToolCalls) => new(true, errorMessage, totalToolCalls);
}

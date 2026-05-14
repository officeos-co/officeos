namespace OffceOs.Application.Features.Agents;

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
internal sealed record SseResult(
    string? Content,
    IReadOnlyList<ParsedToolCall> ToolCalls,
    int? InputTokens,
    int? OutputTokens,
    int? CacheReadTokens,
    int? CacheWriteTokens,
    int? ReasoningTokens);

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
    AgentUsageResolutionResult Usage);

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

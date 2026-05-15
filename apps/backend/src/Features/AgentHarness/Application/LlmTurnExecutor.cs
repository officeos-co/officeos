namespace OffceOs.Application.Features.AgentHarness;

/// <summary>
/// Executes one streamed LLM call inside an agent turn.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> building the request, dispatching the provider call through
/// the Providers feature, parsing the stream, and returning normalized usage with assistant content
/// and tool calls.</para>
/// <para><strong>Responsible only for:</strong> LLM call execution. It does not record billing,
/// publish turn events, execute tools, persist runs, or decide turn completion.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when LLM request building,
/// stream parsing, usage resolution, or LLM-call result shape changes.</para>
/// </remarks>
internal sealed class LlmTurnExecutor
{
    private readonly IProviderDispatchService _providerDispatchService;
    private readonly LlmRequestBuilder _llmRequestBuilder;
    private readonly SseResponseParser _sseResponseParser;
    private readonly TurnEventPublisher _turnEventPublisher;
    private readonly ILogger<LlmTurnExecutor> _logger;

    public LlmTurnExecutor(
        IProviderDispatchService providerDispatchService,
        LlmRequestBuilder requestBuilder,
        SseResponseParser sseResponseParser,
        TurnEventPublisher events,
        ILogger<LlmTurnExecutor> logger)
    {
        _providerDispatchService = providerDispatchService;
        _llmRequestBuilder = requestBuilder;
        _sseResponseParser = sseResponseParser;
        _turnEventPublisher = events;
        _logger = logger;
    }

    public async Task<AgentResult<LlmTurnResult>> ExecuteAsync(
        AgentRecord agent,
        ConversationHistory history,
        ToolRegistry registry,
        int iteration,
        string correlationId,
        CancellationToken ct)
    {
        var requestStart = Stopwatch.GetTimestamp();
        var requestBody = _llmRequestBuilder.Build(agent, history, registry);
        await _turnEventPublisher.PublishDiagnosticAsync(
            agent.Id,
            correlationId,
            $"LLM iteration {iteration}: request built",
            ElapsedMs(requestStart),
            ct);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "LLM request payload for agent {AgentId} correlation {CorrelationId} iteration {Iteration}: {Payload}",
                agent.Id,
                correlationId,
                iteration,
                requestBody.GetRawText());
        }

        var llmStart = Stopwatch.GetTimestamp();
        var llmResult = await _providerDispatchService.DispatchAsync(agent.Provider, agent.WorkspaceId, agent.Model ?? "auto", requestBody, ct);
        if (llmResult.IsFailure)
        {
            await _turnEventPublisher.PublishDiagnosticAsync(
                agent.Id,
                correlationId,
                $"LLM iteration {iteration}: provider dispatch failed",
                ElapsedMs(llmStart),
                ct);
            return llmResult.Error;
        }

        var sseResult = await _sseResponseParser.ParseAsync(llmResult.Value.Response, ct);
        var durationMs = (int)Stopwatch.GetElapsedTime(llmStart).TotalMilliseconds;
        await _turnEventPublisher.PublishDiagnosticAsync(
            agent.Id,
            correlationId,
            $"LLM iteration {iteration}: provider stream parsed",
            durationMs,
            ct);

        var usage = ResolveUsage(requestBody, sseResult);
        if (usage.EstimatedTokens)
        {
            _logger.LogWarning(
                "LLM provider did not return complete token usage for agent {AgentId} correlation {CorrelationId}; using estimated usage {InputTokens}/{OutputTokens}",
                agent.Id,
                correlationId,
                usage.InputTokens,
                usage.OutputTokens);
        }

        return new LlmTurnResult(
            sseResult.Content,
            sseResult.ToolCalls,
            llmResult.Value.Model,
            durationMs,
            usage);
    }

    private static int ElapsedMs(long startTimestamp)
        => (int)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

    private static LlmTurnUsage ResolveUsage(JsonElement requestBody, SseResult sseResult)
    {
        var estimatedInput = EstimateTokens(requestBody.GetRawText());
        var estimatedOutput = EstimateTokens((sseResult.Content ?? string.Empty)
            + string.Concat(sseResult.ToolCalls.Select(call => call.Name + call.Arguments)));

        var inputTokens = sseResult.InputTokens ?? estimatedInput;
        var outputTokens = sseResult.OutputTokens ?? estimatedOutput;
        var estimated = !sseResult.InputTokens.HasValue || !sseResult.OutputTokens.HasValue;

        return new LlmTurnUsage(
            inputTokens,
            outputTokens,
            sseResult.CacheReadTokens,
            sseResult.CacheWriteTokens,
            sseResult.ReasoningTokens,
            estimated,
            [
                new LlmUsageContextPartMessage(
                    "request",
                    "LLM request",
                    null,
                    null,
                    null,
                    inputTokens,
                    estimated,
                    requestBody.GetRawText().Length),
                new LlmUsageContextPartMessage(
                    "response",
                    "LLM response",
                    "assistant",
                    null,
                    null,
                    outputTokens,
                    estimated,
                    (sseResult.Content ?? string.Empty).Length),
            ]);
    }

    private static int EstimateTokens(string text)
        => Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
}

namespace OffceOs.Application.Features.Agents;

/// <summary>
/// Executes one streamed LLM call inside an agent turn.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> building the request, resolving the provider API key,
/// dispatching the model call, parsing the stream, and returning normalized usage with assistant
/// content and tool calls.</para>
/// <para><strong>Responsible only for:</strong> LLM call execution. It does not record billing,
/// publish turn events, execute tools, persist runs, or decide turn completion.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when LLM dispatch flow,
/// provider key lookup, provider error handling, or LLM-call result shape changes.</para>
/// </remarks>
internal sealed class LlmTurnExecutor
{
    private readonly LlmProviderDispatcher _llmProviderDispatcher;
    private readonly IProviderService _providerService;
    private readonly LlmRequestBuilder _llmRequestBuilder;
    private readonly SseResponseParser _sseResponseParser;
    private readonly UsageResolver _usageResolver;
    private readonly TurnEventPublisher _turnEventPublisher;
    private readonly ILogger<LlmTurnExecutor> _logger;

    public LlmTurnExecutor(
        LlmProviderDispatcher llmProviderDispatcher,
        IProviderService providerService,
        LlmRequestBuilder requestBuilder,
        SseResponseParser sseResponseParser,
        UsageResolver usageResolver,
        TurnEventPublisher events,
        ILogger<LlmTurnExecutor> logger)
    {
        _llmProviderDispatcher = llmProviderDispatcher;
        _providerService = providerService;
        _llmRequestBuilder = requestBuilder;
        _sseResponseParser = sseResponseParser;
        _usageResolver = usageResolver;
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

        var provider = agent.Provider;
        var providerKeyStart = Stopwatch.GetTimestamp();
        var apiKey = await _providerService.GetApiKeyForDispatchAsync(provider, ct);
        await _turnEventPublisher.PublishDiagnosticAsync(
            agent.Id,
            correlationId,
            $"LLM iteration {iteration}: provider key loaded",
            ElapsedMs(providerKeyStart),
            ct);
        if (apiKey is null)
        {
            return new AgentError(AgentErrorCategory.Configuration, $"Provider '{provider}' has no API key configured.");
        }

        var llmStart = Stopwatch.GetTimestamp();
        var llmResult = await _llmProviderDispatcher.DispatchAsync(provider, apiKey, agent.Model ?? "auto", requestBody, ct);
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

        var usageStart = Stopwatch.GetTimestamp();
        var usage = _usageResolver.Resolve(
            requestBody,
            sseResult.Content,
            sseResult.ToolCalls,
            sseResult.InputTokens,
            sseResult.OutputTokens);
        await _turnEventPublisher.PublishDiagnosticAsync(
            agent.Id,
            correlationId,
            $"LLM iteration {iteration}: usage resolved",
            ElapsedMs(usageStart),
            ct);

        if (usage.IsEstimated)
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
}

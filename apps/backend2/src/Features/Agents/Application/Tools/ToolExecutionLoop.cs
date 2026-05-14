namespace OffceOs.Application.Features.Agents;

/// <summary>
/// Owns per-turn tool registry creation and assistant-requested tool execution.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> creating the turn's `ToolRegistry`, dispatching parsed tool
/// calls, revealing deferred tools after `tool_search`, applying loop detection, publishing tool-call
/// events, and appending tool results to conversation history.</para>
/// <para><strong>Responsible only for:</strong> tool execution inside the agent loop. It does not call
/// LLM providers, calculate usage, record billing, persist runs, or decide non-tool turn completion.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when tool registry creation,
/// tool dispatch behavior, deferred tool reveal behavior, loop detection handling, or tool-result history
/// shaping changes.</para>
/// </remarks>
internal sealed class ToolExecutionLoop
{
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly ToolRegistryFactory _toolRegistryFactory;
    private readonly IAgentSandbox _agentSandbox;
    private readonly TurnEventPublisher _turnEventPublisher;

    public ToolExecutionLoop(
        IIntegrationDefinitionService integrationDefinitionService,
        ToolRegistryFactory toolRegistryFactory,
        IAgentSandbox sandbox,
        TurnEventPublisher events)
    {
        _integrationDefinitionService = integrationDefinitionService;
        _toolRegistryFactory = toolRegistryFactory;
        _agentSandbox = sandbox;
        _turnEventPublisher = events;
    }

    public async Task<ToolExecutionSession> CreateSessionAsync(AgentRecord agent, string correlationId, CancellationToken ct)
    {
        var integrationListStart = Stopwatch.GetTimestamp();
        var integrations = await _integrationDefinitionService.ListForAgentAsync(agent.Id, agent.OwnerId, ct);
        await _turnEventPublisher.PublishDiagnosticAsync(
            agent.Id,
            correlationId,
            $"Tool setup: listed integrations ({integrations.Count})",
            ElapsedMs(integrationListStart),
            ct);

        var registryStart = Stopwatch.GetTimestamp();
        var registry = await _toolRegistryFactory.CreateAsync(new ToolRegistryRequest
        {
            Sandbox = _agentSandbox,
            SandboxId = agent.PodName ?? string.Empty,
            ServiceUrl = agent.ServiceUrl ?? string.Empty,
            AgentId = agent.Id,
            WorkspaceId = agent.WorkspaceId,
            CorrelationId = correlationId,
            Integrations = integrations,
            OwnerId = agent.OwnerId,
        }, ct);
        await _turnEventPublisher.PublishDiagnosticAsync(
            agent.Id,
            correlationId,
            $"Tool setup: registry created ({registry.Tools.Count} tools)",
            ElapsedMs(registryStart),
            ct);
        return new ToolExecutionSession(registry, new LoopDetector());
    }

    public async Task<ToolLoopResult> ExecuteAsync(
        Guid agentId,
        string correlationId,
        IReadOnlyList<ParsedToolCall> toolCalls,
        ToolExecutionSession session,
        ConversationHistory history,
        int startingToolCallCount,
        CancellationToken ct)
    {
        var totalToolCalls = startingToolCallCount;

        foreach (var toolCall in toolCalls)
        {
            JsonElement args;
            try { args = JsonSerializer.Deserialize<JsonElement>(toolCall.Arguments); }
            catch { args = JsonSerializer.SerializeToElement(new { }); }

            await _turnEventPublisher.PublishToolCallStartedAsync(agentId, correlationId, toolCall.Name, toolCall.Arguments, ct);
            totalToolCalls++;

            var toolStart = Stopwatch.GetTimestamp();
            var toolDispatchResult = await session.Registry.DispatchAsync(toolCall.Name, args, ct);
            var toolDurationMs = (int)Stopwatch.GetElapsedTime(toolStart).TotalMilliseconds;
            if (toolDispatchResult.IsSuccess
                && session.Registry.Tools.FirstOrDefault(t => t.Name == toolCall.Name) is ToolSearchTool searchTool)
            {
                session.Registry.RevealTools(searchTool.LastMatchedToolNames);
            }

            if (toolDispatchResult.IsFailure)
            {
                await _turnEventPublisher.PublishToolCallCompletedAsync(
                    agentId,
                    correlationId,
                    toolCall.Name,
                    false,
                    toolDispatchResult.Error.Message,
                    toolDurationMs,
                    ct);
                history.Push(new ChatMessage { Role = "tool", Content = $"[error] {toolDispatchResult.Error.Message}", ToolCallId = toolCall.Id });
                continue;
            }

            var result = toolDispatchResult.Value;
            var output = result.Success ? result.Output : $"[error] {result.Error}\n{result.Output}";

            var loopResult = session.LoopDetector.Record(toolCall.Name, toolCall.Arguments, output);
            switch (loopResult)
            {
                case LoopDetectionResult.BreakResult breakResult:
                    return ToolLoopResult.Stop(breakResult.Message, totalToolCalls);
                case LoopDetectionResult.BlockResult blockResult:
                    output = $"BLOCKED: {blockResult.Message}";
                    break;
            }

            await _turnEventPublisher.PublishToolCallCompletedAsync(agentId, correlationId, toolCall.Name, result.Success, output, toolDurationMs, ct);

            var historyOutput = output.Length > 10000 ? output[..10000] + "\n[truncated]" : output;
            history.Push(new ChatMessage { Role = "tool", Content = historyOutput, ToolCallId = toolCall.Id });
        }

        return ToolLoopResult.Continue(totalToolCalls);
    }

    private static int ElapsedMs(long startTimestamp)
        => (int)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}

/// <summary>
/// Holds per-turn tool execution state.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> keeping the turn's `ToolRegistry` and `LoopDetector`
/// together for the full lifetime of a turn.</para>
/// <para><strong>Responsible only for:</strong> per-turn tool state lifetime. It does not dispatch
/// tools, publish events, call the LLM, or mutate conversation history.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when per-turn tool state
/// lifetime or ownership changes.</para>
/// </remarks>
internal sealed class ToolExecutionSession : IAsyncDisposable
{
    public ToolExecutionSession(ToolRegistry registry, LoopDetector loopDetector)
    {
        Registry = registry;
        LoopDetector = loopDetector;
    }

    public ToolRegistry Registry { get; }
    public LoopDetector LoopDetector { get; }

    public ValueTask DisposeAsync() => Registry.DisposeAsync();
}

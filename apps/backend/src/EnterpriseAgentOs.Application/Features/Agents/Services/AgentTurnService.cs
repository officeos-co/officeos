using System.Diagnostics;
using MediatR;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class AgentTurnService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IPublisher _publisher;
    private readonly LlmProviderDispatcher _llmProviderDispatcher;
    private readonly IProviderService _providerService;
    private readonly IMcpServerService _mcpServerService;
    private readonly ToolRegistryFactory _toolRegistryFactory;
    private readonly IAgentSandbox _sandbox;
    private readonly ConversationCompactionService _compactionService;
    private readonly IAgentRunRepository _agentRunRepository;
    private readonly IBillingGuard _billingGuard;
    private readonly ILogger<AgentTurnService> _logger;

    private const int MaxIterations = 25;
    private const int MaxTokens = 8192;
    private const int KeepRecent = 4;

    public AgentTurnService(
        IAgentRepository agents,
        IPublisher publisher,
        LlmProviderDispatcher llm,
        IProviderService providers,
        IMcpServerService mcpServerService,
        ToolRegistryFactory toolRegistryFactory,
        IAgentSandbox sandbox,
        ConversationCompactionService compactionService,
        IAgentRunRepository agentRunRepository,
        IBillingGuard billingGuard,
        ILogger<AgentTurnService> logger)
    {
        _agentRepository = agents;
        _publisher = publisher;
        _llmProviderDispatcher = llm;
        _providerService = providers;
        _mcpServerService = mcpServerService;
        _toolRegistryFactory = toolRegistryFactory;
        _sandbox = sandbox;
        _compactionService = compactionService;
        _agentRunRepository = agentRunRepository;
        _billingGuard = billingGuard;
        _logger = logger;
    }

    public async Task RunTurnAsync(Guid agentId, string userMessage, string correlationId, CancellationToken ct)
    {
        try
        {
            await RunTurnCoreAsync(agentId, userMessage, correlationId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in turn for agent {AgentId} correlation {CorrelationId}", agentId, correlationId);
            try
            {
                await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, $"Internal error: {ex.Message}"), ct);
            }
            catch (Exception pubEx)
            {
                _logger.LogError(pubEx, "Failed to publish error event for agent {AgentId}", agentId);
            }
        }
    }

    private async Task RunTurnCoreAsync(Guid agentId, string userMessage, string correlationId, CancellationToken ct)
    {
        var turnStart = Stopwatch.GetTimestamp();

        var agent = await _agentRepository.GetAsync(agentId, ct);
        if (agent is null)
        {
            await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, $"Agent {agentId} not found"), ct);
            return;
        }

        if (string.IsNullOrEmpty(agent.PodName))
        {
            await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, $"Agent {agentId} has no pod"), ct);
            return;
        }

        var run = await _agentRunRepository.CreateAsync(new AgentRunRecord
        {
            AgentId = agentId,
            ParentCorrelationId = correlationId,
            Kind = "turn",
            Status = "running",
            Name = "Agent turn",
            Prompt = userMessage,
        }, ct);
        using var runScope = AgentRunContext.Begin(run.Id, run.ParentRunId);

        async Task FinishRunAsync(string status, string? result = null, string? error = null)
        {
            run.Status = status;
            run.Result = result;
            run.Error = error;
            run.CompletedAt = DateTime.UtcNow;
            run.UpdatedAt = DateTime.UtcNow;
            await _agentRunRepository.UpdateAsync(run, ct);
        }

        await _publisher.Publish(new TurnStartedEvent(agentId, correlationId, userMessage), ct);

        var sandboxId = agent.PodName;
        var sandboxStart = Stopwatch.GetTimestamp();
        await _publisher.Publish(new PodConnectedEvent(agentId, correlationId, (int)Stopwatch.GetElapsedTime(sandboxStart).TotalMilliseconds), ct);

        var mcpServers = await _mcpServerService.ListForAgentAsync(agentId, ct);
        await using var registry = await _toolRegistryFactory.CreateAsync(
            _sandbox, sandboxId, agent.ServiceUrl ?? string.Empty, agentId, mcpServers,
            serverName => _mcpServerService.GetDecryptedCredentialAsync(serverName, ct),
            ct);

        var history = new ConversationHistory();
        var loopDetector = new LoopDetector();
        var totalToolCalls = 0;

        // Seed history from recent logs — includes messages, tool calls, and tool results.
        // Deduplication: ChannelOut is skipped when MessageOut exists for the same correlationId.
        // ChannelIn raw JSON → plain text extraction.
        var contextWindow = await _compactionService.LoadAsync(agentId, correlationId, ct);
        var ordered = contextWindow.Logs.OrderBy(l => l.Time).ToList();

        if (!string.IsNullOrWhiteSpace(contextWindow.Summary))
        {
            history.Push(new ChatMessage
            {
                Role = "user",
                Content = $"<persisted-conversation-summary>\n{contextWindow.Summary}\n</persisted-conversation-summary>"
            });
        }

        var outCorrelations = new HashSet<string>(
            ordered.Where(l => l.Type == AgentLogType.MessageOut && l.CorrelationId is not null)
                   .Select(l => l.CorrelationId!));

        // Build a queue of tool call IDs so ToolResult can reference the matching ToolCall
        var pendingToolCallIds = new Queue<string>();

        foreach (var log in ordered)
        {
            switch (log.Type)
            {
                case AgentLogType.MessageIn:
                    history.Push(new ChatMessage { Role = "user", Content = log.Content ?? "" });
                    break;
                case AgentLogType.ChannelIn:
                    history.Push(new ChatMessage { Role = "user", Content = ExtractPlainText(log.Content ?? "") });
                    break;
                case AgentLogType.MessageOut:
                    history.Push(new ChatMessage { Role = "assistant", Content = log.Content ?? "" });
                    break;
                case AgentLogType.ChannelOut when log.CorrelationId is not null && outCorrelations.Contains(log.CorrelationId):
                    break; // Skip — MessageOut already covers this turn
                case AgentLogType.ChannelOut:
                    history.Push(new ChatMessage { Role = "assistant", Content = log.Content ?? "" });
                    break;
                case AgentLogType.ToolCall:
                    var tcId = log.Id.ToString("N");
                    pendingToolCallIds.Enqueue(tcId);
                    history.Push(new ChatMessage
                    {
                        Role = "assistant", Content = null,
                        ToolCalls = [new ChatToolCall { Id = tcId, Name = log.Tool ?? "unknown", Arguments = log.Content ?? "{}" }],
                    });
                    break;
                case AgentLogType.ToolResult:
                    var matchId = pendingToolCallIds.Count > 0 ? pendingToolCallIds.Dequeue() : log.Id.ToString("N");
                    history.Push(new ChatMessage { Role = "tool", Content = log.Content ?? "", ToolCallId = matchId });
                    break;
            }
        }

        history.Push(new ChatMessage { Role = "user", Content = userMessage });

        for (var i = 0; i < MaxIterations; i++)
        {
            // Pre-flight billing check — block if quota exceeded and overage not enabled
            try
            {
                await _billingGuard.ThrowIfQuotaExceededAsync(agentId, ct);
            }
            catch (QuotaExceededException ex)
            {
                await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, ex.Message), ct);
                var quotaMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
                await _publisher.Publish(new TurnCompletedEvent(agentId, correlationId, quotaMs, i, totalToolCalls), ct);
                await FinishRunAsync("failed", error: ex.Message);
                return;
            }

            var systemPrompt = SystemPromptComposer.Compose(agent);

            history.PruneToolResults(maxResultChars: 500, keepRecentTurns: KeepRecent);
            history.Prune(MaxTokens, KeepRecent);

            var deferredTools = registry.GetDeferredToolsMessage();
            var messages = new List<object>
            {
                new
                {
                    role = "system",
                    content = $"{systemPrompt}\n\n{deferredTools}\nUse tool_search to reveal deferred tool schemas before calling deferred tools."
                }
            };

            foreach (var msg in history.Messages)
            {
                if (msg.ToolCalls is { Count: > 0 })
                {
                    messages.Add(new
                    {
                        role = msg.Role, content = msg.Content ?? "",
                        tool_calls = msg.ToolCalls.Select(tc => new
                        {
                            id = tc.Id, type = "function",
                            function = new { name = tc.Name, arguments = tc.Arguments }
                        }).ToList(),
                    });
                }
                else if (msg.ToolCallId is not null)
                {
                    messages.Add(new { role = msg.Role, content = msg.Content ?? "", tool_call_id = msg.ToolCallId });
                }
                else
                {
                    messages.Add(new { role = msg.Role, content = msg.Content ?? "" });
                }
            }

            var requestBody = JsonSerializer.SerializeToElement(new
            {
                model = agent.Model ?? "auto", messages,
                tools = registry.GetSchemas(), stream = true,
            });

            var provider = agent.Provider;
            var apiKey = await _providerService.GetApiKeyForDispatchAsync(provider, ct);
            if (apiKey is null)
            {
                await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, $"Provider '{provider}' has no API key configured."), ct);
                await FinishRunAsync("failed", error: $"Provider '{provider}' has no API key configured.");
                return;
            }

            var llmStart = Stopwatch.GetTimestamp();
            var llmResult = await _llmProviderDispatcher.DispatchAsync(provider, apiKey, agent.Model ?? "auto", requestBody, ct);
            if (llmResult.IsFailure)
            {
                await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, llmResult.Error.Message), ct);
                await FinishRunAsync("failed", error: llmResult.Error.Message);
                return;
            }

            var sseResult = await ParseSseResponseAsync(llmResult.Value, ct);
            var (assistantContent, toolCalls) = (sseResult.Content, sseResult.ToolCalls);
            var llmDuration = (int)Stopwatch.GetElapsedTime(llmStart).TotalMilliseconds;
            var resolvedModel = agent.Model ?? "auto";
            await _publisher.Publish(new LlmCallCompletedEvent(agentId, correlationId, resolvedModel, llmDuration, sseResult.InputTokens, sseResult.OutputTokens), ct);

            if (!string.IsNullOrEmpty(assistantContent))
                await _publisher.Publish(new MessageOutEvent(agentId, correlationId, assistantContent), ct);

            history.Push(new ChatMessage
            {
                Role = "assistant", Content = assistantContent,
                ToolCalls = toolCalls.Count > 0
                    ? toolCalls.Select(tc => new ChatToolCall { Id = tc.Id, Name = tc.Name, Arguments = tc.Arguments }).ToList()
                    : null,
            });

            if (toolCalls.Count == 0)
            {
                var totalMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
                await _publisher.Publish(new TurnCompletedEvent(agentId, correlationId, totalMs, i + 1, totalToolCalls), ct);
                await FinishRunAsync("completed", result: assistantContent);
                return;
            }

            // ── Tool dispatch — tightly coupled, stays in the loop ──
            foreach (var tc in toolCalls)
            {
                JsonElement args;
                try { args = JsonSerializer.Deserialize<JsonElement>(tc.Arguments); }
                catch { args = JsonSerializer.SerializeToElement(new { }); }

                await _publisher.Publish(new ToolCallStartedEvent(agentId, correlationId, tc.Name, tc.Arguments), ct);
                totalToolCalls++;

                var toolStart = Stopwatch.GetTimestamp();
                var toolDispatchResult = await registry.DispatchAsync(tc.Name, args, ct);
                var toolDurationMs = (int)Stopwatch.GetElapsedTime(toolStart).TotalMilliseconds;
                if (toolDispatchResult.IsSuccess
                    && registry.Tools.FirstOrDefault(t => t.Name == tc.Name) is ToolSearchTool searchTool)
                {
                    registry.RevealTools(searchTool.LastMatchedToolNames);
                }

                if (toolDispatchResult.IsFailure)
                {
                    await _publisher.Publish(new ToolCallCompletedEvent(agentId, correlationId, tc.Name, false, toolDispatchResult.Error.Message, toolDurationMs), ct);
                    history.Push(new ChatMessage { Role = "tool", Content = $"[error] {toolDispatchResult.Error.Message}", ToolCallId = tc.Id });
                    continue;
                }

                var result = toolDispatchResult.Value;
                var output = result.Success ? result.Output : $"[error] {result.Error}\n{result.Output}";

                var loopResult = loopDetector.Record(tc.Name, tc.Arguments, output);
                switch (loopResult)
                {
                    case LoopDetectionResult.BreakResult breakResult:
                        await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, breakResult.Message), ct);
                        var breakMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
                        await _publisher.Publish(new TurnCompletedEvent(agentId, correlationId, breakMs, i + 1, totalToolCalls), ct);
                        await FinishRunAsync("failed", error: breakResult.Message);
                        return;
                    case LoopDetectionResult.BlockResult blockResult:
                        output = $"BLOCKED: {blockResult.Message}";
                        break;
                }

                await _publisher.Publish(new ToolCallCompletedEvent(agentId, correlationId, tc.Name, result.Success, output, toolDurationMs), ct);

                var historyOutput = output.Length > 10000 ? output[..10000] + "\n[truncated]" : output;
                history.Push(new ChatMessage { Role = "tool", Content = historyOutput, ToolCallId = tc.Id });
            }
        }

        await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, $"Hit max iterations ({MaxIterations})"), ct);
        var maxMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
        await _publisher.Publish(new TurnCompletedEvent(agentId, correlationId, maxMs, MaxIterations, totalToolCalls), ct);
        await FinishRunAsync("failed", error: $"Hit max iterations ({MaxIterations})");
    }

    private record ParsedToolCall(string Id, string Name, string Arguments);
    private record SseResult(string? Content, List<ParsedToolCall> ToolCalls, int? InputTokens, int? OutputTokens);

    private static async Task<SseResult> ParseSseResponseAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        var content = new StringBuilder();
        var toolCalls = new Dictionary<int, (string Id, string Name, StringBuilder Args)>();
        int? inputTokens = null;
        int? outputTokens = null;

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!line.StartsWith("data: ")) continue;
            var data = line[6..];
            if (data == "[DONE]") break;

            try
            {
                using var doc = JsonDocument.Parse(data);
                var root = doc.RootElement;

                // Extract usage from the final chunk (OpenAI-compatible: usage.prompt_tokens / completion_tokens)
                if (root.TryGetProperty("usage", out var usage))
                {
                    if (usage.TryGetProperty("prompt_tokens", out var pt))
                        inputTokens = pt.GetInt32();
                    if (usage.TryGetProperty("completion_tokens", out var cpt))
                        outputTokens = cpt.GetInt32();
                }

                if (!root.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    continue;
                var delta = choices[0].GetProperty("delta");

                if (delta.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String)
                    content.Append(c.GetString());

                if (delta.TryGetProperty("tool_calls", out var tcs))
                {
                    foreach (var tc in tcs.EnumerateArray())
                    {
                        var idx = tc.GetProperty("index").GetInt32();
                        if (!toolCalls.ContainsKey(idx))
                        {
                            var id = tc.GetProperty("id").GetString() ?? "";
                            var name = tc.GetProperty("function").GetProperty("name").GetString() ?? "";
                            toolCalls[idx] = (id, name, new StringBuilder());
                        }
                        if (tc.TryGetProperty("function", out var fn) &&
                            fn.TryGetProperty("arguments", out var fnArgs) &&
                            fnArgs.ValueKind == JsonValueKind.String)
                            toolCalls[idx].Args.Append(fnArgs.GetString());
                    }
                }
            }
            catch (JsonException) { }
        }

        return new SseResult(
            content.Length > 0 ? content.ToString() : null,
            toolCalls.Values.Select(tc => new ParsedToolCall(tc.Id, tc.Name, tc.Args.ToString())).ToList(),
            inputTokens,
            outputTokens);
    }

    /// <summary>
    /// Extracts plain text from a Chat SDK JSON envelope (ChannelIn logs).
    /// Falls back to the raw string if not JSON or missing "text" field.
    /// </summary>
    private static string ExtractPlainText(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw[0] != '{') return raw;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("text", out var t) && t.ValueKind == JsonValueKind.String)
                return t.GetString() ?? raw;
        }
        catch (JsonException) { }
        return raw;
    }
}

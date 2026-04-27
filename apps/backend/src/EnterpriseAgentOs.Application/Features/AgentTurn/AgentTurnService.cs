using System.Diagnostics;
using MediatR;

namespace EnterpriseAgentOs.Application.Features.AgentTurn;

internal sealed class AgentTurnService
{
    private readonly IAgentRepository _agentRepository;
    private readonly IPublisher _publisher;
    private readonly LlmProviderDispatcher _llmProviderDispatcher;
    private readonly IProviderService _providerService;
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly SkillRuntimeClient _skillRuntimeClient;
    private readonly ISkillService _skillService;
    private readonly IAgentLogRepository _agentLogRepository;
    private readonly ILogger<AgentTurnService> _logger;

    private const int MaxIterations = 25;
    private const int MaxTokens = 8192;
    private const int KeepRecent = 4;
    private const int HistoryLoadLimit = 50;

    public AgentTurnService(
        IAgentRepository agents,
        IPublisher publisher,
        LlmProviderDispatcher llm,
        IProviderService providers,
        IAgentMemoryRepository memoryRepo,
        SkillRuntimeClient skillRuntime,
        ISkillService skillService,
        IAgentLogRepository agentLogRepository,
        ILogger<AgentTurnService> logger)
    {
        _agentRepository = agents;
        _publisher = publisher;
        _llmProviderDispatcher = llm;
        _providerService = providers;
        _agentMemoryRepository = memoryRepo;
        _skillRuntimeClient = skillRuntime;
        _skillService = skillService;
        _agentLogRepository = agentLogRepository;
        _logger = logger;
    }

    public async Task RunTurnAsync(Guid agentId, string userMessage, string correlationId, CancellationToken ct)
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

        await _publisher.Publish(new TurnStartedEvent(agentId, correlationId, userMessage), ct);

        using var pod = new PodConnection();
        var podStart = Stopwatch.GetTimestamp();
        const int maxRetries = 12;
        const int retryDelayMs = 5_000;
        AgentResult<bool> podResult = default;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            podResult = await pod.ConnectAsync(agent.PodName, "default", agentId, ct);
            if (podResult.IsSuccess) break;

            if (attempt == maxRetries)
            {
                await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, podResult.Error.Message), ct);
                return;
            }

            _logger.LogInformation(
                "Pod not ready for agent {AgentId}, retry {Attempt}/{Max} in {Delay}ms",
                agentId, attempt, maxRetries, retryDelayMs);
            await Task.Delay(retryDelayMs, ct);
        }
        await _publisher.Publish(new PodConnectedEvent(agentId, correlationId, (int)Stopwatch.GetElapsedTime(podStart).TotalMilliseconds), ct);

        var registry = ToolRegistry.Create(
            pod, _agentMemoryRepository, agentId, _skillRuntimeClient, _skillService, agent.SkillDetails);

        var history = new ConversationHistory();
        var loopDetector = new LoopDetector();
        var totalToolCalls = 0;

        // Seed history from recent logs — includes messages, tool calls, and tool results.
        // Deduplication: ChannelOut is skipped when MessageOut exists for the same correlationId.
        // ChannelIn raw JSON → plain text extraction.
        var recentLogs = await _agentLogRepository.ListAsync(agentId, before: null, limit: HistoryLoadLimit, ct);
        var ordered = recentLogs.OrderBy(l => l.Time).ToList();

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
            var systemPrompt = SystemPromptComposer.Compose(agent);

            history.PruneToolResults(maxResultChars: 500, keepRecentTurns: KeepRecent);
            history.Prune(MaxTokens, KeepRecent);

            var messages = new List<object> { new { role = "system", content = systemPrompt } };

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
                return;
            }

            var llmStart = Stopwatch.GetTimestamp();
            var llmResult = await _llmProviderDispatcher.DispatchAsync(provider, apiKey, agent.Model ?? "auto", requestBody, ct);
            if (llmResult.IsFailure)
            {
                await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, llmResult.Error.Message), ct);
                return;
            }

            var (assistantContent, toolCalls) = await ParseSseResponseAsync(llmResult.Value, ct);
            var llmDuration = (int)Stopwatch.GetElapsedTime(llmStart).TotalMilliseconds;
            await _publisher.Publish(new LlmCallCompletedEvent(agentId, correlationId, llmDuration), ct);

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
    }

    private record ParsedToolCall(string Id, string Name, string Arguments);

    private static async Task<(string? Content, List<ParsedToolCall> ToolCalls)> ParseSseResponseAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        var content = new StringBuilder();
        var toolCalls = new Dictionary<int, (string Id, string Name, StringBuilder Args)>();

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
                var choices = doc.RootElement.GetProperty("choices");
                if (choices.GetArrayLength() == 0) continue;
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

        return (content.Length > 0 ? content.ToString() : null,
            toolCalls.Values.Select(tc => new ParsedToolCall(tc.Id, tc.Name, tc.Args.ToString())).ToList());
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

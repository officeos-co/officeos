using System.Diagnostics;
using EnterpriseAgentOs.Application.Services.Agents.Tools;
using EnterpriseAgentOs.Infrastructure.Adapters.LlmProviders;
using EnterpriseAgentOs.Infrastructure.Adapters.SkillRuntime;

namespace EnterpriseAgentOs.Application.Services.Agents;

/// <summary>
/// Orchestrates the agent turn loop: compose prompt, call LLM, dispatch tools, loop.
/// Ported from agent-core's turn_loop.rs with all 12 tools, history pruning, and loop detection.
/// </summary>
public sealed class AgentTurnService
{
    private readonly IAgentRepository _agents;
    private readonly IAgentLogService _logs;
    private readonly PromptComposer _promptComposer;
    private readonly LlmProviderDispatcher _llm;
    private readonly IProviderService _providers;
    private readonly IAgentMemoryRepository _memoryRepo;
    private readonly IAgentSkillRepository _agentSkills;
    private readonly SkillRuntimeClient _skillRuntime;
    private readonly ISkillService _skillService;
    private readonly ILogger<AgentTurnService> _logger;

    private const int MaxIterations = 25;
    private const int MaxTokens = 8192;
    private const int KeepRecent = 4;

    public AgentTurnService(
        IAgentRepository agents,
        IAgentLogService logs,
        PromptComposer promptComposer,
        LlmProviderDispatcher llm,
        IProviderService providers,
        IAgentMemoryRepository memoryRepo,
        IAgentSkillRepository agentSkills,
        SkillRuntimeClient skillRuntime,
        ISkillService skillService,
        ILogger<AgentTurnService> logger)
    {
        _agents = agents;
        _logs = logs;
        _promptComposer = promptComposer;
        _llm = llm;
        _providers = providers;
        _memoryRepo = memoryRepo;
        _agentSkills = agentSkills;
        _skillRuntime = skillRuntime;
        _skillService = skillService;
        _logger = logger;
    }

    /// <summary>
    /// Run a single agent turn. Fires as an async Task — caller does not await completion.
    /// Ported from agent-core's run_turn().
    /// </summary>
    public async Task RunTurnAsync(Guid agentId, string userMessage, string correlationId, CancellationToken ct)
    {
        var turnStart = Stopwatch.GetTimestamp();
        var log = new TurnLogger(agentId, correlationId,
            record => _logs.AppendAsync(record).GetAwaiter().GetResult());

        var agent = await _agents.GetAsync(agentId, ct);
        if (agent is null)
        {
            log.Error($"Agent {agentId} not found");
            return;
        }

        if (string.IsNullOrEmpty(agent.PodName))
        {
            log.Error($"Agent {agentId} has no pod");
            return;
        }

        log.TurnStart(userMessage);

        // Fetch installed skills for the prompt and skill_exec tool.
        var installedSkills = await _agentSkills.ListSkillNamesByAgentAsync(agentId, ct);
        var skillDetails = await _agentSkills.ListSkillDetailsForAgentAsync(agentId, ct);

        // Connect to pod PTY.
        using var pod = new PodConnection();
        try
        {
            var podStart = Stopwatch.GetTimestamp();
            await pod.ConnectAsync(agent.PodName, "default", agentId, ct);
            log.PodConnected((int)Stopwatch.GetElapsedTime(podStart).TotalMilliseconds);
        }
        catch (Exception ex)
        {
            log.Error($"Failed to connect to pod: {ex.Message}");
            return;
        }

        // Build tool registry with all 13 tools — direct to skill-runtime, no HTTP loopback.
        var registry = ToolRegistry.Create(
            pod, _memoryRepo, agentId, _skillRuntime, _skillService, skillDetails);

        // Initialize conversation history and loop detector.
        var history = new ConversationHistory();
        var loopDetector = new LoopDetector();
        var totalToolCalls = 0;

        history.Push(new ChatMessage { Role = "user", Content = userMessage });

        // Turn loop: compose prompt → call LLM → dispatch tools → repeat.
        for (var i = 0; i < MaxIterations; i++)
        {
            // 1. Compose system prompt fresh each iteration (from agent-core's prompt.rs).
            var personalityPrompt = await _promptComposer.ComposeAsync(agentId, agent.Prompt, ct);
            var systemPrompt = string.Join("\n\n",
                new[]
                {
                    PromptSections.DateTime(),
                    personalityPrompt,
                    PromptSections.ToolHonesty(),
                    PromptSections.Safety(),
                    PromptSections.SkillsWithDescriptions(
                        skillDetails.Select(s => new SkillSummaryForPrompt(s.Name, s.Description)).ToList()),
                    PromptSections.Workspace(agent.Name),
                    PromptSections.Runtime(),
                }.Where(s => s is not null));

            // 2. Build messages array with pruning.
            history.Prune(MaxTokens, KeepRecent);

            var messages = new List<object>
            {
                new { role = "system", content = systemPrompt },
            };

            foreach (var msg in history.Messages)
            {
                if (msg.ToolCalls is { Count: > 0 })
                {
                    messages.Add(new
                    {
                        role = msg.Role,
                        content = msg.Content ?? "",
                        tool_calls = msg.ToolCalls.Select(tc => new
                        {
                            id = tc.Id,
                            type = "function",
                            function = new { name = tc.Name, arguments = tc.Arguments }
                        }).ToList(),
                    });
                }
                else if (msg.ToolCallId is not null)
                {
                    messages.Add(new
                    {
                        role = msg.Role,
                        content = msg.Content ?? "",
                        tool_call_id = msg.ToolCallId,
                    });
                }
                else
                {
                    messages.Add(new { role = msg.Role, content = msg.Content ?? "" });
                }
            }

            // 3. Call LLM.
            var requestBody = JsonSerializer.SerializeToElement(new
            {
                model = agent.Model ?? "auto",
                messages,
                tools = registry.GetSchemas(),
                stream = true,
            });

            var provider = agent.Provider;
            var apiKey = Domain.Services.KnownProviders.IsKeyless(provider)
                ? "platform"
                : await _providers.GetDecryptedKeyAsync(provider, ct) ?? "";

            HttpResponseMessage llmResponse;
            var llmStart = Stopwatch.GetTimestamp();
            try
            {
                llmResponse = await _llm.DispatchAsync(provider, apiKey, agent.Model ?? "auto", requestBody, ct);
            }
            catch (Exception ex)
            {
                log.Error($"LLM call failed: {ex.Message}");
                return;
            }

            // 4. Parse SSE response.
            var (assistantContent, toolCalls) = await ParseSseResponseAsync(llmResponse, ct);
            var llmDuration = (int)Stopwatch.GetElapsedTime(llmStart).TotalMilliseconds;
            log.LlmCallComplete(llmDuration);

            // 5. Log assistant response.
            if (!string.IsNullOrEmpty(assistantContent))
            {
                log.MessageOut(assistantContent);
            }

            // 6. Push assistant message to history.
            history.Push(new ChatMessage
            {
                Role = "assistant",
                Content = assistantContent,
                ToolCalls = toolCalls.Count > 0
                    ? toolCalls.Select(tc => new ChatToolCall
                    {
                        Id = tc.Id, Name = tc.Name, Arguments = tc.Arguments
                    }).ToList()
                    : null,
            });

            // 7. No tool calls = turn complete.
            if (toolCalls.Count == 0)
            {
                var totalMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
                log.TurnComplete(totalMs, i + 1, totalToolCalls);
                return;
            }

            // 8. Dispatch each tool call.
            foreach (var tc in toolCalls)
            {
                JsonElement args;
                try
                {
                    args = JsonSerializer.Deserialize<JsonElement>(tc.Arguments);
                }
                catch
                {
                    args = JsonSerializer.SerializeToElement(new { });
                }

                // Log tool call.
                log.ToolCallStart(tc.Name, tc.Arguments);
                totalToolCalls++;

                // Dispatch with timing.
                var toolStart = Stopwatch.GetTimestamp();
                var result = await registry.DispatchAsync(tc.Name, args, ct);
                var toolDurationMs = (int)Stopwatch.GetElapsedTime(toolStart).TotalMilliseconds;
                var output = result.Success ? result.Output : $"[error] {result.Error}\n{result.Output}";

                // Loop detection.
                var loopResult = loopDetector.Record(tc.Name, tc.Arguments, output);

                switch (loopResult)
                {
                    case LoopDetectionResult.BreakResult breakResult:
                        log.Error(breakResult.Message);
                        var breakMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
                        log.TurnComplete(breakMs, i + 1, totalToolCalls);
                        return; // Terminate turn.

                    case LoopDetectionResult.BlockResult blockResult:
                        output = $"BLOCKED: {blockResult.Message}";
                        break;

                    case LoopDetectionResult.WarningResult:
                        // Warning is included in output naturally.
                        break;
                }

                // Log tool result with duration.
                log.ToolCallResult(tc.Name, result.Success, output, toolDurationMs);

                // Push tool result to history (truncate for token budget).
                var historyOutput = output.Length > 10000 ? output[..10000] + "\n[truncated]" : output;
                history.Push(new ChatMessage
                {
                    Role = "tool",
                    Content = historyOutput,
                    ToolCallId = tc.Id,
                });
            }
        }

        log.Error($"Hit max iterations ({MaxIterations})");
        var maxMs = (int)Stopwatch.GetElapsedTime(turnStart).TotalMilliseconds;
        log.TurnComplete(maxMs, MaxIterations, totalToolCalls);
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
                            fn.TryGetProperty("arguments", out var args) &&
                            args.ValueKind == JsonValueKind.String)
                        {
                            toolCalls[idx].Args.Append(args.GetString());
                        }
                    }
                }
            }
            catch (JsonException) { }
        }

        var result = toolCalls.Values
            .Select(tc => new ParsedToolCall(tc.Id, tc.Name, tc.Args.ToString()))
            .ToList();

        return (content.Length > 0 ? content.ToString() : null, result);
    }
}

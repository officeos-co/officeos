namespace OffceOs.Application.Features.AgentUsage;

internal sealed class AgentUsageService : IAgentUsageService
{
    private readonly IAgentUsageRepository _agentUsageRepository;

    public AgentUsageService(IAgentUsageRepository agentUsageRepository)
    {
        _agentUsageRepository = agentUsageRepository;
    }

    public AgentUsageResolutionResult Resolve(AgentUsageResolveRequest request)
    {
        var estimatedInputTokens = EstimateTokens(request.RequestBody.GetRawText());
        var estimatedOutputTokens = EstimateTokens(
            $"{request.AssistantContent ?? string.Empty}\n{string.Join('\n', request.ToolCalls.Select(tc => $"{tc.Name} {tc.Arguments}"))}");

        var inputTokens = request.ReportedInputTokens is > 0 ? request.ReportedInputTokens.Value : estimatedInputTokens;
        var outputTokens = request.ReportedOutputTokens is > 0 ? request.ReportedOutputTokens.Value : estimatedOutputTokens;
        var estimated = request.ReportedInputTokens is not > 0 || request.ReportedOutputTokens is not > 0;
        var contextParts = AnalyzeContextParts(request.RequestBody, inputTokens, estimated).ToList();

        return new AgentUsageResolutionResult(
            inputTokens,
            outputTokens,
            request.CacheReadTokens,
            request.CacheWriteTokens,
            request.ReasoningTokens,
            estimated,
            ClassifyActivity(request.RequestBody, request.ToolCalls),
            contextParts);
    }

    public async Task<AgentUsageCallRecord> RecordCallAsync(AgentUsageRecordRequest request, CancellationToken ct = default)
    {
        var credits = ProviderRegistry.ToCredits(request.Model, request.Usage.TotalTokens);
        var record = new AgentUsageCallRecord
        {
            AgentId = request.AgentId,
            CorrelationId = request.CorrelationId,
            Provider = request.Provider,
            Model = request.Model,
            DurationMs = request.DurationMs,
            InputTokens = request.Usage.InputTokens,
            OutputTokens = request.Usage.OutputTokens,
            CacheReadTokens = request.Usage.CacheReadTokens,
            CacheWriteTokens = request.Usage.CacheWriteTokens,
            ReasoningTokens = request.Usage.ReasoningTokens,
            EstimatedTokens = request.Usage.EstimatedTokens,
            Credits = credits,
            Activity = request.Usage.Activity,
            Outcome = request.Outcome,
            RunId = request.RunId,
            ParentRunId = request.ParentRunId,
            ContextParts = request.Usage.ContextParts
                .Select(p => p with { Id = Guid.NewGuid(), CallId = Guid.Empty })
                .ToList(),
        };

        return await _agentUsageRepository.SaveAsync(record, ct);
    }

    private static IEnumerable<AgentUsageContextPartRecord> AnalyzeContextParts(JsonElement requestBody, int totalInputTokens, bool estimated)
    {
        var parts = new List<AgentUsageContextPartRecord>();
        var accountedChars = 0;

        if (requestBody.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var message in messages.EnumerateArray())
            {
                index++;
                var role = ReadString(message, "role") ?? "unknown";
                var content = ExtractContentText(message);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    accountedChars += content.Length;
                    parts.Add(new AgentUsageContextPartRecord
                    {
                        Kind = KindForRole(role),
                        Label = $"{role} message {index}",
                        Role = role,
                        Tokens = EstimateTokens(content),
                        EstimatedTokens = true,
                        CharacterCount = content.Length,
                    });
                }

                if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array)
                {
                    foreach (var toolCall in toolCalls.EnumerateArray())
                    {
                        var name = ReadNestedString(toolCall, "function", "name") ?? ReadString(toolCall, "name") ?? "tool_call";
                        var arguments = ReadNestedString(toolCall, "function", "arguments") ?? ReadString(toolCall, "arguments") ?? string.Empty;
                        accountedChars += arguments.Length;
                        parts.Add(new AgentUsageContextPartRecord
                        {
                            Kind = AgentUsageContextPartKinds.ToolCall,
                            Label = name,
                            Tool = name,
                            Tokens = EstimateTokens(arguments),
                            EstimatedTokens = true,
                            CharacterCount = arguments.Length,
                        });
                    }
                }
            }
        }

        if (requestBody.TryGetProperty("tools", out var tools) && tools.ValueKind == JsonValueKind.Array)
        {
            foreach (var tool in tools.EnumerateArray())
            {
                var raw = tool.GetRawText();
                var name = ReadNestedString(tool, "function", "name") ?? ReadString(tool, "name") ?? "tool_schema";
                accountedChars += raw.Length;
                parts.Add(new AgentUsageContextPartRecord
                {
                    Kind = AgentUsageContextPartKinds.ToolSchema,
                    Label = name,
                    Tool = name,
                    Tokens = EstimateTokens(raw),
                    EstimatedTokens = true,
                    CharacterCount = raw.Length,
                });
            }
        }

        var rawRequestChars = requestBody.GetRawText().Length;
        var overheadChars = Math.Max(0, rawRequestChars - accountedChars);
        if (overheadChars > 0)
        {
            parts.Add(new AgentUsageContextPartRecord
            {
                Kind = AgentUsageContextPartKinds.RequestOverhead,
                Label = "request_overhead",
                Tokens = EstimateTokens(new string('x', overheadChars)),
                EstimatedTokens = true,
                CharacterCount = overheadChars,
            });
        }

        var estimatedTotal = parts.Sum(p => p.Tokens);
        if (estimatedTotal <= 0 || totalInputTokens <= 0)
            return parts;

        var scale = (double)totalInputTokens / estimatedTotal;
        return parts.Select(p => p with
        {
            Tokens = Math.Max(1, (long)Math.Round(p.Tokens * scale, MidpointRounding.AwayFromZero)),
            EstimatedTokens = estimated || p.EstimatedTokens,
        });
    }

    private static string ClassifyActivity(JsonElement requestBody, IReadOnlyList<AgentUsageToolCallRequest> toolCalls)
    {
        var text = requestBody.GetRawText().ToLowerInvariant();
        var toolNames = toolCalls.Select(t => t.Name.ToLowerInvariant()).ToList();

        if (toolNames.Any(t => t.Contains("dispatch_agent", StringComparison.Ordinal) || t.Contains("task", StringComparison.Ordinal)))
            return AgentUsageActivityKinds.Delegation;
        if (toolNames.Any(t => t.Contains("git", StringComparison.Ordinal)) || text.Contains("git ", StringComparison.Ordinal))
            return AgentUsageActivityKinds.GitOps;
        if (text.Contains("test", StringComparison.Ordinal) || toolNames.Any(t => t.Contains("test", StringComparison.Ordinal)))
            return AgentUsageActivityKinds.Testing;
        if (text.Contains("debug", StringComparison.Ordinal) || text.Contains("error", StringComparison.Ordinal) || text.Contains("fix", StringComparison.Ordinal))
            return AgentUsageActivityKinds.Debugging;
        if (text.Contains("refactor", StringComparison.Ordinal) || text.Contains("rename", StringComparison.Ordinal))
            return AgentUsageActivityKinds.Refactoring;
        if (text.Contains("implement", StringComparison.Ordinal) || text.Contains("feature", StringComparison.Ordinal) || text.Contains("add ", StringComparison.Ordinal))
            return AgentUsageActivityKinds.FeatureDevelopment;
        if (text.Contains("plan", StringComparison.Ordinal) || text.Contains("design", StringComparison.Ordinal))
            return AgentUsageActivityKinds.Planning;
        if (text.Contains("brainstorm", StringComparison.Ordinal) || text.Contains("idea", StringComparison.Ordinal))
            return AgentUsageActivityKinds.Brainstorming;
        if (text.Contains("build", StringComparison.Ordinal) || text.Contains("deploy", StringComparison.Ordinal))
            return AgentUsageActivityKinds.BuildDeploy;
        if (toolNames.Any(t => t.Contains("read", StringComparison.Ordinal) || t.Contains("search", StringComparison.Ordinal) || t.Contains("glob", StringComparison.Ordinal)))
            return AgentUsageActivityKinds.Exploration;
        if (toolNames.Any(t => t.Contains("write", StringComparison.Ordinal) || t.Contains("edit", StringComparison.Ordinal)))
            return AgentUsageActivityKinds.Coding;
        if (toolCalls.Count == 0)
            return AgentUsageActivityKinds.Conversation;

        return AgentUsageActivityKinds.General;
    }

    private static string KindForRole(string role) => role switch
    {
        "system" => AgentUsageContextPartKinds.SystemPrompt,
        "user" => AgentUsageContextPartKinds.UserMessage,
        "assistant" => AgentUsageContextPartKinds.AssistantMessage,
        "tool" => AgentUsageContextPartKinds.ToolResult,
        _ => AgentUsageContextPartKinds.Other,
    };

    private static string ExtractContentText(JsonElement message)
    {
        if (!message.TryGetProperty("content", out var content))
            return string.Empty;

        return content.ValueKind switch
        {
            JsonValueKind.String => content.GetString() ?? string.Empty,
            JsonValueKind.Array => string.Join('\n', content.EnumerateArray().Select(ExtractContentBlockText).Where(s => !string.IsNullOrWhiteSpace(s))),
            JsonValueKind.Object => ExtractContentBlockText(content),
            _ => content.GetRawText(),
        };
    }

    private static string ExtractContentBlockText(JsonElement block)
    {
        if (block.ValueKind == JsonValueKind.String)
            return block.GetString() ?? string.Empty;

        if (block.ValueKind != JsonValueKind.Object)
            return block.GetRawText();

        if (block.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
            return text.GetString() ?? string.Empty;

        if (block.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String)
            return content.GetString() ?? string.Empty;

        return block.GetRawText();
    }

    private static string? ReadString(JsonElement element, string property)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static string? ReadNestedString(JsonElement element, string property, string nestedProperty)
    {
        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var nested)
            ? ReadString(nested, nestedProperty)
            : null;
    }

    private static int EstimateTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        return Math.Max(1, (text.Length + 3) / 4);
    }
}

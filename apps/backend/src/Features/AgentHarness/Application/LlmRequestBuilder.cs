using OffceOs.Features.Agents.Domain;
using OffceOs.Features.AgentHarness.Application.Tools;
namespace OffceOs.Features.AgentHarness.Application;

/// <summary>
/// Builds OpenAI-compatible LLM request payloads from an agent, history, and available tools.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> pruning conversation history for the request budget,
/// composing the system prompt, serializing chat messages, and adding tool schemas.</para>
/// <para><strong>Responsible only for:</strong> request body construction. It does not fetch API keys,
/// dispatch providers, parse SSE responses, record billing, or publish events.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when prompt assembly,
/// request schema, message serialization, tool schema inclusion, or context pruning policy changes.</para>
/// </remarks>
internal sealed class LlmRequestBuilder
{
    private const int MaxTokens = 8192;
    private const int KeepRecent = 4;

    public JsonElement Build(AgentRecord agent, ConversationHistory history, ToolRegistry registry)
    {
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
                messages.Add(new { role = msg.Role, content = msg.Content ?? "", tool_call_id = msg.ToolCallId });
            }
            else
            {
                messages.Add(new { role = msg.Role, content = msg.Content ?? "" });
            }
        }

        var effort = ExtractReasoningEffort(agent.Prompt);
        var body = new Dictionary<string, object?>
        {
            ["model"] = agent.Model ?? "auto",
            ["messages"] = messages,
            ["tools"] = registry.GetSchemas(),
            ["stream"] = true,
        };
        if (effort is not null)
            body["reasoning_effort"] = effort;

        return JsonSerializer.SerializeToElement(body);
    }

    private static string? ExtractReasoningEffort(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt)) return null;
        var match = Regex.Match(prompt, @"Coding effort:\s*(low|medium|high)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
    }
}

using OffceOs.Features.ResourceLogs.Domain;

namespace OffceOs.Features.AgentHarness.Application;

/// <summary>
/// Builds the LLM conversation history for a turn.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> loading compacted conversation context, replaying relevant
/// structured logs into `ConversationHistory`, and appending the current user message.</para>
/// <para><strong>Responsible only for:</strong> history reconstruction. It does not prune for request
/// budgets, compose system prompts, call providers, execute tools, or publish events.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when persisted log types,
/// replay ordering, log deduplication, or conversation reconstruction rules change.</para>
/// </remarks>
internal sealed class TurnContextBuilder
{
    private readonly ConversationCompactionService _conversationCompactionService;

    public TurnContextBuilder(ConversationCompactionService compactionService)
    {
        _conversationCompactionService = compactionService;
    }

    public async Task<ConversationHistory> BuildAsync(Guid agentId, string correlationId, string userMessage, CancellationToken ct)
    {
        var history = new ConversationHistory();
        var contextWindow = await _conversationCompactionService.LoadAsync(agentId, correlationId, ct);
        var ordered = contextWindow.Logs
            .Where(l => l.CorrelationId != correlationId)
            .OrderBy(l => l.Time)
            .ToList();

        if (!string.IsNullOrWhiteSpace(contextWindow.Summary))
        {
            history.Push(new ChatMessage
            {
                Role = "user",
                Content = $"<persisted-conversation-summary>\n{contextWindow.Summary}\n</persisted-conversation-summary>"
            });
        }

        var outCorrelations = new HashSet<string>(
            ordered.Where(l => l.Type == ResourceLogType.MessageOut && l.CorrelationId is not null)
                .Select(l => l.CorrelationId!));

        var pendingToolCallIds = new Queue<string>();

        foreach (var log in ordered)
        {
            switch (log.Type)
            {
                case ResourceLogType.MessageIn:
                    history.Push(new ChatMessage { Role = "user", Content = log.Content ?? "" });
                    break;
                case ResourceLogType.ChannelIn:
                    history.Push(new ChatMessage { Role = "user", Content = ExtractPlainText(log.Content ?? "") });
                    break;
                case ResourceLogType.MessageOut:
                    history.Push(new ChatMessage { Role = "assistant", Content = log.Content ?? "" });
                    break;
                case ResourceLogType.ChannelOut when log.CorrelationId is not null && outCorrelations.Contains(log.CorrelationId):
                    break;
                case ResourceLogType.ChannelOut:
                    history.Push(new ChatMessage { Role = "assistant", Content = log.Content ?? "" });
                    break;
                case ResourceLogType.ToolCall:
                    var tcId = log.Id.ToString("N");
                    pendingToolCallIds.Enqueue(tcId);
                    history.Push(new ChatMessage
                    {
                        Role = "assistant",
                        Content = null,
                        ToolCalls =
                        [
                            new ChatToolCall
                            {
                                Id = tcId,
                                Name = log.Tool ?? "unknown",
                                Arguments = log.Content ?? "{}"
                            }
                        ],
                    });
                    break;
                case ResourceLogType.ToolResult:
                    var matchId = pendingToolCallIds.Count > 0 ? pendingToolCallIds.Dequeue() : log.Id.ToString("N");
                    history.Push(new ChatMessage { Role = "tool", Content = log.Content ?? "", ToolCallId = matchId });
                    break;
            }
        }

        history.Push(new ChatMessage { Role = "user", Content = userMessage });
        return history;
    }

    private static string ExtractPlainText(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw[0] != '{') return raw;
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                return text.GetString() ?? raw;
        }
        catch (JsonException) { }
        return raw;
    }
}

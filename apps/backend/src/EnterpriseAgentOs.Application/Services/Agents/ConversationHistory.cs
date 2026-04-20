using System.Text.Json;

namespace EnterpriseAgentOs.Application.Services.Agents;

/// <summary>
/// Manages conversation messages with token-based pruning.
/// Ported from agent-core's ConversationHistory.
/// </summary>
public sealed class ConversationHistory
{
    private readonly List<ChatMessage> _messages = new();

    /// <summary>Token estimation: (total_chars / 4.0) * 1.2 overhead</summary>
    private const double CharsPerToken = 4.0;
    private const double OverheadMultiplier = 1.2;

    public IReadOnlyList<ChatMessage> Messages => _messages;
    public int Count => _messages.Count;

    public void Push(ChatMessage message) => _messages.Add(message);

    public void Clear() => _messages.Clear();

    /// <summary>
    /// Prune history to fit within token budget.
    /// Phase 1: Collapse old assistant+tool pairs into summaries.
    /// Phase 2: Drop oldest unprotected messages.
    /// Ported from agent-core's history.rs prune().
    /// </summary>
    public void Prune(int maxTokens = 8192, int keepRecent = 4)
    {
        // Phase 1: Collapse old assistant+tool result pairs
        var protectedStart = Math.Max(0, _messages.Count - keepRecent);
        for (var i = 0; i < protectedStart - 1; i++)
        {
            if (_messages[i].Role == "assistant" &&
                _messages[i].ToolCalls?.Count > 0 &&
                i + 1 < protectedStart &&
                _messages[i + 1].Role == "tool")
            {
                var summary = _messages[i + 1].Content?.Length > 100
                    ? $"[Tool result: {_messages[i + 1].Content[..100]}...]"
                    : $"[Tool result: {_messages[i + 1].Content}]";
                _messages[i] = _messages[i] with { Content = summary, ToolCalls = null };
                _messages.RemoveAt(i + 1);
                protectedStart = Math.Max(0, _messages.Count - keepRecent);
                i--; // Re-check this index
            }
        }

        // Phase 2: Drop oldest unprotected messages until under budget
        while (EstimateTokens() > maxTokens && _messages.Count > keepRecent + 1)
        {
            // Find first non-system message that's not protected
            var removeIdx = -1;
            protectedStart = Math.Max(0, _messages.Count - keepRecent);
            for (var i = 0; i < protectedStart; i++)
            {
                if (_messages[i].Role != "system")
                {
                    removeIdx = i;
                    break;
                }
            }
            if (removeIdx < 0) break;
            _messages.RemoveAt(removeIdx);
        }
    }

    /// <summary>
    /// OpenClaw-style context pruning: soft-trims old tool results to reduce token usage.
    /// Preserves recent turns byte-for-byte; older tool results get ellipsis-trimmed;
    /// very old results get hard-cleared to a placeholder.
    /// </summary>
    public void PruneToolResults(int maxResultChars = 500, int keepRecentTurns = 4, int hardClearAfterTurns = 8)
    {
        var protectedStart = Math.Max(0, _messages.Count - keepRecentTurns);
        var hardClearBoundary = Math.Max(0, _messages.Count - hardClearAfterTurns);

        for (var i = 0; i < protectedStart; i++)
        {
            if (_messages[i].Role != "tool" || _messages[i].Content is null) continue;
            var content = _messages[i].Content!;
            if (content == "[tool result cleared]") continue; // already cleared

            if (i < hardClearBoundary)
            {
                // Hard-clear very old tool results
                _messages[i] = _messages[i] with { Content = "[tool result cleared]" };
            }
            else if (content.Length > maxResultChars)
            {
                // Soft-trim: keep beginning + end with ellipsis
                var half = maxResultChars / 2;
                var trimmed = content[..half] + "\n...\n" + content[^half..];
                _messages[i] = _messages[i] with { Content = trimmed };
            }
        }
    }

    private int EstimateTokens()
    {
        var totalChars = 0;
        foreach (var msg in _messages)
        {
            totalChars += msg.Content?.Length ?? 0;
            totalChars += msg.Role.Length;
            if (msg.ToolCalls != null)
            {
                foreach (var tc in msg.ToolCalls)
                    totalChars += tc.Arguments.Length + tc.Name.Length;
            }
        }
        return (int)(totalChars / CharsPerToken * OverheadMultiplier);
    }
}

/// <summary>Chat message for the LLM conversation.</summary>
public record ChatMessage
{
    public required string Role { get; init; }
    public string? Content { get; init; }
    public List<ChatToolCall>? ToolCalls { get; init; }
    public string? ToolCallId { get; init; }
}

/// <summary>Tool call from the assistant.</summary>
public record ChatToolCall
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Arguments { get; init; }
}

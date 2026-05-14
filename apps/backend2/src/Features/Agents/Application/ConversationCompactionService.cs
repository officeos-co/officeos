namespace OffceOs.Application.Features.Agents;

internal sealed record ConversationContextWindow(string? Summary, IReadOnlyList<AgentLogRecord> Logs);

internal sealed class ConversationCompactionService
{
    private const int LoadLimit = 240;
    private const int CompactAfterLogCount = 120;
    private const int PreserveTailCount = 50;
    private const int SummaryMaxChars = 12_000;

    private readonly IAgentSessionContextRepository _agentSessionContextRepository;
    private readonly IAgentLogRepository _agentLogRepository;
    private readonly IPublisher _publisher;

    public ConversationCompactionService(
        IAgentSessionContextRepository contextRepository,
        IAgentLogRepository logRepository,
        IPublisher publisher)
    {
        _agentSessionContextRepository = contextRepository;
        _agentLogRepository = logRepository;
        _publisher = publisher;
    }

    public async Task<ConversationContextWindow> LoadAsync(Guid agentId, string correlationId, CancellationToken ct)
    {
        var context = await _agentSessionContextRepository.GetByAsync(new AgentSessionContextFilter { AgentId = agentId }, ct);
        var logs = await _agentLogRepository.ListAsync(
            new AgentLogFilter { AgentId = agentId },
            new AgentLogListOptions
            {
                AfterLogId = context?.LastCompactedLogId,
                Limit = LoadLimit,
                Sort = AgentLogSort.TimeAscending,
            },
            ct);

        if (logs.Count <= CompactAfterLogCount)
            return new ConversationContextWindow(context?.Summary, logs);

        var compactable = logs.Take(Math.Max(0, logs.Count - PreserveTailCount)).ToList();
        var tail = logs.Skip(compactable.Count).ToList();
        if (compactable.Count == 0)
            return new ConversationContextWindow(context?.Summary, logs);

        var summary = BuildSummary(context?.Summary, compactable);
        var preTokens = EstimateTokens((context?.Summary ?? "") + string.Join('\n', compactable.Select(l => l.Content)));
        var postTokens = EstimateTokens(summary);
        var boundary = compactable[^1];

        await _agentSessionContextRepository.UpsertAsync(new AgentSessionContextRecord
        {
            AgentId = agentId,
            Summary = summary,
            LastCompactedLogId = boundary.Id,
            LastCompactedAt = DateTime.UtcNow,
            PreCompactTokens = preTokens,
            PostCompactTokens = postTokens,
            CompactionVersion = 1,
        }, ct);

        await _publisher.Publish(new ConversationCompactedEvent(
            agentId,
            correlationId,
            boundary.Id,
            preTokens,
            postTokens), ct);

        return new ConversationContextWindow(summary, tail);
    }

    private static string BuildSummary(string? previousSummary, IReadOnlyList<AgentLogRecord> logs)
    {
        var userGoals = logs.Where(l => l.Type is AgentLogType.MessageIn or AgentLogType.ChannelIn)
            .Select(l => Trim(l.Content, 400))
            .TakeLast(8);
        var assistantState = logs.Where(l => l.Type == AgentLogType.MessageOut)
            .Select(l => Trim(l.Content, 400))
            .TakeLast(8);
        var tools = logs.Where(l => l.Type is AgentLogType.ToolCall or AgentLogType.ToolResult)
            .Select(l => $"{l.Type}: {l.Tool ?? "unknown"} {Trim(l.Content, 240)}")
            .TakeLast(16);
        var errors = logs.Where(l => l.Type.ToString().StartsWith("Error", StringComparison.Ordinal))
            .Select(l => Trim(l.Content, 300))
            .TakeLast(6);

        var sb = new StringBuilder();
        sb.AppendLine("# Persisted Conversation Context");
        if (!string.IsNullOrWhiteSpace(previousSummary))
        {
            sb.AppendLine();
            sb.AppendLine("## Previous Summary");
            sb.AppendLine(Trim(previousSummary, 4_000));
        }

        sb.AppendLine();
        sb.AppendLine("## Primary User Goal And Recent Requests");
        foreach (var item in userGoals) sb.AppendLine($"- {item}");

        sb.AppendLine();
        sb.AppendLine("## Key Assistant State And Decisions");
        foreach (var item in assistantState) sb.AppendLine($"- {item}");

        sb.AppendLine();
        sb.AppendLine("## Commands, Tools, Tests, And Results");
        foreach (var item in tools) sb.AppendLine($"- {item}");

        if (errors.Any())
        {
            sb.AppendLine();
            sb.AppendLine("## Errors Or Blockers");
            foreach (var item in errors) sb.AppendLine($"- {item}");
        }

        sb.AppendLine();
        sb.AppendLine("## Exact Next Step");
        sb.AppendLine("- Continue from the preserved recent tail and satisfy the latest user request.");

        return Trim(sb.ToString(), SummaryMaxChars);
    }

    private static int EstimateTokens(string text) => Math.Max(1, text.Length / 4);

    private static string Trim(string value, int maxChars)
        => value.Length <= maxChars ? value : value[..maxChars] + "\n[truncated]";
}

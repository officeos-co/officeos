using MediatR;

namespace EnterpriseAgentOs.Application.Features.Analytics;

internal sealed class AgentLogService : IAgentLogService
{
    private static readonly string[] SecretKeySubstrings =
    [
        "apikey", "token", "secret", "password", "credential", "key"
    ];

    private readonly IAgentLogRepository _agentLogRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IPublisher _publisher;
    private readonly ITopicEventSender _topicEventSender;
    private readonly ILogger<AgentLogService> _logger;

    public AgentLogService(
        IAgentLogRepository agentLogRepository,
        IAgentRepository agentRepository,
        IPublisher publisher,
        ITopicEventSender topicEventSender,
        ILogger<AgentLogService> logger)
    {
        _agentLogRepository = agentLogRepository;
        _agentRepository = agentRepository;
        _publisher = publisher;
        _topicEventSender = topicEventSender;
        _logger = logger;
    }

    public Task<List<AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default)
        => _agentLogRepository.ListAsync(agentId, before, limit, ct);

    public Task<List<AgentLogRecord>> ListUsageAsync(Guid ownerId, DateTime fromInclusive, DateTime toExclusive, CancellationToken ct = default)
        => _agentLogRepository.ListUsageAsync(ownerId, fromInclusive, toExclusive, ct);

    public async Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersInput filters, CancellationToken ct = default)
    {
        var limit = Math.Clamp(filters.Limit, 1, 200);
        var skip = Math.Max(filters.Skip, 0);
        var (rows, total) = await _agentLogRepository.ListGlobalAsync(filters.Search, filters.AgentName, filters.Type, skip, limit, ct);
        var items = rows.Select(r => r.Log.ToDto(r.AgentName)).ToList();
        return new GlobalLogsPage(items, total);
    }

    public async Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default)
    {
        var saved = await _agentLogRepository.AppendAsync(record, ct);
        if (saved.AgentId is { } agentId)
        {
            await _topicEventSender.SendAsync(AgentLogTopics.AgentLogAppended(agentId), saved.ToDto(), ct);
        }
        return saved;
    }

    public async Task<AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId }, ct);
        if (agent is null) throw new InvalidOperationException($"Agent {agentId} not found");

        var correlationId = Guid.NewGuid().ToString("N");

        var record = await AppendAsync(AgentLogRecord.MessageIn(agentId, content, correlationId));

        if (string.IsNullOrEmpty(agent.PodName))
        {
            const string message = "Agent runtime is unavailable: no pod is assigned. The message was saved, but no agent turn could be started.";
            _logger.LogWarning("Agent {AgentId} has no pod, message queued only", agentId);
            await _publisher.Publish(new AgentErrorOccurredEvent(agentId, correlationId, message), ct);
            return record;
        }

        await _publisher.Publish(new MessageReceivedEvent(agentId, content, correlationId), ct);

        return record;
    }

    // ── Audit (merged from Entities/Audit) ───────────────────────────────

    public async Task RecordToolCallAsync(
        Guid agentId,
        Guid? userId,
        string skillName,
        string action,
        string paramsJson,
        string? resultSummary,
        long durationMs,
        CancellationToken ct = default)
    {
        var redacted = RedactSecrets(paramsJson);
        var correlationId = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        var toolCall = AgentLogRecord.ToolCallEntry(agentId, action, redacted, correlationId, now, skillName);

        var toolResult = AgentLogRecord.ToolResultEntry(agentId, action, resultSummary ?? string.Empty,
            correlationId, now.AddMilliseconds(1),
            new TokenUsage(null, null, (int)Math.Min(durationMs, int.MaxValue)), skillName);

        await _agentLogRepository.AppendPairAsync(toolCall, toolResult, ct);
    }

    public async Task<(List<AgentLogRecord> Items, int Total)> GetAuditLogAsync(
        Guid agentId, int limit, int offset, CancellationToken ct = default)
    {
        return await _agentLogRepository.GetToolCallsAsync(agentId, limit, offset, ct);
    }

    public async Task<Dictionary<string, AgentLogRecord>> GetResultsByCorrelationAsync(
        Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default)
    {
        return await _agentLogRepository.GetResultsByCorrelationAsync(agentId, correlationIds, ct);
    }

    // ── Secret redaction ─────────────────────────────────────────────────

    private string RedactSecrets(string paramsJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(paramsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return paramsJson;

            var dict = new Dictionary<string, object?>();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (IsSecretKey(prop.Name))
                    dict[prop.Name] = "[REDACTED]";
                else
                    dict[prop.Name] = JsonElementToObject(prop.Value);
            }
            return JsonSerializer.Serialize(dict);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to redact params JSON; storing as-is");
            return paramsJson;
        }
    }

    private static bool IsSecretKey(string key)
    {
        var lower = key.ToLowerInvariant();
        return SecretKeySubstrings.Any(s => lower.Contains(s));
    }

    private static object? JsonElementToObject(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.TryGetInt64(out var l) ? (object)l : el.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Object => el.EnumerateObject().ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
        JsonValueKind.Array => el.EnumerateArray().Select(JsonElementToObject).ToList(),
        _ => el.GetRawText(),
    };
}

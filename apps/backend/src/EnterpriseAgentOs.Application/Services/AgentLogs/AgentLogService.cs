namespace EnterpriseAgentOs.Application.Services.AgentLogs;

public sealed class AgentLogService : IAgentLogService
{
    private static readonly string[] SecretKeySubstrings =
    [
        "apikey", "token", "secret", "password", "credential", "key"
    ];

    private readonly IAgentLogRepository _repo;
    private readonly ITopicEventSender _sender;
    private readonly IAgentRepository _agents;
    private readonly IPostHogService _analytics;
    private readonly ILogger<AgentLogService> _logger;

    public AgentLogService(
        IAgentLogRepository repo,
        ITopicEventSender sender,
        IAgentRepository agents,
        IPostHogService analytics,
        ILogger<AgentLogService> logger)
    {
        _repo = repo;
        _sender = sender;
        _agents = agents;
        _analytics = analytics;
        _logger = logger;
    }

    public Task<List<EnterpriseAgentOs.Domain.Models.AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default)
        => _repo.ListAsync(agentId, before, limit, ct);

    public async Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersInput filters, CancellationToken ct = default)
    {
        var limit = Math.Clamp(filters.Limit, 1, 200);
        var skip = Math.Max(filters.Skip, 0);
        var (rows, total) = await _repo.ListGlobalAsync(filters.Search, filters.AgentName, filters.Type, skip, limit, ct);
        var items = rows.Select(r => r.Log.ToDto(r.AgentName)).ToList();
        return new GlobalLogsPage(items, total);
    }

    public async Task<EnterpriseAgentOs.Domain.Models.AgentLogRecord> AppendAsync(EnterpriseAgentOs.Domain.Models.AgentLogRecord record, CancellationToken ct = default)
    {
        var saved = await _repo.AppendAsync(record, ct);
        await _sender.SendAsync($"agent-log:{saved.AgentId}", saved.ToDto(), ct);
        return saved;
    }

    public async Task<EnterpriseAgentOs.Domain.Models.AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default)
    {
        var agent = await _agents.GetAsync(agentId, ct)
            ?? throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Agent '{agentId}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());

        var record = new EnterpriseAgentOs.Domain.Models.AgentLogRecord
        {
            AgentId = agent.Id,
            Time = DateTime.UtcNow,
            Type = EnterpriseAgentOs.Domain.Models.AgentLogType.MessageIn,
            Content = content,
            CorrelationId = Guid.NewGuid().ToString(),
        };

        var saved = await AppendAsync(record, ct);

        await KickAgentPodAsync(agent, content, record.CorrelationId!, ct);

        await _analytics.CaptureAsync(
            userId.ToString(),
            "agent_message_sent",
            new Dictionary<string, object?>
            {
                ["agent_id"] = agentId,
                ["content_length"] = content?.Length ?? 0,
            },
            ct);

        return saved;
    }

    // ── Pod kick ─────────────────────────────────────────────────────────

    private async Task KickAgentPodAsync(
        EnterpriseAgentOs.Domain.Models.AgentRecord agent,
        string content,
        string correlationId,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(agent.PodName))
        {
            _logger.LogWarning("Agent {AgentId} has no pod deployed — message queued only", agent.Id);
            await AppendAsync(new EnterpriseAgentOs.Domain.Models.AgentLogRecord
            {
                AgentId = agent.Id,
                Time = DateTime.UtcNow,
                Type = EnterpriseAgentOs.Domain.Models.AgentLogType.System,
                Content = "Agent pod not deployed, message queued",
                CorrelationId = correlationId,
            }, ct);
            return;
        }

        try
        {
            using var ws = new System.Net.WebSockets.ClientWebSocket();
            var uri = new Uri($"ws://{agent.PodName}.default.svc.cluster.local:42617/ws?token={agent.Id}");
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            connectCts.CancelAfter(TimeSpan.FromSeconds(5));
            await ws.ConnectAsync(uri, connectCts.Token);

            var payload = JsonSerializer.Serialize(new
            {
                type = "user_message",
                text = content,
                id = correlationId,
            });
            var bytes = System.Text.Encoding.UTF8.GetBytes(payload);
            await ws.SendAsync(
                new ArraySegment<byte>(bytes),
                System.Net.WebSockets.WebSocketMessageType.Text,
                endOfMessage: true,
                ct);

            await ws.CloseOutputAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver message to agent pod {PodName}", agent.PodName);
            await AppendAsync(new EnterpriseAgentOs.Domain.Models.AgentLogRecord
            {
                AgentId = agent.Id,
                Time = DateTime.UtcNow,
                Type = EnterpriseAgentOs.Domain.Models.AgentLogType.Error,
                Content = $"Failed to deliver message to agent pod: {ex.Message}",
                CorrelationId = correlationId,
            }, ct);
        }
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

        var toolCall = new EnterpriseAgentOs.Domain.Models.AgentLogRecord
        {
            AgentId = agentId,
            Time = now,
            Type = EnterpriseAgentOs.Domain.Models.AgentLogType.ToolCall,
            Tool = action,
            Integration = skillName,
            Content = redacted,
            CorrelationId = correlationId,
        };

        var toolResult = new EnterpriseAgentOs.Domain.Models.AgentLogRecord
        {
            AgentId = agentId,
            Time = now.AddMilliseconds(1),
            Type = EnterpriseAgentOs.Domain.Models.AgentLogType.ToolResult,
            Tool = action,
            Integration = skillName,
            Content = resultSummary ?? string.Empty,
            DurationMs = (int)Math.Min(durationMs, int.MaxValue),
            CorrelationId = correlationId,
        };

        await _repo.AppendPairAsync(toolCall, toolResult, ct);
        await _sender.SendAsync($"agent-log:{agentId}", toolCall.ToDto(), ct);
        await _sender.SendAsync($"agent-log:{agentId}", toolResult.ToDto(), ct);
    }

    public async Task<(List<EnterpriseAgentOs.Domain.Models.AgentLogRecord> Items, int Total)> GetAuditLogAsync(
        Guid agentId, int limit, int offset, CancellationToken ct = default)
    {
        return await _repo.GetToolCallsAsync(agentId, limit, offset, ct);
    }

    public async Task<Dictionary<string, EnterpriseAgentOs.Domain.Models.AgentLogRecord>> GetResultsByCorrelationAsync(
        Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default)
    {
        return await _repo.GetResultsByCorrelationAsync(agentId, correlationIds, ct);
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

namespace OffceOs.Application.Features.Observability;

internal sealed class AgentLogService : IAgentLogService
{
    private const int ActivityPreviewMaxLength = 240;

    private static readonly string[] SecretKeySubstrings =
    [
        "apikey", "token", "secret", "password", "credential", "key"
    ];

    private readonly IAgentLogRepository _agentLogRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly IPublisher _publisher;
    private readonly ILogger<AgentLogService> _logger;

    public AgentLogService(
        IAgentLogRepository agentLogRepository,
        IAgentRepository agentRepository,
        IPublisher publisher,
        ILogger<AgentLogService> logger)
    {
        _agentLogRepository = agentLogRepository;
        _agentRepository = agentRepository;
        _publisher = publisher;
        _logger = logger;
    }

    public IQueryable<AgentLogProjection> AgentLogs(Guid agentId, Guid? workspaceId = null) =>
        ToProjectionQuery(
            ExcludePodStartupLogs(_agentLogRepository.Query(new AgentLogFilter { AgentId = agentId, WorkspaceId = workspaceId }))
                .OrderBy(l => l.Time)
                .ThenBy(l => l.Id));

    public IQueryable<AgentLogProjection> ChannelLogs(Guid channelConnectionId, Guid? workspaceId = null) =>
        ToProjectionQuery(
            ExcludePodStartupLogs(_agentLogRepository.Query(new AgentLogFilter { ChannelConnectionId = channelConnectionId, WorkspaceId = workspaceId }))
                .OrderBy(l => l.Time)
                .ThenBy(l => l.Id));

    public IQueryable<AgentLogProjection> GlobalLogs(GlobalLogFiltersRequest filters, Guid? workspaceId = null) =>
        ToProjectionQuery(
            ExcludePodStartupLogs(_agentLogRepository.Query(new AgentLogFilter
                {
                    WorkspaceId = workspaceId,
                    Search = filters.Search,
                    AgentName = filters.AgentName,
                    Type = filters.Type,
                }))
                .OrderByDescending(l => l.Time)
                .ThenByDescending(l => l.Id));

    public IQueryable<AuditEntry> AuditLog(Guid agentId, Guid? workspaceId = null)
    {
        var toolCalls = _agentLogRepository.Query(new AgentLogFilter
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = AgentLogType.ToolCall,
        });
        var results = _agentLogRepository.Query(new AgentLogFilter
        {
            AgentId = agentId,
            WorkspaceId = workspaceId,
            Type = AgentLogType.ToolResult,
        });

        return
            from call in toolCalls
            join result in results on call.CorrelationId equals result.CorrelationId into pairedResults
            from result in pairedResults.DefaultIfEmpty()
            orderby call.Time descending, call.Id descending
            select new AuditEntry(
                call.Id,
                call.AgentId,
                null,
                call.Integration ?? string.Empty,
                call.Tool ?? string.Empty,
                call.Content,
                result == null ? null : result.Content,
                result == null
                    ? call.Usage.DurationMs ?? 0
                    : result.Usage.DurationMs ?? call.Usage.DurationMs ?? 0,
                call.Time);
    }

    public Task<List<AgentLogRecord>> ListForAgentAsync(Guid agentId, DateTime? before, int limit, CancellationToken ct = default)
        => _agentLogRepository.ListAsync(
            new AgentLogFilter { AgentId = agentId, Before = before },
            new AgentLogListOptions { Limit = limit, Sort = AgentLogSort.TimeDescending },
            ct);

    public Task<List<AgentLogRecord>> ListForRunAsync(Guid runId, Guid workspaceId, int limit, CancellationToken ct = default)
        => _agentLogRepository.ListAsync(
            new AgentLogFilter { RunId = runId, WorkspaceId = workspaceId },
            new AgentLogListOptions { Limit = limit, Sort = AgentLogSort.TimeAscending },
            ct);

    public Task<List<AgentLogRecord>> ListForChannelConnectionAsync(Guid channelConnectionId, DateTime? before, int limit, CancellationToken ct = default)
        => _agentLogRepository.ListAsync(
            new AgentLogFilter { ChannelConnectionId = channelConnectionId, Before = before },
            new AgentLogListOptions { Limit = limit, Sort = AgentLogSort.TimeDescending },
            ct);

    public Task<List<AgentLogRecord>> ListForResourceAsync(ResourceLogQueryRequest request, CancellationToken ct = default)
    {
        var tail = Math.Clamp(request.Tail, 1, 1000);
        return _agentLogRepository.ListAsync(
            new AgentLogFilter
            {
                WorkspaceId = request.WorkspaceId,
                ResourceKind = NormalizeResourceKind(request.ResourceKind),
                ResourceId = request.ResourceId,
                ResourceName = request.ResourceId.HasValue ? null : request.ResourceName,
                FromInclusive = request.SinceTime,
                Type = request.Type,
                Severity = request.Severity,
            },
            new AgentLogListOptions { Limit = tail, Sort = AgentLogSort.TimeDescending },
            ct);
    }

    public Task<string?> GetLastRelevantMessageForAgentAsync(Guid agentId, Guid? workspaceId = null, CancellationToken ct = default)
        => GetLastRelevantMessageAsync(new AgentLogFilter { AgentId = agentId, WorkspaceId = workspaceId }, ct);

    public async Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesForAgentsAsync(
        IReadOnlyCollection<Guid> agentIds,
        Guid? workspaceId = null,
        CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, string?>();
        foreach (var agentId in agentIds.Distinct())
            result[agentId] = await GetLastRelevantMessageForAgentAsync(agentId, workspaceId, ct);

        return result;
    }

    public Task<string?> GetLastRelevantMessageForChannelConnectionAsync(
        Guid channelConnectionId,
        Guid? workspaceId = null,
        CancellationToken ct = default)
        => GetLastRelevantMessageAsync(new AgentLogFilter { ChannelConnectionId = channelConnectionId, WorkspaceId = workspaceId }, ct);

    public async Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesForChannelConnectionsAsync(
        IReadOnlyCollection<Guid> channelConnectionIds,
        Guid? workspaceId = null,
        CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, string?>();
        foreach (var channelConnectionId in channelConnectionIds.Distinct())
            result[channelConnectionId] = await GetLastRelevantMessageForChannelConnectionAsync(channelConnectionId, workspaceId, ct);

        return result;
    }

    public async Task<GlobalLogsPage> ListGlobalAsync(GlobalLogFiltersRequest filters, CancellationToken ct = default)
    {
        var limit = Math.Clamp(filters.Limit, 1, 200);
        var skip = Math.Max(filters.Skip, 0);
        var query = GlobalLogs(filters, filters.WorkspaceId);
        var total = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(limit).ToListAsync(ct);
        return new GlobalLogsPage(items, total);
    }

    public async Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default)
    {
        var saved = await _agentLogRepository.AppendAsync(record, ct);
        return saved;
    }

    public async Task<AgentLogRecord> SendMessageAsync(Guid agentId, string content, Guid userId, CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId }, ct);
        if (agent is null) throw new InvalidOperationException($"Agent {agentId} not found");

        var correlationId = Guid.NewGuid().ToString("N");

        var record = await AppendAsync(AgentLogRecord.MessageIn(agentId, content, correlationId));

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
        var filter = new AgentLogFilter { AgentId = agentId, Type = AgentLogType.ToolCall };
        var total = await _agentLogRepository.CountAsync(filter, ct);
        var items = await _agentLogRepository.ListAsync(
            filter,
            new AgentLogListOptions { Skip = offset, Limit = limit, Sort = AgentLogSort.TimeDescending },
            ct);
        return (items, total);
    }

    public async Task<Dictionary<string, AgentLogRecord>> GetResultsByCorrelationAsync(
        Guid agentId, IReadOnlyCollection<string> correlationIds, CancellationToken ct = default)
    {
        if (correlationIds.Count == 0)
            return new Dictionary<string, AgentLogRecord>();

        var rows = await _agentLogRepository.ListAsync(
            new AgentLogFilter
            {
                AgentId = agentId,
                Type = AgentLogType.ToolResult,
                CorrelationIds = correlationIds.ToList(),
            },
            ct: ct);

        return rows
            .Where(r => r.CorrelationId is not null)
            .GroupBy(r => r.CorrelationId!)
            .ToDictionary(g => g.Key, g => g.First());
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

    private static IQueryable<AgentLogProjection> ToProjectionQuery(IQueryable<AgentLogRecord> query) =>
        query.Select(log => new AgentLogProjection(
            log.Id,
            log.AgentId,
            log.Agent == null ? null : log.Agent.Name,
            log.Time,
            log.Type,
            log.Tool,
            log.Integration,
            log.Channel,
            log.ChannelConnectionId,
            log.Content,
            log.Usage.DurationMs,
            log.Usage.InputTokens,
            log.Usage.OutputTokens,
            log.CorrelationId));

    private async Task<string?> GetLastRelevantMessageAsync(AgentLogFilter filter, CancellationToken ct)
    {
        var log = await RelevantActivityLogs(_agentLogRepository.Query(filter))
            .OrderByDescending(l => l.Time)
            .ThenByDescending(l => l.Id)
            .FirstOrDefaultAsync(ct);

        return log is null ? null : FormatRelevantMessage(log);
    }

    private static IQueryable<AgentLogRecord> ExcludePodStartupLogs(IQueryable<AgentLogRecord> query) =>
        query.Where(log =>
            log.Type != AgentLogType.AgentStartup &&
            !(log.Type == AgentLogType.System && log.Content == "Pod connected"));

    private static string NormalizeResourceKind(string kind)
    {
        var value = kind.Trim().ToLowerInvariant();
        return value switch
        {
            "agent" or "agents" => ResourceLogKinds.Agent,
            "run" or "runs" => ResourceLogKinds.Run,
            "channel" or "channels" => ResourceLogKinds.Channel,
            "integration" or "integrations" or "integrationdeployment" or "integrationdeployments" or "integration-deployment" or "integration-deployments" => ResourceLogKinds.IntegrationDeployment,
            "provider" or "providers" => ResourceLogKinds.Provider,
            _ => kind.Trim(),
        };
    }

    private static IQueryable<AgentLogRecord> RelevantActivityLogs(IQueryable<AgentLogRecord> query) =>
        ExcludePodStartupLogs(query)
            .Where(log =>
                log.Type == AgentLogType.MessageIn ||
                log.Type == AgentLogType.MessageOut ||
                log.Type == AgentLogType.ChannelIn ||
                log.Type == AgentLogType.ChannelOut ||
                log.Type == AgentLogType.ToolCall ||
                log.Type == AgentLogType.ToolResult ||
                log.Type == AgentLogType.Error ||
                log.Type == AgentLogType.ErrorPodConnection ||
                log.Type == AgentLogType.ErrorLlmCall ||
                log.Type == AgentLogType.ErrorToolExecution ||
                log.Type == AgentLogType.ErrorSkillExecution ||
                log.Type == AgentLogType.ErrorTurnOrchestration ||
                log.Type == AgentLogType.ErrorMemory ||
                log.Type == AgentLogType.ErrorConfiguration ||
                (log.Type == AgentLogType.System &&
                    !log.Content.StartsWith("Turn setup:") &&
                    !log.Content.StartsWith("Turn started:") &&
                    !log.Content.StartsWith("Turn complete:") &&
                    !log.Content.StartsWith("LLM call complete:") &&
                    !log.Content.StartsWith("Conversation compacted")));

    private static string FormatRelevantMessage(AgentLogRecord log) => log.Type switch
    {
        AgentLogType.ToolCall => $"Using {DisplayTool(log)}",
        AgentLogType.ToolResult => FormatToolResult(log),
        AgentLogType.Error or
            AgentLogType.ErrorPodConnection or
            AgentLogType.ErrorLlmCall or
            AgentLogType.ErrorToolExecution or
            AgentLogType.ErrorSkillExecution or
            AgentLogType.ErrorTurnOrchestration or
            AgentLogType.ErrorMemory or
            AgentLogType.ErrorConfiguration => $"Error: {Preview(log.Content)}",
        _ => Preview(log.Content),
    };

    private static string FormatToolResult(AgentLogRecord log)
    {
        var content = Preview(log.Content);
        return string.IsNullOrWhiteSpace(content)
            ? $"{DisplayTool(log)} finished"
            : $"{DisplayTool(log)} finished: {content}";
    }

    private static string DisplayTool(AgentLogRecord log)
    {
        if (!string.IsNullOrWhiteSpace(log.Integration) && !string.IsNullOrWhiteSpace(log.Tool))
            return $"{log.Integration}.{log.Tool}";

        if (!string.IsNullOrWhiteSpace(log.Tool))
            return log.Tool;

        return "tool";
    }

    private static string Preview(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var normalized = Regex.Replace(content.Trim(), "\\s+", " ");
        return normalized.Length <= ActivityPreviewMaxLength
            ? normalized
            : normalized[..ActivityPreviewMaxLength] + "...";
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

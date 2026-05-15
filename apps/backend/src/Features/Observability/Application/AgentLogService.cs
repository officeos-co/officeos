namespace OffceOs.Application.Features.Observability;

internal sealed class AgentLogService : IAgentLogService
{
    private const int ActivityPreviewMaxLength = 240;

    private readonly IAgentLogRepository _agentLogRepository;

    public AgentLogService(IAgentLogRepository agentLogRepository) => _agentLogRepository = agentLogRepository;

    public async Task<AgentLogPage> ListAsync(AgentLogQueryRequest request, CancellationToken ct = default)
    {
        var limit = Math.Clamp(request.Limit, 1, 1000);
        var skip = Math.Max(request.Skip, 0);
        var query = ExcludePodStartupLogs(_agentLogRepository.Query(ToFilter(request)));
        var total = await query.CountAsync(ct);
        var ordered = request.Sort == AgentLogSort.TimeAscending
            ? query.OrderBy(log => log.Time).ThenBy(log => log.Id)
            : query.OrderByDescending(log => log.Time).ThenByDescending(log => log.Id);
        var items = await ordered.Skip(skip).Take(limit).ToListAsync(ct);

        return new AgentLogPage(items, total);
    }

    public async Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(
        LastRelevantLogQueryRequest request,
        CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, string?>();
        foreach (var agentId in request.AgentIds?.Distinct() ?? [])
            result[agentId] = await GetLastRelevantMessageAsync(
                new AgentLogFilter { AgentId = agentId, WorkspaceId = request.WorkspaceId },
                ct);
        foreach (var channelConnectionId in request.ChannelConnectionIds?.Distinct() ?? [])
            result[channelConnectionId] = await GetLastRelevantMessageAsync(
                new AgentLogFilter { ChannelConnectionId = channelConnectionId, WorkspaceId = request.WorkspaceId },
                ct);
        return result;
    }

    public async Task<AgentLogRecord> AppendAsync(AgentLogRecord record, CancellationToken ct = default)
    {
        var saved = await _agentLogRepository.AppendAsync(record, ct);
        return saved;
    }

    public Task<AgentLogRecord> QueueWorkAsync(QueueAgentWorkRequest request, CancellationToken ct = default)
        => _agentLogRepository.UpsertQueuedWorkAsync(new AgentLogRecord
        {
            AgentId = request.AgentId,
            WorkspaceId = request.WorkspaceId,
            ResourceKind = ResourceLogKinds.Agent,
            ResourceId = request.AgentId,
            Type = AgentLogType.MessageIn,
            Content = request.Content,
            CorrelationId = request.CorrelationId,
            Time = request.Time ?? DateTime.UtcNow,
            WorkStatus = AgentWorkStatusKinds.Queued,
            WorkPurpose = AgentWorkPurposeKinds.Normalize(request.Purpose),
            DefinitionId = request.DefinitionId,
        }, ct);

    public Task<AgentLogRecord?> ClaimNextQueuedWorkAsync(CancellationToken ct = default)
        => _agentLogRepository.ClaimNextQueuedWorkAsync(ct);

    public Task CompleteWorkAsync(Guid workLogId, CancellationToken ct = default)
        => _agentLogRepository.MarkWorkAsync(workLogId, AgentWorkStatusKinds.Completed, null, ct);

    public Task FailWorkAsync(Guid workLogId, string error, CancellationToken ct = default)
        => _agentLogRepository.MarkWorkAsync(workLogId, AgentWorkStatusKinds.Failed, error, ct);

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
            "channel" or "channels" => ResourceLogKinds.Channel,
            "integration" or "integrations" or "integrationdeployment" or "integrationdeployments" or "integration-deployment" or "integration-deployments" => ResourceLogKinds.IntegrationDeployment,
            "provider" or "providers" => ResourceLogKinds.Provider,
            _ => kind.Trim(),
        };
    }

    private static AgentLogFilter ToFilter(AgentLogQueryRequest request) => new()
    {
        WorkspaceId = request.WorkspaceId,
        AgentId = request.AgentId,
        AgentIds = request.AgentIds,
        ChannelConnectionId = request.ChannelConnectionId,
        ResourceKind = string.IsNullOrWhiteSpace(request.ResourceKind) ? null : NormalizeResourceKind(request.ResourceKind),
        ResourceId = request.ResourceId,
        ResourceName = request.ResourceId.HasValue ? null : request.ResourceName,
        Type = request.Type,
        Types = request.Types,
        WorkStatus = request.WorkStatus,
        WorkPurpose = request.WorkPurpose,
        DefinitionId = request.DefinitionId,
        Severity = request.Severity,
        Search = request.Search,
        AgentName = request.AgentName,
        Before = request.Before,
        FromInclusive = request.FromInclusive,
        ToExclusive = request.ToExclusive,
    };

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

}

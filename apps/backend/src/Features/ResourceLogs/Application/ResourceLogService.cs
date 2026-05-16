using OffceOs.Application.Features.ControlPlane;
using OffceOs.Domain.Features.ResourceLogs;
namespace OffceOs.Application.Features.ResourceLogs;

internal sealed class ResourceLogService : IResourceLogService
{
    private const int ActivityPreviewMaxLength = 240;

    private readonly IResourceLogRepository _resourceLogRepository;
    private readonly IControlPlaneResourceCatalogService _controlPlaneResourceCatalogService;

    public ResourceLogService(
        IResourceLogRepository resourceLogRepository,
        IControlPlaneResourceCatalogService resourceCatalog)
    {
        _resourceLogRepository = resourceLogRepository;
        _controlPlaneResourceCatalogService = resourceCatalog;
    }

    public async Task<ResourceLogPage> ListAsync(ResourceLogQueryRequest request, CancellationToken ct = default)
    {
        var limit = Math.Clamp(request.Limit, 1, 1000);
        var skip = Math.Max(request.Skip, 0);
        var query = ExcludePodStartupLogs(_resourceLogRepository.Query(ToFilter(request)));
        var total = await query.CountAsync(ct);
        var ordered = request.Sort == ResourceLogSort.TimeAscending
            ? query.OrderBy(log => log.Time).ThenBy(log => log.Id)
            : query.OrderByDescending(log => log.Time).ThenByDescending(log => log.Id);
        var items = await ordered.Skip(skip).Take(limit).ToListAsync(ct);

        return new ResourceLogPage(items, total);
    }

    public async Task<IReadOnlyDictionary<Guid, string?>> GetLastRelevantMessagesAsync(
        LastRelevantLogQueryRequest request,
        CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, string?>();
        foreach (var agentId in request.AgentIds?.Distinct() ?? [])
            result[agentId] = await GetLastRelevantMessageAsync(
                new ResourceLogFilter { AgentId = agentId, WorkspaceId = request.WorkspaceId },
                ct);
        foreach (var channelConnectionId in request.ChannelConnectionIds?.Distinct() ?? [])
            result[channelConnectionId] = await GetLastRelevantMessageAsync(
                new ResourceLogFilter { ChannelConnectionId = channelConnectionId, WorkspaceId = request.WorkspaceId },
                ct);
        return result;
    }

    public async Task<ResourceLogRecord> AppendAsync(ResourceLogRecord record, CancellationToken ct = default)
    {
        var saved = await _resourceLogRepository.AppendAsync(record, ct);
        return saved;
    }

    public Task<ResourceLogRecord?> GetAsync(Guid logId, CancellationToken ct = default)
        => _resourceLogRepository.GetByAsync(new ResourceLogFilter { Id = logId }, ct);

    private async Task<string?> GetLastRelevantMessageAsync(ResourceLogFilter filter, CancellationToken ct)
    {
        var log = await RelevantActivityLogs(_resourceLogRepository.Query(filter))
            .OrderByDescending(l => l.Time)
            .ThenByDescending(l => l.Id)
            .FirstOrDefaultAsync(ct);

        return log is null ? null : FormatRelevantMessage(log);
    }

    private static IQueryable<ResourceLogRecord> ExcludePodStartupLogs(IQueryable<ResourceLogRecord> query) =>
        query.Where(log =>
            log.Type != ResourceLogType.AgentStartup &&
            !(log.Type == ResourceLogType.System && log.Content == "Pod connected"));

    private string NormalizeResourceKind(string kind)
    {
        var descriptor = _controlPlaneResourceCatalogService.Find(kind.Trim());
        if (descriptor is not null)
            return ResourceLogKindFor(descriptor.Singular);

        return ResourceLogKindFor(kind);
    }

    internal static string ResourceLogKindFor(string kind)
    {
        var value = kind.Trim().ToLowerInvariant();
        return value switch
        {
            "agent" or "agents" => ResourceLogKinds.Agent,
            "browser" or "browsers" => ResourceLogKinds.Browser,
            "channel" or "channels" => ResourceLogKinds.Channel,
            "control-plane" or "controlplane" or "system" => ResourceLogKinds.ControlPlane,
            "credential" or "credentials" => ResourceLogKinds.Credential,
            "integration" or "integrations" or "integrationdeployment" or "integrationdeployments" or "integration-deployment" or "integration-deployments" => ResourceLogKinds.IntegrationDeployment,
            "memory-store" or "memory-stores" or "memorystore" or "memorystores" => ResourceLogKinds.MemoryStore,
            "model" or "models" => ResourceLogKinds.Model,
            "provider" or "providers" => ResourceLogKinds.Provider,
            "routine" or "routines" => ResourceLogKinds.Routine,
            _ => kind.Trim(),
        };
    }

    private ResourceLogFilter ToFilter(ResourceLogQueryRequest request) => new()
    {
        WorkspaceId = request.WorkspaceId,
        AgentId = request.AgentId,
        AgentIds = request.AgentIds,
        ChannelConnectionId = request.ChannelConnectionId,
        ResourceKind = string.IsNullOrWhiteSpace(request.ResourceKind) ? null : NormalizeResourceKind(request.ResourceKind),
        ResourceId = request.ResourceId,
        ResourceName = request.ResourceId.HasValue ? null : request.ResourceName,
        CorrelationId = request.CorrelationId,
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

    private static IQueryable<ResourceLogRecord> RelevantActivityLogs(IQueryable<ResourceLogRecord> query) =>
        ExcludePodStartupLogs(query)
            .Where(log =>
                log.Type == ResourceLogType.MessageIn ||
                log.Type == ResourceLogType.MessageOut ||
                log.Type == ResourceLogType.ChannelIn ||
                log.Type == ResourceLogType.ChannelOut ||
                log.Type == ResourceLogType.ToolCall ||
                log.Type == ResourceLogType.ToolResult ||
                log.Type == ResourceLogType.Error ||
                log.Type == ResourceLogType.ErrorPodConnection ||
                log.Type == ResourceLogType.ErrorLlmCall ||
                log.Type == ResourceLogType.ErrorToolExecution ||
                log.Type == ResourceLogType.ErrorSkillExecution ||
                log.Type == ResourceLogType.ErrorTurnOrchestration ||
                log.Type == ResourceLogType.ErrorMemory ||
                log.Type == ResourceLogType.ErrorConfiguration ||
                (log.Type == ResourceLogType.System &&
                    !log.Content.StartsWith("Turn setup:") &&
                    !log.Content.StartsWith("Turn started:") &&
                    !log.Content.StartsWith("Turn complete:") &&
                    !log.Content.StartsWith("LLM call complete:") &&
                    !log.Content.StartsWith("Conversation compacted")));

    private static string FormatRelevantMessage(ResourceLogRecord log) => log.Type switch
    {
        ResourceLogType.ToolCall => $"Using {DisplayTool(log)}",
        ResourceLogType.ToolResult => FormatToolResult(log),
        ResourceLogType.Error or
            ResourceLogType.ErrorPodConnection or
            ResourceLogType.ErrorLlmCall or
            ResourceLogType.ErrorToolExecution or
            ResourceLogType.ErrorSkillExecution or
            ResourceLogType.ErrorTurnOrchestration or
            ResourceLogType.ErrorMemory or
            ResourceLogType.ErrorConfiguration => $"Error: {Preview(log.Content)}",
        _ => Preview(log.Content),
    };

    private static string FormatToolResult(ResourceLogRecord log)
    {
        var content = Preview(log.Content);
        return string.IsNullOrWhiteSpace(content)
            ? $"{DisplayTool(log)} finished"
            : $"{DisplayTool(log)} finished: {content}";
    }

    private static string DisplayTool(ResourceLogRecord log)
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

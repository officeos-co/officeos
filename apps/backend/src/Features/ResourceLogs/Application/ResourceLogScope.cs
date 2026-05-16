namespace OffceOs.Application.Features.ResourceLogs;

public sealed class ResourceLogScope
{
    private readonly IResourceLogService _resourceLogService;
    private readonly string _resourceKind;
    private readonly Guid? _resourceId;
    private readonly string? _resourceName;
    private readonly Guid? _workspaceId;
    private readonly Guid? _agentId;
    private readonly Guid? _channelConnectionId;
    private readonly string? _parentResourceKind;
    private readonly Guid? _parentResourceId;
    private readonly string? _correlationId;

    public ResourceLogScope(
        IResourceLogService resourceLogService,
        string resourceKind,
        Guid? resourceId = null,
        string? resourceName = null,
        Guid? workspaceId = null,
        Guid? agentId = null,
        Guid? channelConnectionId = null,
        string? parentResourceKind = null,
        Guid? parentResourceId = null,
        string? correlationId = null)
    {
        _resourceLogService = resourceLogService;
        _resourceKind = resourceKind;
        _resourceId = resourceId;
        _resourceName = resourceName;
        _workspaceId = workspaceId;
        _agentId = agentId;
        _channelConnectionId = channelConnectionId;
        _parentResourceKind = parentResourceKind;
        _parentResourceId = parentResourceId;
        _correlationId = correlationId;
    }

    public ResourceLogScope WithAgent(Guid? agentId) => Copy(agentId: agentId);

    public ResourceLogScope WithCorrelation(string? correlationId) => Copy(correlationId: correlationId);

    public ResourceLogScope WithParent(string resourceKind, Guid resourceId) =>
        Copy(parentResourceKind: resourceKind, parentResourceId: resourceId);

    public Task InfoAsync(string messageTemplate, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.System, ResourceLogSeverityKinds.Info, null, null, messageTemplate, [], ct);

    public Task InfoAsync(string messageTemplate, object? value, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.System, ResourceLogSeverityKinds.Info, null, null, messageTemplate, [value], ct);

    public Task InfoAsync(string messageTemplate, object? value1, object? value2, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.System, ResourceLogSeverityKinds.Info, null, null, messageTemplate, [value1, value2], ct);

    public Task InfoAsync(string messageTemplate, IReadOnlyList<object?> values, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.System, ResourceLogSeverityKinds.Info, null, null, messageTemplate, values, ct);

    public Task WarningAsync(string messageTemplate, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.System, ResourceLogSeverityKinds.Warning, null, null, messageTemplate, [], ct);

    public Task WarningAsync(string messageTemplate, object? value, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.System, ResourceLogSeverityKinds.Warning, null, null, messageTemplate, [value], ct);

    public Task WarningAsync(string messageTemplate, object? value1, object? value2, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.System, ResourceLogSeverityKinds.Warning, null, null, messageTemplate, [value1, value2], ct);

    public Task WarningAsync(string messageTemplate, IReadOnlyList<object?> values, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.System, ResourceLogSeverityKinds.Warning, null, null, messageTemplate, values, ct);

    public Task ErrorAsync(Exception exception, string messageTemplate, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.Error, ResourceLogSeverityKinds.Error, exception, null, messageTemplate, [], ct);

    public Task ErrorAsync(Exception exception, string messageTemplate, object? value, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.Error, ResourceLogSeverityKinds.Error, exception, null, messageTemplate, [value], ct);

    public Task ErrorAsync(Exception exception, string messageTemplate, object? value1, object? value2, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.Error, ResourceLogSeverityKinds.Error, exception, null, messageTemplate, [value1, value2], ct);

    public Task ErrorAsync(Exception exception, string messageTemplate, IReadOnlyList<object?> values, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.Error, ResourceLogSeverityKinds.Error, exception, null, messageTemplate, values, ct);

    public Task ErrorAsync(string content, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.Error, ResourceLogSeverityKinds.Error, null, content, "{Content}", [content], ct);

    public Task ChannelInAsync(string channelType, string content, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.ChannelIn, ResourceLogSeverityKinds.Info, null, content, "{Content}", [content], ct, channelType);

    public Task ChannelOutAsync(string channelType, string content, CancellationToken ct = default) =>
        WriteAsync(ResourceLogType.ChannelOut, ResourceLogSeverityKinds.Info, null, content, "{Content}", [content], ct, channelType);

    private async Task WriteAsync(
        ResourceLogType type,
        string severity,
        Exception? exception,
        string? fixedContent,
        string messageTemplate,
        IReadOnlyList<object?> values,
        CancellationToken ct,
        string? channel = null)
    {
        IReadOnlyList<ResourceLogTemplateItem> templateValues = [];
        var rendered = fixedContent ?? ResourceLogTemplateBuilder.Render(messageTemplate, values, out templateValues);
        var metadataJson = ResourceLogTemplateBuilder.MetadataJson(messageTemplate, templateValues, exception);

        await _resourceLogService.AppendAsync(new ResourceLogRecord
        {
            ResourceKind = _resourceKind,
            ResourceId = _resourceId,
            ResourceName = _resourceName,
            ParentResourceKind = _parentResourceKind,
            ParentResourceId = _parentResourceId,
            AgentId = _agentId,
            WorkspaceId = _workspaceId,
            Type = type,
            Severity = severity,
            Channel = channel,
            ChannelConnectionId = _channelConnectionId,
            Content = rendered,
            CorrelationId = _correlationId,
            MetadataJson = metadataJson,
        }, ct);
    }

    private ResourceLogScope Copy(
        Guid? agentId = null,
        string? parentResourceKind = null,
        Guid? parentResourceId = null,
        string? correlationId = null) =>
        new(
            _resourceLogService,
            _resourceKind,
            _resourceId,
            _resourceName,
            _workspaceId,
            agentId ?? _agentId,
            _channelConnectionId,
            parentResourceKind ?? _parentResourceKind,
            parentResourceId ?? _parentResourceId,
            correlationId ?? _correlationId);
}

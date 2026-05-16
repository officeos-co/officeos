namespace OffceOs.Api.Features.ResourceLogs;

[ApiController]
[Route("api/v1/resources")]
public sealed class ResourceLogController : ControllerBase
{
    [HttpGet("{kind}/{name}/logs")]
    public async Task<IActionResult> GetResourceLogs(
        string kind,
        string name,
        [FromQuery] int? tail,
        [FromQuery] string? since,
        [FromQuery] DateTime? sinceTime,
        [FromQuery] string? type,
        [FromQuery] string? severity,
        [FromQuery] string? correlationId,
        [FromQuery] string? workStatus,
        [FromQuery] string? purpose,
        [FromQuery] bool? follow,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IControlPlaneResourceCatalogService catalog,
        [FromServices] IResourceLogService logs,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var resourceKind = ResolveResourceLogKind(kind, catalog);
        if (resourceKind is null)
            return NotFound(new { error = $"Resource kind '{kind}' was not found." });

        var parsedType = ParseLogType(type);
        if (type is not null && parsedType is null)
            return BadRequest(new { error = $"Log type '{type}' is not supported." });

        if (follow == true)
            return await StreamResourceLogs(resourceKind, name, tail, since, sinceTime, parsedType, severity, correlationId, workStatus, purpose, scope.Value.WorkspaceId, logs, ct);

        var resourceId = Guid.TryParse(name, out var parsedResourceId) ? parsedResourceId : (Guid?)null;
        var page = await logs.ListAsync(new ResourceLogQueryRequest(
            WorkspaceId: scope.Value.WorkspaceId,
            ResourceKind: resourceKind,
            ResourceName: name,
            ResourceId: resourceId,
            Type: parsedType,
            WorkStatus: workStatus,
            WorkPurpose: purpose,
            Severity: severity,
            CorrelationId: correlationId,
            FromInclusive: sinceTime ?? ParseSince(since),
            Limit: tail ?? 100), ct);

        var items = page.Items
            .OrderBy(item => item.Time)
            .ThenBy(item => item.Id)
            .Select(ToResourceLogPayload);
        return Ok(new
        {
            kind = resourceKind,
            name,
            total = page.Total,
            items,
        });
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static string? ResolveResourceLogKind(string kind, IControlPlaneResourceCatalogService catalog)
    {
        var descriptor = catalog.Find(kind);
        return descriptor is null ? null : ResourceLogService.ResourceLogKindFor(descriptor.Singular);
    }

    private static ResourceLogType? ParseLogType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return null;

        var normalized = type.Replace("-", string.Empty).Replace("_", string.Empty);
        return Enum.TryParse<ResourceLogType>(normalized, true, out var parsed) ? parsed : null;
    }

    private static DateTime? ParseSince(string? since)
    {
        if (string.IsNullOrWhiteSpace(since))
            return null;

        var value = since.Trim();
        if (DateTime.TryParse(value, out var parsed))
            return parsed.ToUniversalTime();

        if (value.Length < 2 || !double.TryParse(value[..^1], out var amount))
            return null;

        var duration = value[^1] switch
        {
            's' => TimeSpan.FromSeconds(amount),
            'm' => TimeSpan.FromMinutes(amount),
            'h' => TimeSpan.FromHours(amount),
            'd' => TimeSpan.FromDays(amount),
            _ => TimeSpan.Zero,
        };

        return duration == TimeSpan.Zero ? null : DateTime.UtcNow.Subtract(duration);
    }

    private async Task<IActionResult> StreamResourceLogs(
        string kind,
        string name,
        int? tail,
        string? since,
        DateTime? sinceTime,
        ResourceLogType? type,
        string? severity,
        string? correlationId,
        string? workStatus,
        string? purpose,
        Guid workspaceId,
        IResourceLogService logs,
        CancellationToken ct)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var sent = new HashSet<Guid>();
        var from = sinceTime ?? ParseSince(since);
        var resourceId = Guid.TryParse(name, out var parsedResourceId) ? parsedResourceId : (Guid?)null;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var page = await logs.ListAsync(new ResourceLogQueryRequest(
                    WorkspaceId: workspaceId,
                    ResourceKind: kind,
                    ResourceName: name,
                    ResourceId: resourceId,
                    Type: type,
                    WorkStatus: workStatus,
                    WorkPurpose: purpose,
                    Severity: severity,
                    CorrelationId: correlationId,
                    FromInclusive: from,
                    Limit: tail ?? 100,
                    Sort: ResourceLogSort.TimeAscending), ct);

                foreach (var log in page.Items.OrderBy(item => item.Time).ThenBy(item => item.Id))
                {
                    if (!sent.Add(log.Id))
                        continue;

                    await Response.WriteAsync($"id: {log.Id:N}\n", ct);
                    await Response.WriteAsync("event: log\n", ct);
                    await Response.WriteAsync($"data: {JsonSerializer.Serialize(ToResourceLogPayload(log))}\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                }

                if (page.Items.Count > 0)
                    from = page.Items.Max(item => item.Time);

                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }

        return new EmptyResult();
    }

    private static object ToResourceLogPayload(ResourceLogRecord log) => new
    {
        log.Id,
        log.AgentId,
        log.WorkspaceId,
        log.ResourceKind,
        log.ResourceId,
        log.ResourceName,
        log.ParentResourceKind,
        log.ParentResourceId,
        log.Type,
        log.Severity,
        log.Tool,
        log.Integration,
        log.Channel,
        log.ChannelConnectionId,
        log.Content,
        metadata = ParseMetadata(log.MetadataJson),
        log.CorrelationId,
        log.WorkStatus,
        log.WorkPurpose,
        log.DefinitionId,
        usage = new
        {
            log.Usage.InputTokens,
            log.Usage.OutputTokens,
            log.Usage.DurationMs,
        },
        log.Time,
        log.StartedAt,
        log.CompletedAt,
        log.WorkError,
    };

    private static object? ParseMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<JsonElement>(metadataJson);
        }
        catch (JsonException)
        {
            return new { raw = metadataJson };
        }
    }
}

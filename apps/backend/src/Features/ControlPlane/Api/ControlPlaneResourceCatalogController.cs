using OffceOs.Features.ControlPlane.Application;
using OffceOs.Features.ControlPlane.Domain;
using OffceOs.Features.Management.Domain;
using OffceOs.Features.ResourceLogs.Application;
using OffceOs.Features.ResourceLogs.Domain;

namespace OffceOs.Features.ControlPlane.Api;

[ApiController]
[Route("api/v1/resources")]
public sealed class ControlPlaneResourceCatalogController : ControllerBase
{
    [HttpGet]
    public IActionResult ListResources(
        [FromServices] IControlPlaneResourceService controlPlaneResourceService)
    {
        if (HttpContext.Items["User"] is not UserRecord)
            return Unauthorized(new { error = "Unauthenticated." });

        return Ok(controlPlaneResourceService.ListDefinitions());
    }

    [HttpGet("{kind}")]
    public async Task<IActionResult> ListResourceKind(
        string kind,
        [FromServices] IWorkspaceService workspaceService,
        [FromServices] IControlPlaneResourceService controlPlaneResourceService,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaceService, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var resources = await controlPlaneResourceService.ListAsync(kind, scope, ct);
        return resources is null
            ? NotFound(new { error = $"{kind} is not a registered resource kind." })
            : Ok(resources.Select(resource => resource.Fields));
    }

    [HttpGet("~/api/v1/providers")]
    public Task<IActionResult> ListProviderResources(
        [FromServices] IWorkspaceService workspaceService,
        [FromServices] IControlPlaneResourceService controlPlaneResourceService,
        CancellationToken ct) =>
        ListResourceKind("providers", workspaceService, controlPlaneResourceService, ct);

    [HttpGet("~/api/v1/models")]
    public Task<IActionResult> ListModelResources(
        [FromServices] IWorkspaceService workspaceService,
        [FromServices] IControlPlaneResourceService controlPlaneResourceService,
        CancellationToken ct) =>
        ListResourceKind("models", workspaceService, controlPlaneResourceService, ct);

    [HttpGet("{kind}/{name}")]
    public async Task<IActionResult> DescribeResource(
        string kind,
        string name,
        [FromServices] IWorkspaceService workspaceService,
        [FromServices] IControlPlaneResourceService controlPlaneResourceService,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaceService, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var resource = await controlPlaneResourceService.DescribeAsync(kind, name, scope, ct);
        return resource is null
            ? NotFound(new { error = $"{kind}/{name} was not found." })
            : Ok(resource.Fields);
    }

    [HttpDelete("{kind}/{name}")]
    public async Task<IActionResult> DeleteResource(
        string kind,
        string name,
        [FromServices] IWorkspaceService workspaceService,
        [FromServices] IControlPlaneResourceService controlPlaneResourceService,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaceService, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var result = await controlPlaneResourceService.DeleteAsync(kind, name, scope, ct);
        if (result.Deleted)
            return Ok(new { deleted = true });
        if (result.Unsupported)
            return StatusCode(405, new { error = result.Error });

        return NotFound(new { error = result.Error });
    }

    [HttpPost("{kind}/{name}/messages")]
    public async Task<IActionResult> SendResourceMessage(
        string kind,
        string name,
        [FromBody] ControlPlaneMessageInput input,
        [FromServices] IWorkspaceService workspaceService,
        [FromServices] IControlPlaneResourceService controlPlaneResourceService,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaceService, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var result = await controlPlaneResourceService.SendMessageAsync(
            kind,
            name,
            new ControlPlaneMessageRequest(input.Message, input.Purpose),
            scope,
            ct);
        if (result.Succeeded)
            return Ok(result.Payload);
        if (result.NotFound)
            return NotFound(new { error = result.Error });
        if (result.StatusCode.HasValue)
            return StatusCode(result.StatusCode.Value, new { error = result.Error });

        return StatusCode(500, new { error = result.Error ?? "Resource message failed." });
    }

    [HttpPost("{kind}/{name}/auth")]
    public async Task<IActionResult> AuthenticateResource(
        string kind,
        string name,
        [FromBody] ControlPlaneAuthenticationInput input,
        [FromServices] IWorkspaceService workspaceService,
        [FromServices] IControlPlaneResourceService controlPlaneResourceService,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaceService, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var result = await controlPlaneResourceService.AuthenticateAsync(
            kind,
            name,
            new ControlPlaneAuthenticationRequest(
                input.AccessToken,
                input.RefreshToken,
                input.ExpiresAt,
                input.AccountEmail,
                input.AccountId,
                input.ClientId,
                input.TokenUrl,
                input.Scopes),
            scope,
            ct);
        if (result.Succeeded)
            return Ok(result.Payload);
        if (result.NotFound)
            return NotFound(new { error = result.Error });
        if (result.StatusCode.HasValue)
            return StatusCode(result.StatusCode.Value, new { error = result.Error });

        return StatusCode(500, new { error = result.Error ?? "Resource authentication failed." });
    }

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
        [FromServices] IWorkspaceService workspaceService,
        [FromServices] IControlPlaneResourceService controlPlaneResourceService,
        [FromServices] IResourceLogService resourceLogService,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaceService, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var resourceKind = ResolveResourceLogKind(kind, controlPlaneResourceService);
        if (resourceKind is null)
            return NotFound(new { error = $"Resource kind '{kind}' was not found." });

        var parsedType = ParseLogType(type);
        if (type is not null && parsedType is null)
            return BadRequest(new { error = $"Log type '{type}' is not supported." });

        if (follow == true)
        {
            return await StreamResourceLogs(
                resourceKind,
                name,
                tail,
                since,
                sinceTime,
                parsedType,
                severity,
                correlationId,
                workStatus,
                purpose,
                scope.WorkspaceId,
                resourceLogService,
                ct);
        }

        var resourceId = Guid.TryParse(name, out var parsedResourceId) ? parsedResourceId : (Guid?)null;
        var isSessionResource = resourceKind == ResourceLogKinds.Session;
        var page = await resourceLogService.ListAsync(new ResourceLogQueryRequest(
            WorkspaceId: scope.WorkspaceId,
            SessionId: isSessionResource ? resourceId : null,
            ResourceKind: isSessionResource ? null : resourceKind,
            ResourceName: isSessionResource ? null : name,
            ResourceId: isSessionResource ? null : resourceId,
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

    private async Task<ControlPlaneResourceScope?> RequireScopeAsync(IWorkspaceService workspaceService, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaceService.GetCurrentAsync(user.Id, ct);
        return new ControlPlaneResourceScope(user.Id, workspace.Id);
    }

    private static string? ResolveResourceLogKind(string kind, IControlPlaneResourceService controlPlaneResourceService)
    {
        var descriptor = controlPlaneResourceService.FindDefinition(kind);
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
        IResourceLogService resourceLogService,
        CancellationToken ct)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var sent = new HashSet<Guid>();
        var from = sinceTime ?? ParseSince(since);
        var resourceId = Guid.TryParse(name, out var parsedResourceId) ? parsedResourceId : (Guid?)null;
        var isSessionResource = kind == ResourceLogKinds.Session;

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var page = await resourceLogService.ListAsync(new ResourceLogQueryRequest(
                    WorkspaceId: workspaceId,
                    SessionId: isSessionResource ? resourceId : null,
                    ResourceKind: isSessionResource ? null : kind,
                    ResourceName: isSessionResource ? null : name,
                    ResourceId: isSessionResource ? null : resourceId,
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

public sealed record ControlPlaneMessageInput(
    string Message,
    string? Purpose = null);

public sealed record ControlPlaneAuthenticationInput(
    string AccessToken,
    string RefreshToken,
    DateTime? ExpiresAt,
    string? AccountEmail,
    string? AccountId,
    string? ClientId,
    string? TokenUrl,
    IReadOnlyList<string>? Scopes);

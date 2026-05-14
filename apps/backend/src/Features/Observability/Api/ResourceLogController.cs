namespace OffceOs.Api.Features.Observability;

[ApiController]
[Route("api/control-plane/v1/resources")]
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
        [FromQuery] bool? follow,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentLogService logs,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        _ = follow;
        var parsedType = ParseLogType(type);
        if (type is not null && parsedType is null)
            return BadRequest(new { error = $"Log type '{type}' is not supported." });

        var resourceId = Guid.TryParse(name, out var parsedResourceId) ? parsedResourceId : (Guid?)null;
        var items = await logs.ListForResourceAsync(new ResourceLogQueryRequest(
            NormalizeResourceLogKind(kind),
            name,
            scope.Value.WorkspaceId,
            resourceId,
            tail ?? 100,
            sinceTime ?? ParseSince(since),
            parsedType,
            severity), ct);

        var lines = items
            .OrderBy(item => item.Time)
            .ThenBy(item => item.Id)
            .Select(FormatResourceLogLine);
        return Content(string.Join('\n', lines), "text/plain; charset=utf-8");
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static string NormalizeResourceLogKind(string kind)
    {
        var value = kind.Trim().ToLowerInvariant();
        return value switch
        {
            "agent" or "agents" => ResourceLogKinds.Agent,
            "run" or "runs" => ResourceLogKinds.Run,
            "channel" or "channels" => ResourceLogKinds.Channel,
            "provider" or "providers" => ResourceLogKinds.Provider,
            "integration" or "integrations" or "integrationdeployment" or "integrationdeployments" or "integration-deployment" or "integration-deployments" => ResourceLogKinds.IntegrationDeployment,
            _ => kind.Trim(),
        };
    }

    private static AgentLogType? ParseLogType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return null;

        var normalized = type.Replace("-", string.Empty).Replace("_", string.Empty);
        return Enum.TryParse<AgentLogType>(normalized, true, out var parsed) ? parsed : null;
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

    private static string FormatResourceLogLine(AgentLogRecord log)
    {
        var resource = log.ResourceName ?? log.ResourceId?.ToString("N") ?? "-";
        return $"{log.Time:O} {log.Severity} {log.ResourceKind}/{resource} {log.Type}: {log.Content}";
    }
}

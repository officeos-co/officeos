namespace OffceOs.Api.Features.Agents;

[ApiController]
[Route("api/declarative")]
public sealed class DeclarativeAgentsController : ControllerBase
{
    [HttpPost("validate")]
    public async Task<IActionResult> Validate(
        [FromBody] DeclarativeManifestInput input,
        [FromServices] IDeclarativeAgentService declarative,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var user = RequireUser();
        if (user is null) return Unauthorized(new { error = "Unauthenticated." });
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return Ok(await declarative.ValidateAsync(input.Manifest, user.Id, workspace.Id, ct));
    }

    [HttpPost("diff")]
    public async Task<IActionResult> Diff(
        [FromBody] DeclarativeManifestInput input,
        [FromServices] IDeclarativeAgentService declarative,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var user = RequireUser();
        if (user is null) return Unauthorized(new { error = "Unauthenticated." });
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return Ok(await declarative.DiffAsync(input.Manifest, user.Id, workspace.Id, ct));
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply(
        [FromBody] DeclarativeManifestInput input,
        [FromServices] IDeclarativeAgentService declarative,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var user = RequireUser();
        if (user is null) return Unauthorized(new { error = "Unauthenticated." });
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return Ok(await declarative.ApplyAsync(input.Manifest, user.Id, workspace.Id, ct));
    }

    [HttpGet("agents/{name}/export")]
    public async Task<IActionResult> ExportAgent(
        string name,
        [FromServices] IDeclarativeAgentService declarative,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var user = RequireUser();
        if (user is null) return Unauthorized(new { error = "Unauthenticated." });
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var yaml = await declarative.ExportAgentAsync(name, user.Id, workspace.Id, ct);
        return yaml is null
            ? NotFound(new { error = "Agent not found." })
            : Content(yaml, "application/yaml", Encoding.UTF8);
    }

    private UserRecord? RequireUser() => HttpContext.Items["User"] as UserRecord;
}

public sealed record DeclarativeManifestInput(string Manifest);

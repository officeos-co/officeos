using OffceOs.Features.AgentDefinitions.Application;
using OffceOs.Features.Management.Domain;
namespace OffceOs.Features.AgentDefinitions.Api;

[ApiController]
[Route("api/v1/manifests")]
public sealed class AgentDefinitionManifestController : ControllerBase
{
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateManifest(
        [FromBody] DeclarativeManifestInput input,
        [FromServices] IDeclarativeAgentService manifests,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok(await manifests.ValidateAsync(input.Manifest, scope.Value.UserId, scope.Value.WorkspaceId, ct));
    }

    [HttpPost("diff")]
    public async Task<IActionResult> DiffManifest(
        [FromBody] DeclarativeManifestInput input,
        [FromServices] IDeclarativeAgentService manifests,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        try
        {
            return Ok(await manifests.DiffAsync(input.Manifest, scope.Value.UserId, scope.Value.WorkspaceId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("apply")]
    public async Task<IActionResult> ApplyManifest(
        [FromBody] DeclarativeManifestInput input,
        [FromServices] IDeclarativeAgentService manifests,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        try
        {
            return Ok(await manifests.ApplyAsync(input.Manifest, scope.Value.UserId, scope.Value.WorkspaceId, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }
}

public sealed record DeclarativeManifestInput(string Manifest);

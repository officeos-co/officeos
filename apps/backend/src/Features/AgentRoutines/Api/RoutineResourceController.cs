namespace OffceOs.Api.Features.AgentRoutines;

[ApiController]
[Route("api/v1/resources")]
public sealed class RoutineResourceController : ControllerBase
{
    [HttpGet("routines")]
    [HttpGet("routine")]
    public async Task<IActionResult> ListRoutines(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok((await routines.ListForOwnerAsync(scope.Value.UserId, scope.Value.WorkspaceId, ct)).Select(ToRoutineResource));
    }

    [HttpGet("routines/{name}")]
    [HttpGet("routine/{name}")]
    public async Task<IActionResult> DescribeRoutine(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var items = await routines.ListForOwnerAsync(scope.Value.UserId, scope.Value.WorkspaceId, ct);
        var routine = items.FirstOrDefault(item => item.Routine.Id.ToString().Equals(name, StringComparison.OrdinalIgnoreCase));
        return routine is null ? NotFound(new { error = $"routines/{name} was not found." }) : Ok(ToRoutineResource(routine));
    }

    [HttpDelete("routines/{name}")]
    [HttpDelete("routine/{name}")]
    public async Task<IActionResult> DeleteRoutine(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        return Guid.TryParse(name, out var routineId) &&
            await routines.DeleteAsync(routineId, scope.Value.UserId, scope.Value.WorkspaceId, ct)
            ? Ok(new { deleted = true })
            : NotFound(new { error = $"routines/{name} was not found." });
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static object ToRoutineResource(AgentRoutineWithAgentRecord routine) => new
    {
        kind = "Routine",
        name = routine.Routine.Id.ToString(),
        id = routine.Routine.Id,
        routine.Routine.AgentId,
        agentName = routine.AgentName,
        routine.Routine.Enabled,
        routine.Routine.CreatedAt,
    };
}

namespace OffceOs.Api.Features.Integrations;

[ApiController]
[Route("api/control-plane/v1/resources")]
public sealed class IntegrationResourceController : ControllerBase
{
    [HttpGet("integrations")]
    [HttpGet("integration")]
    public async Task<IActionResult> ListIntegrations(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IIntegrationDeploymentRepository integrations,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok((await integrations.ListAsync(new IntegrationDeploymentFilter { WorkspaceId = scope.Value.WorkspaceId }, ct)).Select(ToIntegrationResource));
    }

    [HttpGet("integrations/{name}")]
    [HttpGet("integration/{name}")]
    public async Task<IActionResult> DescribeIntegration(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IIntegrationDeploymentRepository integrations,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var integration = await FindIntegrationAsync(integrations, name, scope.Value.WorkspaceId, ct);
        return integration is null ? NotFound(new { error = $"integrations/{name} was not found." }) : Ok(ToIntegrationResource(integration));
    }

    [HttpDelete("integrations/{name}")]
    [HttpDelete("integration/{name}")]
    public async Task<IActionResult> DeleteIntegration(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IIntegrationDeploymentRepository integrations,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var deleted = await integrations.DeleteAsync(new IntegrationDeploymentFilter
        {
            WorkspaceId = scope.Value.WorkspaceId,
            IntegrationName = name,
        }, ct);
        return deleted ? Ok(new { deleted = true }) : NotFound(new { error = $"integrations/{name} was not found." });
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static async Task<IntegrationDeploymentRecord?> FindIntegrationAsync(
        IIntegrationDeploymentRepository integrations,
        string name,
        Guid workspaceId,
        CancellationToken ct)
    {
        if (Guid.TryParse(name, out var id))
            return await integrations.GetByAsync(new IntegrationDeploymentFilter { Id = id, WorkspaceId = workspaceId }, ct);

        return await integrations.GetByAsync(new IntegrationDeploymentFilter
        {
            WorkspaceId = workspaceId,
            IntegrationName = name,
        }, ct);
    }

    private static object ToIntegrationResource(IntegrationDeploymentRecord deployment) => new
    {
        kind = "IntegrationDeployment",
        name = deployment.IntegrationName,
        id = deployment.Id,
        deployment.WorkspaceId,
        deployment.Enabled,
        deployment.CreatedAt,
        deployment.UpdatedAt,
    };
}

namespace OffceOs.Api.Features.AgentRoutines;

[ApiController]
[Route("api/v1/resources")]
public sealed class CredentialResourceController : ControllerBase
{
    [HttpGet("credentials")]
    [HttpGet("credential")]
    public async Task<IActionResult> ListCredentials(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRoutineCredentialRepository credentials,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok((await credentials.ListAsync(scope.Value.WorkspaceId, ct)).Select(ToCredentialResource));
    }

    [HttpGet("credentials/{name}")]
    [HttpGet("credential/{name}")]
    public async Task<IActionResult> DescribeCredential(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRoutineCredentialRepository credentials,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var credential = await credentials.GetByNameAsync(scope.Value.WorkspaceId, name, ct);
        return credential is null ? NotFound(new { error = $"credentials/{name} was not found." }) : Ok(ToCredentialResource(credential));
    }

    [HttpDelete("credentials/{name}")]
    [HttpDelete("credential/{name}")]
    public async Task<IActionResult> DeleteCredential(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRoutineCredentialRepository credentials,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return await credentials.DeleteAsync(scope.Value.WorkspaceId, name, ct)
            ? Ok(new { deleted = true })
            : NotFound(new { error = $"credentials/{name} was not found." });
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static object ToCredentialResource(AgentRoutineCredentialRecord credential) => new
    {
        kind = "Credential",
        name = credential.Name,
        id = credential.Id,
        credential.Provider,
        credential.AuthKind,
        configured = !string.IsNullOrWhiteSpace(credential.EncryptedSecret),
        credential.ExpiresAtUtc,
        credential.LastUsedAt,
        credential.CreatedAt,
        credential.UpdatedAt,
    };
}

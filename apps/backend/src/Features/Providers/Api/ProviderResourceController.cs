namespace OffceOs.Api.Features.Providers;

[ApiController]
[Route("api/v1")]
public sealed class ProviderResourceController : ControllerBase
{
    [HttpGet("resources/providers")]
    [HttpGet("resources/provider")]
    public async Task<IActionResult> ListProviderResources(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IProviderResourceRepository providers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok((await providers.ListAsync(scope.Value.WorkspaceId, ct)).Select(ToProviderResource));
    }

    [HttpGet("resources/providers/{name}")]
    [HttpGet("resources/provider/{name}")]
    public async Task<IActionResult> DescribeProviderResource(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IProviderResourceRepository providers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var provider = await providers.GetByNameAsync(scope.Value.WorkspaceId, name, ct);
        return provider is null ? NotFound(new { error = $"providers/{name} was not found." }) : Ok(ToProviderResource(provider));
    }

    [HttpDelete("resources/providers/{name}")]
    [HttpDelete("resources/provider/{name}")]
    public async Task<IActionResult> DeleteProviderResource(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IProviderResourceRepository providers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return await providers.DeleteAsync(scope.Value.WorkspaceId, name, ct)
            ? Ok(new { deleted = true })
            : NotFound(new { error = $"providers/{name} was not found." });
    }

    [HttpGet("providers")]
    public async Task<IActionResult> Providers(
        [FromServices] IProviderService providers,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok(await providers.ListForWorkspaceAsync(scope.Value.WorkspaceId, ct));
    }

    [HttpGet("models")]
    public async Task<IActionResult> Models(
        [FromServices] IProviderService providers,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var rows = await providers.ListForWorkspaceAsync(scope.Value.WorkspaceId, ct);
        return Ok(rows.SelectMany(provider => provider.Models.Select(model => new
        {
            provider = provider.Name,
            model.Id,
            model.DisplayName,
            model.CostWeight,
            provider.Configured,
        })));
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static object ToProviderResource(ProviderResourceRecord provider) => new
    {
        kind = "Provider",
        name = provider.Name,
        id = provider.Id,
        type = provider.Type,
        displayName = provider.DisplayName,
        enabled = provider.Enabled,
        configured = provider.Enabled && !string.IsNullOrWhiteSpace(provider.EncryptedCredentialsJson),
        defaultModel = provider.DefaultModel,
        models = provider.Models,
        createdAt = provider.CreatedAt,
        updatedAt = provider.UpdatedAt,
    };
}

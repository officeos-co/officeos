using OffceOs.Features.Providers.Application;
using OffceOs.Features.Management.Domain;
using OffceOs.Features.Providers.Domain;
namespace OffceOs.Features.Providers.Api;

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

    [HttpPost("resources/providers/codex/auth")]
    [HttpPost("resources/provider/codex/auth")]
    public async Task<IActionResult> AuthenticateCodexProvider(
        [FromBody] CodexProviderAuthInput input,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IProviderService providers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var result = await providers.AuthenticateCodexAsync(scope.Value.WorkspaceId, new CodexProviderAuthRequest(
            input.AccessToken,
            input.RefreshToken,
            input.ExpiresAt,
            input.AccountEmail,
            input.AccountId,
            input.ClientId,
            input.TokenUrl,
            input.Scopes), ct);
        return Ok(result);
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

    [HttpGet("resources/models")]
    [HttpGet("resources/model")]
    public async Task<IActionResult> ListModelResources(
        [FromServices] IProviderService providers,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var rows = await providers.ListForWorkspaceAsync(scope.Value.WorkspaceId, ct);
        return Ok(rows.SelectMany(provider => provider.Models.Select(model => ToModelResource(provider, model))));
    }

    [HttpGet("resources/models/{name}")]
    [HttpGet("resources/model/{name}")]
    public async Task<IActionResult> DescribeModelResource(
        string name,
        [FromServices] IProviderService providers,
        [FromServices] IWorkspaceService workspaces,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var rows = await providers.ListForWorkspaceAsync(scope.Value.WorkspaceId, ct);
        var model = rows
            .SelectMany(provider => provider.Models.Select(item => ToModelResource(provider, item)))
            .FirstOrDefault(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.Id, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(item.DisplayName, name, StringComparison.OrdinalIgnoreCase));

        return model is null ? NotFound(new { error = $"models/{name} was not found." }) : Ok(model);
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
        phase = provider.Phase,
        statusMessage = provider.StatusMessage,
        account = provider.Account,
        expiresAt = provider.ExpiresAt,
        lastValidatedAt = provider.LastValidatedAt,
        defaultModel = provider.DefaultModel,
        models = provider.Models,
        createdAt = provider.CreatedAt,
        updatedAt = provider.UpdatedAt,
    };

    private static ModelResourcePayload ToModelResource(ProviderResult provider, ProviderModelResult model) => new(
        Kind: "Model",
        Name: model.Id,
        Provider: provider.Name,
        Id: model.Id,
        DisplayName: model.DisplayName,
        CostWeight: model.CostWeight,
        Configured: provider.Configured);
}

public sealed record ModelResourcePayload(
    string Kind,
    string Name,
    string Provider,
    string Id,
    string DisplayName,
    int CostWeight,
    bool Configured);

public sealed record CodexProviderAuthInput(
    string AccessToken,
    string RefreshToken,
    DateTime? ExpiresAt,
    string? AccountEmail,
    string? AccountId,
    string? ClientId,
    string? TokenUrl,
    IReadOnlyList<string>? Scopes);

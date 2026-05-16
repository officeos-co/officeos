using OffceOs.Application.Features.Browser;
using OffceOs.Domain.Features.Browser;
using OffceOs.Domain.Features.Management;
namespace OffceOs.Api.Features.Browser;

[ApiController]
[Route("api/v1/resources")]
public sealed class BrowserResourceController : ControllerBase
{
    [HttpGet("browsers")]
    [HttpGet("browser")]
    public async Task<IActionResult> ListBrowsers(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IBrowserResourceService browsers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok((await browsers.ListAsync(scope.Value.WorkspaceId, ct)).Select(ToBrowserResource));
    }

    [HttpGet("browsers/{name}")]
    [HttpGet("browser/{name}")]
    public async Task<IActionResult> DescribeBrowser(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IBrowserResourceService browsers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        if (!Guid.TryParse(name, out var id))
            return NotFound(new { error = $"browsers/{name} was not found." });

        var browser = await browsers.GetAsync(id, scope.Value.WorkspaceId, ct);
        return browser is null ? NotFound(new { error = $"browsers/{name} was not found." }) : Ok(ToBrowserResource(browser));
    }

    [HttpDelete("browsers/{name}")]
    [HttpDelete("browser/{name}")]
    public async Task<IActionResult> DeleteBrowser(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IBrowserResourceService browsers,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        return Guid.TryParse(name, out var id) &&
            await browsers.DeleteAsync(id, scope.Value.WorkspaceId, ct)
            ? Ok(new { deleted = true })
            : NotFound(new { error = $"browsers/{name} was not found." });
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static object ToBrowserResource(BrowserResourceRecord browser) => new
    {
        kind = "Browser",
        name = browser.Id.ToString(),
        id = browser.Id,
        browser.DisplayName,
        browser.CurrentAgentId,
        browser.CreatedAt,
        browser.UpdatedAt,
    };
}

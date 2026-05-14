namespace OffceOs.Api.Features.Context;

[ApiController]
[Route("api/control-plane/v1/resources")]
public sealed class MemoryStoreResourceController : ControllerBase
{
    [HttpGet("memorystores")]
    [HttpGet("memorystore")]
    [HttpGet("memory-stores")]
    [HttpGet("memory-store")]
    public async Task<IActionResult> ListMemoryStores(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IMemoryStoreRepository memoryStores,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok((await memoryStores.ListAsync(null, scope.Value.WorkspaceId, ct)).Select(ToMemoryStoreResource));
    }

    [HttpGet("memorystores/{name}")]
    [HttpGet("memorystore/{name}")]
    [HttpGet("memory-stores/{name}")]
    [HttpGet("memory-store/{name}")]
    public async Task<IActionResult> DescribeMemoryStore(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IMemoryStoreRepository memoryStores,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        if (!Guid.TryParse(name, out var id))
            return NotFound(new { error = $"memorystores/{name} was not found." });

        var store = await memoryStores.GetAsync(id, null, scope.Value.WorkspaceId, ct);
        return store is null ? NotFound(new { error = $"memorystores/{name} was not found." }) : Ok(ToMemoryStoreResource(store));
    }

    [HttpDelete("memorystores/{name}")]
    [HttpDelete("memorystore/{name}")]
    [HttpDelete("memory-stores/{name}")]
    [HttpDelete("memory-store/{name}")]
    public async Task<IActionResult> DeleteMemoryStore(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IMemoryStoreRepository memoryStores,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        return Guid.TryParse(name, out var memoryStoreId) &&
            await memoryStores.DeleteAsync(memoryStoreId, null, scope.Value.WorkspaceId, ct)
            ? Ok(new { deleted = true })
            : NotFound(new { error = $"memorystores/{name} was not found." });
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static object ToMemoryStoreResource(MemoryStoreRecord store) => new
    {
        kind = "MemoryStore",
        name = store.Id.ToString(),
        id = store.Id,
        store.DisplayName,
        store.CreatedAt,
    };
}

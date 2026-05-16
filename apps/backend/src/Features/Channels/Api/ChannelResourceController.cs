using OffceOs.Domain.Features.Channels;
using OffceOs.Domain.Features.Management;
namespace OffceOs.Api.Features.Channels;

[ApiController]
[Route("api/v1/resources")]
public sealed class ChannelResourceController : ControllerBase
{
    [HttpGet("channels")]
    [HttpGet("channel")]
    public async Task<IActionResult> ListChannels(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IChannelRepository channels,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        return Ok((await channels.ListConnectionsAsync(new ChannelConnectionFilter { WorkspaceId = scope.Value.WorkspaceId }, ct)).Select(ToChannelResource));
    }

    [HttpGet("channels/{name}")]
    [HttpGet("channel/{name}")]
    public async Task<IActionResult> DescribeChannel(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IChannelRepository channels,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });
        var channel = await FindChannelAsync(channels, name, scope.Value.WorkspaceId, ct);
        return channel is null ? NotFound(new { error = $"channels/{name} was not found." }) : Ok(ToChannelResource(channel));
    }

    [HttpDelete("channels/{name}")]
    [HttpDelete("channel/{name}")]
    public async Task<IActionResult> DeleteChannel(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IChannelRepository channels,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        return Guid.TryParse(name, out var channelId) &&
            await channels.DeleteConnectionAsync(channelId, ct)
            ? Ok(new { deleted = true })
            : NotFound(new { error = $"channels/{name} was not found." });
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static object ToChannelResource(ChannelConnectionRecord channel) => new
    {
        kind = "Channel",
        name = channel.Id.ToString(),
        id = channel.Id,
        type = channel.ChannelType.ToStorageString(),
        displayName = channel.DisplayName,
        enabled = channel.Enabled,
        createdAt = channel.CreatedAt,
    };

    private static async Task<ChannelConnectionRecord?> FindChannelAsync(
        IChannelRepository channels,
        string name,
        Guid workspaceId,
        CancellationToken ct)
    {
        if (!Guid.TryParse(name, out var id))
            return null;

        return await channels.GetConnectionByAsync(new ChannelConnectionFilter { Id = id, WorkspaceId = workspaceId }, ct);
    }
}

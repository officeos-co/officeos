using OffceOs.Domain.Features.Channels;
using OffceOs.Infrastructure.Common.Security;
namespace OffceOs.Api.Features.Channels;

[ApiController]
[Route("api/channels")]
public sealed class ChannelSidecarController : ControllerBase
{
    [HttpPost("inbound")]
    public async Task<IActionResult> Inbound(
        [FromBody] ChannelInboundInput request,
        [FromServices] IChannelService channelService,
        CancellationToken ct)
    {
        var agentIds = await channelService.RouteInboundAsync(
            request.ConnectionId,
            request.SenderIdentifier,
            request.MessageText,
            request.IsGroupMessage,
            request.MessageId,
            request.ChannelId,
            ct);

        return Ok(new { agentIds });
    }

    [HttpGet("active")]
    public async Task<IActionResult> Active(
        [FromServices] IChannelRepository channelRepository,
        [FromServices] ChannelCredentialProtector protector,
        CancellationToken ct)
    {
        var connections = await channelRepository.ListConnectionsAsync(ct: ct);
        var active = new List<object>();

        foreach (var conn in connections)
        {
            if (!conn.Enabled || string.IsNullOrEmpty(conn.EncryptedCreds))
                continue;

            try
            {
                var decrypted = protector.Unprotect(conn.EncryptedCreds);
                active.Add(new
                {
                    connectionId = conn.Id.ToString(),
                    channelType = conn.ChannelType.ToStorageString(),
                    creds = JsonSerializer.Deserialize<Dictionary<string, string>>(decrypted),
                });
            }
            catch
            {
                // Skip connections with corrupted creds.
            }
        }

        return Ok(active);
    }
}

public sealed record ChannelInboundInput(
    Guid ConnectionId,
    string SenderIdentifier,
    string MessageText,
    bool IsGroupMessage,
    string? MessageId,
    string? ChannelId);

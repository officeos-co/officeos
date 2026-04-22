namespace EnterpriseAgentOs.Api.Channels;

/// <summary>
/// Internal endpoints called by the WhatsApp Baileys sidecar (localhost only).
/// Not exposed publicly — the sidecar runs in the same pod.
/// </summary>
[ApiController]
[Route("api/internal")]
public sealed class WhatsAppInternalController : ControllerBase
{
    private readonly ChannelMessageRouter _channelMessageRouter;
    private readonly ILogger<WhatsAppInternalController> _logger;

    public WhatsAppInternalController(
        ChannelMessageRouter router,
        ILogger<WhatsAppInternalController> logger)
    {
        _channelMessageRouter = router;
        _logger = logger;
    }

    /// <summary>
    /// Called by the sidecar on startup to discover which connections to restore.
    /// Returns all enabled channel connections that have credentials stored.
    /// </summary>
    [HttpGet("channel/connections")]
    public async Task<IActionResult> ListActiveConnections(
        [FromServices] IChannelRepository repo,
        CancellationToken ct)
    {
        var all = await repo.ListConnectionsAsync(ct);
        var active = all
            .Where(c => c.Enabled && !string.IsNullOrEmpty(c.EncryptedConfig))
            .Select(c => new { id = c.Id.ToString(), channelType = c.ChannelType })
            .ToList();

        _logger.LogInformation("Sidecar requested active connections: {Count} found", active.Count);
        return Ok(new { connections = active });
    }

    /// <summary>
    /// Receive an inbound WhatsApp message from the sidecar, route to agents, return responses.
    /// </summary>
    [HttpPost("channel/inbound")]
    public async Task<IActionResult> HandleInbound([FromBody] WhatsAppInboundRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.ConnectionId) || string.IsNullOrEmpty(request.Text))
            return BadRequest();

        if (!Guid.TryParse(request.ConnectionId, out var connectionId))
            return BadRequest("Invalid connectionId");

        var sender = request.IsGroup ? (request.Participant ?? request.SenderJid) : request.SenderJid;

        _logger.LogInformation("WhatsApp inbound from {Sender} on connection {Id} (group: {IsGroup})",
            sender, connectionId, request.IsGroup);

        // Route to agents — replies are broadcast to all channels
        // via ChannelBroadcastService (triggered by MessageOut log append)
        var responses = await _channelMessageRouter.RouteMessageAsync(
            connectionId, sender ?? "", request.Text,
            isGroupMessage: request.IsGroup, messageId: request.MessageId,
            channelId: request.IsGroup ? request.SenderJid : sender, ct: ct);

        return Ok(new { responses });
    }

    /// <summary>
    /// Called by the sidecar when a WhatsApp connection is fully established
    /// (QR code scanned, session authenticated). Sends a test message to
    /// confirm the channel is working.
    /// </summary>
    [HttpPost("channel/connected")]
    public async Task<IActionResult> HandleConnected(
        [FromBody] ChannelConnectedRequest request,
        [FromServices] IChannelService channelService,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.ConnectionId))
            return BadRequest();

        if (!Guid.TryParse(request.ConnectionId, out var connectionId))
            return BadRequest("Invalid connectionId");

        _logger.LogInformation("Channel connected: {ConnectionId}", connectionId);

        await channelService.SendTestMessageAsync(connectionId, ct);
        return Ok();
    }

    /// <summary>
    /// Save WhatsApp session credentials from the sidecar.
    /// </summary>
    [HttpPost("wa/creds/{connectionId}")]
    public async Task<IActionResult> SaveCreds(
        Guid connectionId,
        [FromBody] WhatsAppCredsRequest request,
        [FromServices] IChannelService channelService,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.CredsJson))
            return BadRequest();

        await channelService.SaveChannelCredsAsync(connectionId, request.CredsJson, ct);
        return Ok();
    }

    /// <summary>
    /// Load WhatsApp session credentials for the sidecar.
    /// </summary>
    [HttpGet("wa/creds/{connectionId}")]
    public async Task<IActionResult> LoadCreds(
        Guid connectionId,
        [FromServices] IChannelService channelService,
        CancellationToken ct)
    {
        var credsJson = await channelService.LoadChannelCredsAsync(connectionId, ct);
        return Ok(new { credsJson });
    }
}

public sealed record WhatsAppInboundRequest(
    string? ConnectionId,
    string? SenderJid,
    string? Participant,
    string? Text,
    string? MessageId,
    bool IsGroup);

public sealed record WhatsAppCredsRequest(string? CredsJson);

public sealed record ChannelConnectedRequest(
    string? ConnectionId,
    string? SenderJid);

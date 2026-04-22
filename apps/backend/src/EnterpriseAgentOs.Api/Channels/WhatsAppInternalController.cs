namespace EnterpriseAgentOs.Api.Channels;

/// <summary>
/// Internal endpoints called by the WhatsApp Baileys sidecar (localhost only).
/// Not exposed publicly — the sidecar runs in the same pod.
/// </summary>
[ApiController]
[Route("api/internal")]
public sealed class WhatsAppInternalController : ControllerBase
{
    private readonly IChannelRepository _channelRepository;
    private readonly ChannelMessageRouter _channelMessageRouter;
    private readonly ChannelConfigProtector _channelConfigProtector;
    private readonly ILogger<WhatsAppInternalController> _logger;

    public WhatsAppInternalController(
        IChannelRepository repo,
        ChannelMessageRouter router,
        ChannelConfigProtector protector,
        ILogger<WhatsAppInternalController> logger)
    {
        _channelRepository = repo;
        _channelMessageRouter = router;
        _channelConfigProtector = protector;
        _logger = logger;
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
        if (string.IsNullOrEmpty(request.ConnectionId) || string.IsNullOrEmpty(request.SenderJid))
            return BadRequest();

        if (!Guid.TryParse(request.ConnectionId, out var connectionId))
            return BadRequest("Invalid connectionId");

        _logger.LogInformation("Channel connected: {ConnectionId}, owner JID: {Jid}", connectionId, request.SenderJid);

        await channelService.SendTestMessageAsync(connectionId, request.SenderJid, ct);
        return Ok();
    }

    /// <summary>
    /// Save WhatsApp session credentials from the sidecar.
    /// </summary>
    [HttpPost("wa/creds/{connectionId}")]
    public async Task<IActionResult> SaveCreds(Guid connectionId, [FromBody] WhatsAppCredsRequest request, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(request.CredsJson))
            return BadRequest();

        var encrypted = _channelConfigProtector.Protect(
            JsonSerializer.Serialize(new Dictionary<string, string> { ["credsJson"] = request.CredsJson }));

        await _channelRepository.UpdateConnectionAsync(connectionId, row =>
        {
            row.EncryptedConfig = encrypted;
        }, ct);

        return Ok();
    }

    /// <summary>
    /// Load WhatsApp session credentials for the sidecar.
    /// </summary>
    [HttpGet("wa/creds/{connectionId}")]
    public async Task<IActionResult> LoadCreds(Guid connectionId, CancellationToken ct)
    {
        var connection = await _channelRepository.GetConnectionAsync(connectionId, ct);
        if (connection is null || string.IsNullOrEmpty(connection.EncryptedConfig))
            return Ok(new { credsJson = (string?)null });

        try
        {
            var json = _channelConfigProtector.Unprotect(connection.EncryptedConfig);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            var credsJson = dict?.GetValueOrDefault("credsJson");
            return Ok(new { credsJson });
        }
        catch
        {
            return Ok(new { credsJson = (string?)null });
        }
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

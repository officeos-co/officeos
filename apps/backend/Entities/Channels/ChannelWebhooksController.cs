namespace EnterpriseAgentOs.Api.Entities.Channels;

/// <summary>
/// Inbound webhook endpoints for each platform. These are unauthenticated
/// (no session cookie required) — each platform has its own verification
/// mechanism (signing secrets, tokens, etc).
/// </summary>
[ApiController]
[Route("api/webhooks")]
public sealed class ChannelWebhooksController : ControllerBase
{
    private readonly IChannelRepository _repo;
    private readonly ChannelMessageRouter _router;
    private readonly ChannelConfigProtector _protector;
    private readonly ILogger<ChannelWebhooksController> _logger;

    public ChannelWebhooksController(
        IChannelRepository repo,
        ChannelMessageRouter router,
        ChannelConfigProtector protector,
        ILogger<ChannelWebhooksController> logger)
    {
        _repo = repo;
        _router = router;
        _protector = protector;
        _logger = logger;
    }

    // ======================== SLACK ========================

    [HttpPost("slack/events")]
    public async Task<IActionResult> SlackEvents(CancellationToken ct)
    {
        var body = await ReadBodyAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        // Handle Slack URL verification challenge
        if (json.TryGetProperty("type", out var typeProp) &&
            typeProp.GetString() == "url_verification")
        {
            var challenge = json.GetProperty("challenge").GetString();
            return Ok(new { challenge });
        }

        // Process event callbacks
        if (typeProp.GetString() != "event_callback") return Ok();

        if (!json.TryGetProperty("event", out var eventObj)) return Ok();
        var eventType = eventObj.TryGetProperty("type", out var et) ? et.GetString() : null;
        if (eventType != "message") return Ok();

        // Ignore bot messages to prevent loops
        if (eventObj.TryGetProperty("bot_id", out _)) return Ok();
        if (eventObj.TryGetProperty("subtype", out _)) return Ok();

        var text = eventObj.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
        var channel = eventObj.TryGetProperty("channel", out var ch) ? ch.GetString() ?? "" : "";
        var user = eventObj.TryGetProperty("user", out var u) ? u.GetString() ?? "" : "";

        // Find matching channel connection by verifying signing secret
        var connections = await _repo.ListConnectionsAsync(ct);
        var slackConnections = connections.Where(c =>
            c.ChannelType == "slack" && c.Enabled).ToList();

        foreach (var connection in slackConnections)
        {
            var config = _router.GetDecryptedConfig(connection);

            // Verify Slack signature if signing secret is configured
            if (config.TryGetValue("signingSecret", out var signingSecret) &&
                !string.IsNullOrEmpty(signingSecret))
            {
                if (!VerifySlackSignature(body, signingSecret))
                    continue;
            }

            var responses = await _router.RouteMessageAsync(connection.Id, user, text, ct);

            foreach (var (_, responseText) in responses)
            {
                await _router.SendPlatformReplyAsync(connection, channel, responseText, ct);
            }

            return Ok();
        }

        _logger.LogWarning("No matching Slack connection found for incoming event");
        return Ok();
    }

    // ======================== TELEGRAM ========================

    [HttpPost("telegram/{connectionId:guid}")]
    public async Task<IActionResult> TelegramUpdate(Guid connectionId, CancellationToken ct)
    {
        var connection = await _repo.GetConnectionAsync(connectionId, ct);
        if (connection is null || connection.ChannelType != "telegram" || !connection.Enabled)
            return NotFound();

        var body = await ReadBodyAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        // Extract message from Telegram update
        if (!json.TryGetProperty("message", out var message)) return Ok();
        if (!message.TryGetProperty("text", out var textProp)) return Ok();

        var text = textProp.GetString() ?? "";
        var chatId = message.GetProperty("chat").GetProperty("id").GetInt64().ToString();
        var fromId = message.TryGetProperty("from", out var from) &&
                     from.TryGetProperty("id", out var fid)
            ? fid.GetInt64().ToString()
            : "";

        var responses = await _router.RouteMessageAsync(connection.Id, fromId, text, ct);

        foreach (var (_, responseText) in responses)
        {
            await _router.SendPlatformReplyAsync(connection, chatId, responseText, ct);
        }

        return Ok();
    }

    // ======================== DISCORD ========================

    [HttpPost("discord")]
    public async Task<IActionResult> DiscordInteraction(CancellationToken ct)
    {
        var body = await ReadBodyAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);

        // Discord interaction type 1 = PING (verification)
        if (json.TryGetProperty("type", out var typeProp) && typeProp.GetInt32() == 1)
        {
            return Ok(new { type = 1 });
        }

        // Type 2 = APPLICATION_COMMAND, type 3 = MESSAGE_COMPONENT
        // For now, handle basic message-like interactions
        if (!json.TryGetProperty("data", out var data)) return Ok();

        var content = data.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(content) && data.TryGetProperty("options", out var options))
        {
            // Try to extract from slash command options
            var firstOpt = options.EnumerateArray().FirstOrDefault();
            if (firstOpt.TryGetProperty("value", out var val))
                content = val.GetString() ?? "";
        }

        var userId = json.TryGetProperty("member", out var member) &&
                     member.TryGetProperty("user", out var discordUser) &&
                     discordUser.TryGetProperty("id", out var uid)
            ? uid.GetString() ?? ""
            : "";

        // Find Discord connections
        var connections = await _repo.ListConnectionsAsync(ct);
        var discordConnections = connections.Where(c =>
            c.ChannelType == "discord" && c.Enabled).ToList();

        foreach (var connection in discordConnections)
        {
            var config = _router.GetDecryptedConfig(connection);

            // Verify Discord signature
            if (config.TryGetValue("publicKey", out var publicKey) &&
                !string.IsNullOrEmpty(publicKey))
            {
                // Discord uses Ed25519 signatures — full implementation
                // would verify here; for now we trust the request
            }

            var responses = await _router.RouteMessageAsync(connection.Id, userId, content, ct);

            if (responses.Count > 0)
            {
                var responseText = string.Join("\n", responses.Values);
                // Discord interaction response type 4 = CHANNEL_MESSAGE_WITH_SOURCE
                return Ok(new { type = 4, data = new { content = responseText } });
            }
        }

        return Ok(new { type = 4, data = new { content = "No agent available to handle this message." } });
    }

    // ======================== Helpers ========================

    private async Task<string> ReadBodyAsync()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private bool VerifySlackSignature(string body, string signingSecret)
    {
        if (!Request.Headers.TryGetValue("X-Slack-Signature", out var signature) ||
            !Request.Headers.TryGetValue("X-Slack-Request-Timestamp", out var timestamp))
            return false;

        var sig = signature.ToString();
        var ts = timestamp.ToString();

        var basestring = $"v0:{ts}:{body}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(basestring));
        var computed = $"v0={Convert.ToHexString(hash).ToLowerInvariant()}";

        return string.Equals(sig, computed, StringComparison.OrdinalIgnoreCase);
    }
}

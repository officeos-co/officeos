namespace EnterpriseAgentOs.Api.Controllers;

/// <summary>
/// Generic inbound webhook endpoint for all channel platforms. Delegates to
/// platform-specific <see cref="IChannelAdapter"/> implementations via the
/// <see cref="ChannelAdapterRegistry"/>.
///
/// These endpoints are unauthenticated (no session cookie) — each platform
/// has its own verification mechanism handled by its adapter.
/// </summary>
[ApiController]
[Route("api/webhooks")]
public sealed class ChannelWebhooksController : ControllerBase
{
    private readonly IChannelRepository _repo;
    private readonly ChannelMessageRouter _router;
    private readonly ChannelAdapterRegistry _adapters;
    private readonly ILogger<ChannelWebhooksController> _logger;

    public ChannelWebhooksController(
        IChannelRepository repo,
        ChannelMessageRouter router,
        ChannelAdapterRegistry adapters,
        ILogger<ChannelWebhooksController> logger)
    {
        _repo = repo;
        _router = router;
        _adapters = adapters;
        _logger = logger;
    }

    /// <summary>
    /// Single webhook endpoint for all platforms.
    /// Route: POST /api/webhooks/{platform}/{endpoint?}
    ///
    /// For platforms that include a connection ID in the URL (e.g. Telegram),
    /// pass it as the endpoint parameter.
    /// </summary>
    [HttpPost("{platform}/{endpoint?}")]
    public async Task<IActionResult> HandleWebhook(string platform, string? endpoint, CancellationToken ct)
    {
        var adapter = _adapters.GetAdapter(platform);
        if (adapter is null)
        {
            _logger.LogWarning("No adapter registered for platform {Platform}", platform);
            return NotFound();
        }

        // Read raw body once for signature verification + parsing
        var rawBody = await ReadRawBodyAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(rawBody);

        // Resolve the connection(s) to use
        var connections = await ResolveConnectionsAsync(platform, endpoint, ct);
        if (connections.Count == 0)
        {
            _logger.LogWarning("No matching {Platform} connection found", platform);
            return Ok(); // Return 200 to avoid platform retries
        }

        foreach (var connection in connections)
        {
            var config = _router.GetDecryptedConfig(connection);

            // Extract headers for signature verification
            var headers = Request.Headers.ToDictionary(
                h => h.Key,
                h => h.Value.ToString(),
                StringComparer.OrdinalIgnoreCase);

            // Verify platform-specific signature
            if (!adapter.VerifySignature(rawBody, config, headers))
            {
                _logger.LogWarning("Signature verification failed for {Platform} connection {Id}",
                    platform, connection.Id);
                continue;
            }

            // Parse the inbound message
            var message = adapter.ParseInbound(json);
            if (message is null)
                return Ok();

            // Handle platform verification challenges (Slack URL verification, Discord PING, etc.)
            if (message.IsChallenge)
            {
                if (message.ChallengeResponse is not null)
                {
                    // Try to return as JSON if it's valid JSON, otherwise as plain text
                    try
                    {
                        var challengeJson = JsonSerializer.Deserialize<JsonElement>(message.ChallengeResponse);
                        return Ok(challengeJson);
                    }
                    catch
                    {
                        return Ok(new { challenge = message.ChallengeResponse });
                    }
                }
                return Ok();
            }

            // Route message to bound agents
            var responses = await _router.RouteMessageAsync(connection.Id, message.SenderIdentifier, message.Text, ct);

            // Send replies back through the platform
            var httpClient = HttpContext.RequestServices
                .GetRequiredService<IHttpClientFactory>()
                .CreateClient("channel-platform");

            foreach (var (_, responseText) in responses)
            {
                await adapter.SendReplyAsync(httpClient, config, message.ChannelId, responseText, ct);
            }

            return Ok();
        }

        return Ok();
    }

    /// <summary>
    /// WhatsApp webhook verification (GET request with hub.verify_token challenge).
    /// </summary>
    [HttpGet("{platform}/{endpoint?}")]
    public async Task<IActionResult> HandleWebhookVerification(
        string platform,
        string? endpoint,
        [FromQuery(Name = "hub.mode")] string? hubMode,
        [FromQuery(Name = "hub.verify_token")] string? hubVerifyToken,
        [FromQuery(Name = "hub.challenge")] string? hubChallenge,
        CancellationToken ct)
    {
        if (hubMode != "subscribe" || string.IsNullOrEmpty(hubChallenge))
            return NotFound();

        // Find the connection and check verify token
        var connections = await ResolveConnectionsAsync(platform, endpoint, ct);
        foreach (var connection in connections)
        {
            var config = _router.GetDecryptedConfig(connection);
            if (config.TryGetValue("verifyToken", out var token) &&
                string.Equals(token, hubVerifyToken, StringComparison.Ordinal))
            {
                return Ok(hubChallenge);
            }
        }

        return Unauthorized();
    }

    private async Task<List<ChannelConnectionRecord>> ResolveConnectionsAsync(
        string platform, string? endpoint, CancellationToken ct)
    {
        // If endpoint is a GUID, resolve directly (e.g. Telegram uses /telegram/{connectionId})
        if (Guid.TryParse(endpoint, out var connectionId))
        {
            var connection = await _repo.GetConnectionAsync(connectionId, ct);
            if (connection is not null && connection.Enabled &&
                string.Equals(connection.ChannelType, platform, StringComparison.OrdinalIgnoreCase))
            {
                return [connection];
            }
            return [];
        }

        // Otherwise, find all enabled connections for this platform
        var all = await _repo.ListConnectionsAsync(ct);
        return all.Where(c =>
            string.Equals(c.ChannelType, platform, StringComparison.OrdinalIgnoreCase) &&
            c.Enabled).ToList();
    }

    private async Task<byte[]> ReadRawBodyAsync()
    {
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        return ms.ToArray();
    }
}

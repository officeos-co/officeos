using EnterpriseAgentOs.Infrastructure.Channels.Common;

namespace EnterpriseAgentOs.Infrastructure.Channels;

/// <summary>
/// Infrastructure implementation of <see cref="IChannelGateway"/>.
/// Routes outbound messages to the correct platform transport:
/// WhatsApp via the Baileys sidecar, everything else via the adapter registry.
/// All platform-specific destination resolution happens here.
/// </summary>
public sealed class ChannelGateway : IChannelGateway
{
    private readonly ChannelAdapterRegistry _adapterRegistry;
    private readonly ChannelConfigProtector _configProtector;
    private readonly WhatsAppGatewayService _whatsApp;
    private readonly IChannelRepository _channelRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ChannelGateway> _logger;

    public ChannelGateway(
        ChannelAdapterRegistry adapterRegistry,
        ChannelConfigProtector configProtector,
        WhatsAppGatewayService whatsApp,
        IChannelRepository channelRepository,
        IHttpClientFactory httpClientFactory,
        ILogger<ChannelGateway> logger)
    {
        _adapterRegistry = adapterRegistry;
        _configProtector = configProtector;
        _whatsApp = whatsApp;
        _channelRepository = channelRepository;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendAsync(Guid connectionId, string text, CancellationToken ct = default)
    {
        var connection = await _channelRepository.GetConnectionAsync(connectionId, ct);
        if (connection is null)
            throw new InvalidOperationException($"Connection {connectionId} not found");

        if (string.Equals(connection.ChannelType, "whatsapp", StringComparison.OrdinalIgnoreCase))
        {
            var destination = ExtractWhatsAppOwnerJid(connection);
            if (string.IsNullOrEmpty(destination))
                throw new InvalidOperationException($"WhatsApp connection {connectionId} has no owner JID in creds");

            _logger.LogInformation("Sending WhatsApp message to {Jid} on connection {Id}", destination, connectionId);
            await _whatsApp.SendMessageAsync(connectionId, destination, text);
            return;
        }

        var adapter = _adapterRegistry.GetAdapter(connection.ChannelType);
        if (adapter is null)
            throw new InvalidOperationException($"No adapter for channel type {connection.ChannelType}");

        var config = DecryptConfig(connection);
        var defaultChannel = config.GetValueOrDefault("defaultChannelId") ?? "";
        var httpClient = _httpClientFactory.CreateClient("channel-platform");
        await adapter.SendReplyAsync(httpClient, config, defaultChannel, text, ct);
    }

    public async Task StartConnectionAsync(Guid connectionId, string channelType, CancellationToken ct = default)
    {
        if (string.Equals(channelType, "whatsapp", StringComparison.OrdinalIgnoreCase))
            await _whatsApp.StartConnectionAsync(connectionId);
    }

    public async Task StopConnectionAsync(Guid connectionId, string channelType, CancellationToken ct = default)
    {
        if (string.Equals(channelType, "whatsapp", StringComparison.OrdinalIgnoreCase))
            await _whatsApp.StopConnectionAsync(connectionId);
    }

    private Dictionary<string, string> DecryptConfig(ChannelConnectionRecord connection)
    {
        if (string.IsNullOrEmpty(connection.EncryptedConfig))
            return new Dictionary<string, string>();

        var json = _configProtector.Unprotect(connection.EncryptedConfig);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Extract and normalize the WhatsApp owner JID from the connection's encrypted creds.
    /// Platform-specific logic lives here in infrastructure, not in domain.
    /// </summary>
    private string? ExtractWhatsAppOwnerJid(ChannelConnectionRecord connection)
    {
        if (string.IsNullOrEmpty(connection.EncryptedConfig))
        {
            _logger.LogWarning("Connection {Id}: EncryptedConfig is null/empty — connection was never paired or creds were lost", connection.Id);
            return null;
        }

        try
        {
            var json = _configProtector.Unprotect(connection.EncryptedConfig);
            var outerDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            var credsJson = outerDict?.GetValueOrDefault("credsJson");
            if (string.IsNullOrEmpty(credsJson))
            {
                _logger.LogWarning("Connection {Id}: no 'credsJson' key in config", connection.Id);
                return null;
            }

            // New format: credsJson is a JSON object like {"creds.json": "...", "session-x.json": "..."}
            // Old format: credsJson is the raw creds.json content with "me" at top level
            using var doc = JsonDocument.Parse(credsJson);

            // Check if top-level has "me" (old format — raw creds.json)
            if (doc.RootElement.TryGetProperty("me", out var me) &&
                me.TryGetProperty("id", out var idProp))
            {
                return NormalizeWhatsAppJid(idProp.GetString());
            }

            // New format: look for "creds.json" key, then parse that
            if (doc.RootElement.TryGetProperty("creds.json", out var credsFile))
            {
                var rawCreds = credsFile.GetString();
                if (!string.IsNullOrEmpty(rawCreds))
                {
                    using var credsDoc = JsonDocument.Parse(rawCreds);
                    if (credsDoc.RootElement.TryGetProperty("me", out var me2) &&
                        me2.TryGetProperty("id", out var idProp2))
                    {
                        return NormalizeWhatsAppJid(idProp2.GetString());
                    }
                }
            }

            _logger.LogWarning("Connection {Id}: could not find 'me.id' in auth state", connection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection {Id}: failed to extract owner JID", connection.Id);
        }
        return null;
    }

    /// <summary>
    /// Normalize a WhatsApp JID by stripping the device suffix (":N@" → "@").
    /// </summary>
    private static string? NormalizeWhatsAppJid(string? jid)
    {
        if (string.IsNullOrEmpty(jid)) return null;
        // Strip device suffix: "12345:6@s.whatsapp.net" → "12345@s.whatsapp.net"
        var colonIdx = jid.IndexOf(':');
        var atIdx = jid.IndexOf('@');
        if (colonIdx > 0 && atIdx > colonIdx)
            return jid[..colonIdx] + jid[atIdx..];
        return jid;
    }
}

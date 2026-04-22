using EnterpriseAgentOs.Infrastructure.Channels.Common;

namespace EnterpriseAgentOs.Infrastructure.Channels;

/// <summary>
/// Infrastructure implementation of <see cref="IChannelGateway"/>.
/// Routes outbound messages to the correct platform transport:
/// WhatsApp via the Baileys sidecar, everything else via the adapter registry.
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

    public async Task SendAsync(Guid connectionId, string channelType, string destination, string text, CancellationToken ct = default)
    {
        if (string.Equals(channelType, "whatsapp", StringComparison.OrdinalIgnoreCase))
        {
            await _whatsApp.SendMessageAsync(connectionId, destination, text);
            return;
        }

        var adapter = _adapterRegistry.GetAdapter(channelType);
        if (adapter is null)
        {
            _logger.LogWarning("No adapter for channel type {Type}, cannot send", channelType);
            return;
        }

        var connection = await _channelRepository.GetConnectionAsync(connectionId, ct);
        if (connection is null)
        {
            _logger.LogWarning("Connection {Id} not found, cannot send", connectionId);
            return;
        }

        var config = DecryptConfig(connection);
        var httpClient = _httpClientFactory.CreateClient("channel-platform");
        await adapter.SendReplyAsync(httpClient, config, destination, text, ct);
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
}

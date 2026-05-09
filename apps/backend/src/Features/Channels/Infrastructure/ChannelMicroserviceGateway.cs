namespace EnterpriseAgentOs.Infrastructure.Features.Channels;

/// <summary>
/// HTTP proxy to the channel sidecar (same K8s pod, localhost:3100).
/// </summary>
public sealed class ChannelSidecarGateway : IChannelGateway
{
    private readonly HttpClient _http;
    private readonly ILogger<ChannelSidecarGateway> _logger;

    public ChannelSidecarGateway(IHttpClientFactory httpClientFactory, ILogger<ChannelSidecarGateway> logger)
    {
        _http = httpClientFactory.CreateClient("channel-sidecar");
        _logger = logger;
    }

    public async Task SendAsync(string channelType, string platformId, string? threadId,
                                ChannelMessage message, CancellationToken ct = default)
    {
        var payload = new { channelType, platformId, threadId, message = new { kind = message.Kind, content = message.Content } };
        var response = await _http.PostAsJsonAsync("/send", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.PostAsync("/reload", null, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Channel sidecar reload failed — sidecar may not be running");
        }
    }
}

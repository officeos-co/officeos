namespace EnterpriseAgentOs.Infrastructure.Features.Channels;

/// <summary>
/// HTTP proxy to the channels microservice (apps/channels).
/// Replaces the old per-platform adapter registry with a single HTTP client.
/// </summary>
public sealed class ChannelMicroserviceGateway : IChannelGateway
{
    private readonly HttpClient _http;
    private readonly ILogger<ChannelMicroserviceGateway> _logger;

    public ChannelMicroserviceGateway(IHttpClientFactory httpClientFactory, ILogger<ChannelMicroserviceGateway> logger)
    {
        _http = httpClientFactory.CreateClient("channel-microservice");
        _logger = logger;
    }

    public async Task SendAsync(Guid connectionId, string text, CancellationToken ct = default)
    {
        var payload = new { connectionId, text };
        var response = await _http.PostAsJsonAsync("/api/send", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task StartConnectionAsync(Guid connectionId, string channelType, CancellationToken ct = default)
    {
        var payload = new { connectionId, channelType };
        var response = await _http.PostAsJsonAsync("/api/connections/start", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task StopConnectionAsync(Guid connectionId, string channelType, CancellationToken ct = default)
    {
        var response = await _http.DeleteAsync($"/api/connections/{connectionId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task SaveCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default)
    {
        var payload = new { connectionId, credsJson };
        var response = await _http.PostAsJsonAsync($"/api/connections/{connectionId}/creds", payload, ct);
        response.EnsureSuccessStatusCode();
    }
}

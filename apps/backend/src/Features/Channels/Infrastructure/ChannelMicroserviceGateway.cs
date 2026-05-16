using OffceOs.Domain.Features.Channels;

namespace OffceOs.Infrastructure.Features.Channels;

/// <summary>
/// HTTP proxy to the channel sidecar (same K8s pod, localhost:3100).
/// </summary>
public sealed class ChannelSidecarGateway : IChannelGateway
{
    private readonly HttpClient _httpClient;

    public ChannelSidecarGateway(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("channel-sidecar");
    }

    public async Task SendAsync(Guid connectionId, string channelType, string platformId, string? threadId,
                                ChannelMessage message, CancellationToken ct = default)
    {
        var payload = new
        {
            connectionId = connectionId.ToString(),
            channelType,
            platformId,
            threadId,
            message = new { kind = message.Kind, content = message.Content }
        };
        var response = await _httpClient.PostAsJsonAsync("/send", payload, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReloadAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.PostAsync("/reload", null, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _ = ex;
        }
    }
}

using System.Text.Json;
using EnterpriseAgentOs.Infrastructure.Channels.Common;
using EnterpriseAgentOs.Infrastructure.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Infrastructure.Channels;

/// <summary>
/// Proxies WhatsApp operations to the Baileys sidecar (Node.js) via HTTP.
/// The sidecar runs on localhost:3001 inside the same pod.
/// </summary>
public sealed class WhatsAppGatewayService : IDisposable
{
    private readonly HttpClient _http;
    private readonly ILogger<WhatsAppGatewayService> _logger;

    // In-memory cache of last known status per connection (updated by polling or push)
    private readonly Dictionary<Guid, WhatsAppStatus> _statusCache = new();

    public WhatsAppGatewayService(
        IHttpClientFactory httpClientFactory,
        WhatsAppGatewayConfig config,
        ILogger<WhatsAppGatewayService> logger)
    {
        _http = httpClientFactory.CreateClient("whatsapp-gateway");
        _http.BaseAddress = new Uri(config.Url);
        _http.Timeout = TimeSpan.FromSeconds(30);
        _logger = logger;
    }

    /// <summary>
    /// Start a new WhatsApp connection via the sidecar.
    /// Returns the initial status (may include QR data).
    /// </summary>
    public async Task<WhatsAppStatus> StartConnectionAsync(Guid connectionId)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new { connectionId = connectionId.ToString() });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");

            var response = await _http.PostAsync("/connect", content);
            var body = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("WhatsApp connect response for {Id}: {Status} {Body}",
                connectionId, response.StatusCode, body);

            if (!response.IsSuccessStatusCode)
            {
                return new WhatsAppStatus("error", null);
            }

            var result = JsonSerializer.Deserialize<JsonElement>(body);
            var status = result.TryGetProperty("status", out var s) ? s.GetString() ?? "connecting" : "connecting";
            var qrData = result.TryGetProperty("qrData", out var q) && q.ValueKind != JsonValueKind.Null
                ? q.GetString() : null;

            var waStatus = new WhatsAppStatus(status, qrData);
            _statusCache[connectionId] = waStatus;
            return waStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WhatsApp connection {Id}", connectionId);
            return new WhatsAppStatus("error", null);
        }
    }

    /// <summary>
    /// Stop a WhatsApp connection via the sidecar.
    /// </summary>
    public async Task StopConnectionAsync(Guid connectionId)
    {
        try
        {
            await _http.PostAsync($"/disconnect/{connectionId}", null);
            _statusCache.Remove(connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stop WhatsApp connection {Id}", connectionId);
        }
    }

    /// <summary>
    /// Get current connection status from the sidecar.
    /// </summary>
    public async Task<WhatsAppStatus> GetStatusAsync(Guid connectionId)
    {
        try
        {
            var response = await _http.GetAsync($"/status/{connectionId}");
            if (!response.IsSuccessStatusCode)
            {
                return new WhatsAppStatus("closed", null);
            }

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(body);
            var status = result.TryGetProperty("status", out var s) ? s.GetString() ?? "closed" : "closed";
            var qrData = result.TryGetProperty("qrData", out var q) && q.ValueKind != JsonValueKind.Null
                ? q.GetString() : null;

            var waStatus = new WhatsAppStatus(status, qrData);
            _statusCache[connectionId] = waStatus;
            return waStatus;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get WhatsApp status for {Id} (sidecar may not be running)", connectionId);
            return _statusCache.GetValueOrDefault(connectionId) ?? new WhatsAppStatus("closed", null);
        }
    }

    /// <summary>
    /// Send a message through an active WhatsApp connection.
    /// </summary>
    public async Task SendMessageAsync(Guid connectionId, string jid, string text)
    {
        var converted = MarkdownFormatConverter.Convert(text, "whatsapp");
        var chunks = PlatformTextChunker.ChunkForPlatform(converted, "whatsapp");

        foreach (var chunk in chunks)
        {
            var payload = JsonSerializer.Serialize(new
            {
                connectionId = connectionId.ToString(),
                jid,
                text = chunk,
            });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var response = await _http.PostAsync("/send", content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to send WhatsApp message: {Status}", response.StatusCode);
            }
        }
    }

    public void Dispose()
    {
        // HttpClient is managed by IHttpClientFactory, no disposal needed
    }
}

public sealed record WhatsAppStatus(string Status, string? QrData);

namespace EnterpriseAgentOs.Infrastructure.Adapters.Channels;

/// <summary>
/// Routes incoming platform messages to the correct agent's chat gateway
/// and sends agent responses back through the platform API.
/// </summary>
public sealed class ChannelMessageRouter
{
    private readonly IChannelRepository _repo;
    private readonly ChannelConfigProtector _protector;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ChannelMessageRouter> _logger;

    public ChannelMessageRouter(
        IChannelRepository repo,
        ChannelConfigProtector protector,
        IHttpClientFactory httpFactory,
        ILogger<ChannelMessageRouter> logger)
    {
        _repo = repo;
        _protector = protector;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    /// <summary>
    /// Forward an incoming message to every agent bound to the given connection,
    /// collect responses, and return them keyed by agent ID.
    /// </summary>
    public async Task<Dictionary<Guid, string>> RouteMessageAsync(
        Guid connectionId,
        string senderIdentifier,
        string messageText,
        CancellationToken ct = default)
    {
        var bindings = await _repo.FindBindingsByConnectionAsync(connectionId, ct);
        var responses = new Dictionary<Guid, string>();

        foreach (var binding in bindings)
        {
            if (binding.Agent is null) continue;

            var agentConfig = DeserializeBindingConfig(binding.Config);

            // Check access policy
            if (!IsAllowed(agentConfig, senderIdentifier))
            {
                _logger.LogDebug("Message from {Sender} blocked by policy for agent {AgentId}",
                    senderIdentifier, binding.AgentId);
                continue;
            }

            var serviceUrl = binding.Agent.ServiceUrl;
            if (string.IsNullOrEmpty(serviceUrl))
            {
                _logger.LogWarning("Agent {AgentId} has no service URL, skipping", binding.AgentId);
                continue;
            }

            try
            {
                var response = await SendToAgentAsync(serviceUrl, messageText, ct);
                responses[binding.AgentId] = response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forward message to agent {AgentId}", binding.AgentId);
                responses[binding.AgentId] = "Sorry, I'm having trouble processing your message right now.";
            }
        }

        return responses;
    }

    /// <summary>
    /// Send a reply back through the platform API.
    /// </summary>
    public async Task SendPlatformReplyAsync(
        EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord connection,
        string channelId,
        string text,
        CancellationToken ct = default)
    {
        var config = GetDecryptedConfig(connection);
        var client = _httpFactory.CreateClient("channel-platform");

        switch (connection.ChannelType.ToLowerInvariant())
        {
            case "slack":
                await SendSlackReplyAsync(client, config, channelId, text, ct);
                break;
            case "telegram":
                await SendTelegramReplyAsync(client, config, channelId, text, ct);
                break;
            case "discord":
                // Discord uses interaction responses, handled inline in the webhook controller
                break;
            default:
                _logger.LogWarning("Platform reply not implemented for channel type {Type}", connection.ChannelType);
                break;
        }
    }

    public Dictionary<string, string> GetDecryptedConfig(EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord connection)
    {
        if (string.IsNullOrEmpty(connection.EncryptedConfig))
            return new Dictionary<string, string>();

        var json = _protector.Unprotect(connection.EncryptedConfig);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
    }

    private async Task<string> SendToAgentAsync(string serviceUrl, string message, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("agent-proxy");
        var chatUrl = $"{serviceUrl.TrimEnd('/')}/api/chat";

        var payload = JsonSerializer.Serialize(new { message });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(chatUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<JsonElement>(body);

        return result.TryGetProperty("response", out var resp)
            ? resp.GetString() ?? ""
            : body;
    }

    private static AgentChannelConfig DeserializeBindingConfig(string? configJson)
    {
        if (string.IsNullOrEmpty(configJson))
            return new AgentChannelConfig();

        return JsonSerializer.Deserialize<AgentChannelConfig>(configJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? new AgentChannelConfig();
    }

    private static bool IsAllowed(AgentChannelConfig config, string senderIdentifier)
    {
        if (config.DmPolicy == "disabled") return false;
        if (config.DmPolicy == "open") return true;

        if (config.DmPolicy == "allowlist" && config.AllowedUsers is not null)
        {
            return config.AllowedUsers.Any(u =>
                string.Equals(u, senderIdentifier, StringComparison.OrdinalIgnoreCase));
        }

        return true;
    }

    private static async Task SendSlackReplyAsync(
        HttpClient client, Dictionary<string, string> config,
        string channelId, string text, CancellationToken ct)
    {
        if (!config.TryGetValue("botToken", out var token)) return;

        var payload = JsonSerializer.Serialize(new { channel = channelId, text });
        var request = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/chat.postMessage")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        await client.SendAsync(request, ct);
    }

    private static async Task SendTelegramReplyAsync(
        HttpClient client, Dictionary<string, string> config,
        string chatId, string text, CancellationToken ct)
    {
        if (!config.TryGetValue("botToken", out var token)) return;

        var payload = JsonSerializer.Serialize(new { chat_id = chatId, text });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");
        await client.PostAsync($"https://api.telegram.org/bot{token}/sendMessage", content, ct);
    }
}

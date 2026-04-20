using System.Net.WebSockets;
using System.Text;

namespace EnterpriseAgentOs.Infrastructure.Adapters.Channels;

/// <summary>
/// Routes incoming platform messages to the correct agent's chat gateway
/// and sends agent responses back through the platform API.
/// </summary>
public sealed class ChannelMessageRouter
{
    private readonly IChannelRepository _channelRepository;
    private readonly IAgentLogRepository _agentLogRepository;
    private readonly ChannelConfigProtector _channelConfigProtector;
    private readonly ILogger<ChannelMessageRouter> _logger;

    public ChannelMessageRouter(
        IChannelRepository repo,
        IAgentLogRepository logRepo,
        ChannelConfigProtector protector,
        ILogger<ChannelMessageRouter> logger)
    {
        _channelRepository = repo;
        _agentLogRepository = logRepo;
        _channelConfigProtector = protector;
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
        var bindings = await _channelRepository.FindBindingsByConnectionAsync(connectionId, ct);
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

            var correlationId = Guid.NewGuid().ToString("N");
            var channelType = binding.ChannelConnection?.ChannelType ?? "unknown";

            // Log inbound channel message
            await _agentLogRepository.AppendAsync(new AgentLogRecord
            {
                AgentId = binding.AgentId,
                Type = AgentLogType.ChannelIn,
                Channel = channelType,
                Content = messageText,
                CorrelationId = correlationId,
            }, ct);

            try
            {
                var response = await SendToAgentAsync(serviceUrl, binding.AgentId, messageText, ct);
                responses[binding.AgentId] = response;

                // Log outbound agent response
                await _agentLogRepository.AppendAsync(new AgentLogRecord
                {
                    AgentId = binding.AgentId,
                    Type = AgentLogType.ChannelOut,
                    Channel = channelType,
                    Content = response,
                    CorrelationId = correlationId,
                }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to forward message to agent {AgentId}", binding.AgentId);
                responses[binding.AgentId] = "Sorry, I'm having trouble processing your message right now.";
            }
        }

        return responses;
    }

    public Dictionary<string, string> GetDecryptedConfig(ChannelConnectionRecord connection)
    {
        if (string.IsNullOrEmpty(connection.EncryptedConfig))
            return new Dictionary<string, string>();

        var json = _channelConfigProtector.Unprotect(connection.EncryptedConfig);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? new Dictionary<string, string>();
    }

    private static readonly TimeSpan WsTimeout = TimeSpan.FromMinutes(3);

    private async Task<string> SendToAgentAsync(string serviceUrl, Guid agentId, string message, CancellationToken ct)
    {
        // serviceUrl is like "ws://zeroclaw-abc12345.default.svc.cluster.local:42617/ws/chat"
        var uri = new Uri(serviceUrl.TrimEnd('/'));
        var wsUri = new Uri($"{uri}?token={agentId}");

        using var ws = new ClientWebSocket();
        ws.Options.AddSubProtocol("zeroclaw.v1");

        using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        connectCts.CancelAfter(TimeSpan.FromSeconds(10));
        await ws.ConnectAsync(wsUri, connectCts.Token);

        // Send user_message
        var turnId = Guid.NewGuid().ToString("N");
        var outbound = JsonSerializer.Serialize(new
        {
            type = "user_message",
            text = message,
            id = turnId,
        });
        var sendBytes = Encoding.UTF8.GetBytes(outbound);
        await ws.SendAsync(sendBytes, WebSocketMessageType.Text, true, ct);

        // Collect assistant_delta chunks until turn_complete
        var responseBuilder = new StringBuilder();
        var buffer = new byte[8 * 1024];

        using var turnCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        turnCts.CancelAfter(WsTimeout);

        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(buffer, turnCts.Token);
            if (result.MessageType == WebSocketMessageType.Close)
                break;

            var frame = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var json = JsonSerializer.Deserialize<JsonElement>(frame);

            if (!json.TryGetProperty("type", out var typeProp))
                continue;

            var eventType = typeProp.GetString();
            switch (eventType)
            {
                case "assistant_delta":
                    if (json.TryGetProperty("text", out var text))
                        responseBuilder.Append(text.GetString());
                    break;

                case "turn_complete":
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
                    return responseBuilder.ToString();

                case "error":
                    var errorMsg = json.TryGetProperty("message", out var errText)
                        ? errText.GetString() ?? "Unknown agent error"
                        : "Unknown agent error";
                    throw new InvalidOperationException($"Agent error: {errorMsg}");
            }
        }

        return responseBuilder.ToString();
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

}

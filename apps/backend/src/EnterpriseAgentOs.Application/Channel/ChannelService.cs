namespace EnterpriseAgentOs.Application.Channel;

/// <summary>
/// Application-layer channel orchestration. Works exclusively with Domain
/// abstractions — no infrastructure dependencies.
/// </summary>
internal sealed class ChannelService : IChannelService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelGateway _gateway;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(
        IChannelRepository channelRepository,
        IChannelGateway gateway,
        ILogger<ChannelService> logger)
    {
        _channelRepository = channelRepository;
        _gateway = gateway;
        _logger = logger;
    }

    public async Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default)
    {
        var bindings = await _channelRepository.ListBindingsAsync(agentId, ct);

        foreach (var binding in bindings)
        {
            if (!binding.Enabled) continue;
            if (binding.ChannelConnection is null) continue;

            var destination = binding.LastChannelId ?? binding.LastSenderIdentifier;
            if (string.IsNullOrEmpty(destination))
                continue; // no inbound context yet — nowhere to send

            try
            {
                await _gateway.SendAsync(
                    binding.ChannelConnectionId,
                    binding.ChannelConnection.ChannelType,
                    destination,
                    text,
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to broadcast to {ChannelType} binding {BindingId} for agent {AgentId}",
                    binding.ChannelConnection.ChannelType, binding.Id, agentId);
            }
        }
    }

    public async Task SendTestMessageAsync(Guid connectionId, string destination, CancellationToken ct = default)
    {
        var connection = await _channelRepository.GetConnectionAsync(connectionId, ct);
        if (connection is null)
        {
            _logger.LogWarning("Cannot send test message — connection {Id} not found", connectionId);
            return;
        }

        var message = $"✅ {connection.DisplayName} connected successfully!\n\n"
                    + "This channel is now active and ready to receive messages from your agents.";

        try
        {
            await _gateway.SendAsync(connectionId, connection.ChannelType, destination, message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test message on {ChannelType} connection {Id}",
                connection.ChannelType, connectionId);
        }
    }
}

using MediatR;

namespace EnterpriseAgentOs.Application.Features.Channels;

/// <summary>
/// Thin orchestration layer. Backend owns connection metadata + bindings in its DB.
/// All platform-specific work (creds, send, webhooks) is delegated to the channel
/// microservice via IChannelGateway.
/// </summary>
internal sealed class ChannelService : IChannelService
{
    private readonly IChannelRepository _repo;
    private readonly IChannelGateway _gateway;
    private readonly IPublisher _publisher;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(
        IChannelRepository repo,
        IChannelGateway gateway,
        IPublisher publisher,
        ILogger<ChannelService> logger)
    {
        _repo = repo;
        _gateway = gateway;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<ChannelConnectionRecord> CreateConnectionAsync(
        string channelType, string displayName, string? configJson,
        Guid createdById, CancellationToken ct = default)
    {
        var record = ChannelConnectionRecord.Create(channelType, displayName, createdById);
        var created = await _repo.CreateConnectionAsync(record, ct);

        // Tell microservice to set up the platform connection
        await _gateway.StartConnectionAsync(created.Id, created.ChannelType, ct);

        // If config was provided (API tokens etc.), forward to microservice
        if (!string.IsNullOrWhiteSpace(configJson))
            await _gateway.SaveCredsAsync(created.Id, configJson, ct);

        return created;
    }

    public async Task<ChannelConnectionRecord> UpdateConnectionAsync(
        Guid id, string? displayName, bool? enabled, CancellationToken ct = default)
    {
        var updated = await _repo.UpdateConnectionAsync(id, row => row.ApplyUpdate(displayName, enabled), ct);
        return updated ?? throw new InvalidOperationException($"Channel connection '{id}' not found.");
    }

    public async Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _repo.GetConnectionAsync(id, ct);
        if (existing is not null)
            await _gateway.StopConnectionAsync(id, existing.ChannelType, ct);

        return await _repo.DeleteConnectionAsync(id, ct);
    }

    public async Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default)
    {
        var bindings = await _repo.ListBindingsAsync(agentId, ct);

        foreach (var binding in bindings)
        {
            if (!binding.Enabled || binding.ChannelConnection is null)
                continue;

            var channelType = binding.ChannelConnection.ChannelType;
            var correlationId = Guid.NewGuid().ToString("N");

            try
            {
                await _gateway.SendAsync(binding.ChannelConnectionId, text, ct: ct);

                await _publisher.Publish(new ChannelMessageRoutedEvent(
                    agentId, AgentLogType.ChannelOut, channelType, text, correlationId), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast failed for binding {BindingId}", binding.Id);

                await _publisher.Publish(new ChannelMessageRoutedEvent(
                    agentId, AgentLogType.Error, channelType,
                    $"Failed to deliver message via {channelType}: {ex.Message}", correlationId), ct);
            }
        }
    }

    public async Task SendTestMessageAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await _repo.GetConnectionAsync(connectionId, ct);
        if (connection is null) return;

        var message = $"✅ {connection.DisplayName} connected successfully!\n\n"
                    + "This channel is now active and ready to receive messages from your agents.";

        try
        {
            await _gateway.SendAsync(connectionId, message, ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test message failed on connection {Id}", connectionId);
        }
    }

    public async Task SaveChannelCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default)
    {
        // Forward creds to microservice — it owns all platform secrets
        await _gateway.SaveCredsAsync(connectionId, credsJson, ct);

        await _publisher.Publish(new ChannelCredsStoredEvent(connectionId), ct);
    }
}

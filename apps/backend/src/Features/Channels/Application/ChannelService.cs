namespace OffceOs.Application.Features.Channels;

internal sealed class ChannelService : IChannelService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelGateway _channelGateway;
    private readonly IAgentRepository _agentRepository;
    private readonly ChannelCredentialProtector _channelCredentialProtector;
    private readonly IPublisher _publisher;
    private readonly ChannelReplyContext _channelReplyContext;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(
        IChannelRepository repo,
        IChannelGateway gateway,
        IAgentRepository agents,
        ChannelCredentialProtector protector,
        IPublisher publisher,
        ChannelReplyContext replyContext,
        ILogger<ChannelService> logger)
    {
        _channelRepository = repo;
        _channelGateway = gateway;
        _agentRepository = agents;
        _channelCredentialProtector = protector;
        _publisher = publisher;
        _channelReplyContext = replyContext;
        _logger = logger;
    }

    public async Task<ChannelConnectionRecord> CreateConnectionAsync(
        string channelType, string displayName, string? configJson,
        Guid createdById, Guid workspaceId, CancellationToken ct = default)
    {
        var record = ChannelConnectionRecord.Create(channelType.ToChannelType(), displayName, createdById, workspaceId);

        if (!string.IsNullOrWhiteSpace(configJson))
            record.EncryptedCreds = _channelCredentialProtector.Protect(configJson);

        var created = await _channelRepository.CreateConnectionAsync(record, ct);

        if (!string.IsNullOrEmpty(created.EncryptedCreds) && created.Enabled)
            await _channelGateway.ReloadAsync(ct);

        return created;
    }

    public async Task<ChannelConnectionRecord> UpdateConnectionAsync(
        Guid id, string? displayName, bool? enabled, CancellationToken ct = default)
    {
        var updated = await _channelRepository.UpdateConnectionAsync(id, row => row.ApplyUpdate(displayName, enabled), ct);
        if (updated is null) throw new InvalidOperationException($"Channel connection '{id}' not found.");

        if (enabled.HasValue)
            await _channelGateway.ReloadAsync(ct);

        return updated;
    }

    public async Task<ChannelConnectionRecord> UpdateOwnedConnectionAsync(
        Guid id, Guid ownerId, Guid workspaceId, string? displayName, bool? enabled, CancellationToken ct = default)
    {
        await EnsureOwnedConnectionAsync(id, ownerId, workspaceId, ct);
        return await UpdateConnectionAsync(id, displayName, enabled, ct);
    }

    public async Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default)
    {
        var deleted = await _channelRepository.DeleteConnectionAsync(id, ct);
        if (deleted)
            await _channelGateway.ReloadAsync(ct);
        return deleted;
    }

    public async Task<bool> DeleteOwnedConnectionAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default)
    {
        await EnsureOwnedConnectionAsync(id, ownerId, workspaceId, ct);
        return await DeleteConnectionAsync(id, ct);
    }

    public async Task<IReadOnlyList<Guid>> RouteInboundAsync(
        Guid connectionId, string senderIdentifier, string messageText,
        bool isGroupMessage, string? messageId, string? channelId,
        CancellationToken ct = default)
    {
        var bindings = await _channelRepository.FindBindingsByConnectionAsync(connectionId, ct);
        var agentIds = new List<Guid>();

        // Log even when no agent bindings exist for this connection
        if (bindings.Count == 0)
        {
            var connection = await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter { Id = connectionId }, ct);
            var channelType = connection?.ChannelType.ToStorageString() ?? "unknown";

            await _publisher.Publish(new ChannelMessageRoutedEvent(
                null, AgentLogType.ChannelIn, channelType,
                messageText, Guid.NewGuid().ToString("N"), connectionId), ct);
        }

        foreach (var binding in bindings)
        {
            var channelType = binding.ChannelConnection?.ChannelType.ToStorageString() ?? "unknown";
            var correlationId = Guid.NewGuid().ToString("N");

            // Always log the inbound message, even if the binding is disabled
            await _publisher.Publish(new ChannelMessageRoutedEvent(
                binding.AgentId, AgentLogType.ChannelIn, channelType,
                messageText, correlationId, binding.ChannelConnectionId), ct);

            if (!binding.Enabled) continue;

            // Extract plain text from the Chat SDK JSON envelope
            var plainText = ExtractPlainText(messageText);

            // Stash the reply target so the outbound handler can deliver
            // the response back to the same conversation — no DB, pure in-memory
            if (!string.IsNullOrEmpty(channelId))
                _channelReplyContext.Set(correlationId, channelType, channelId, channelId, binding.ChannelConnectionId);

            await _publisher.Publish(new MessageReceivedEvent(
                binding.AgentId, plainText, correlationId), ct);

            agentIds.Add(binding.AgentId);
        }

        return agentIds;
    }

    public async Task<IReadOnlyList<Guid>> RouteInboundByChannelTypeAsync(
        string channelType, string senderIdentifier, string messageText,
        bool isGroupMessage, string? messageId, string? channelId,
        CancellationToken ct = default)
    {
        var connections = await _channelRepository.FindConnectionsByChannelTypeAsync(channelType, ct);
        if (connections.Count == 0)
        {
            await _publisher.Publish(new ChannelMessageRoutedEvent(
                null, AgentLogType.ChannelIn, channelType,
                messageText, Guid.NewGuid().ToString("N")), ct);
            return [];
        }

        var allAgentIds = new List<Guid>();
        foreach (var connection in connections)
        {
            var agentIds = await RouteInboundAsync(connection.Id, senderIdentifier, messageText,
                isGroupMessage, messageId, channelId, ct);
            allAgentIds.AddRange(agentIds);
        }
        return allAgentIds;
    }

    public async Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default)
    {
        var bindings = await _channelRepository.ListBindingsAsync(agentId, ct);

        foreach (var binding in bindings)
        {
            if (!binding.Enabled || binding.ChannelConnection is null)
                continue;

            var channelType = binding.ChannelConnection.ChannelType.ToStorageString();
            var correlationId = Guid.NewGuid().ToString("N");

            ChannelBindingConfig? config = null;
            if (!string.IsNullOrEmpty(binding.Config))
            {
                try { config = JsonSerializer.Deserialize<ChannelBindingConfig>(binding.Config); }
                catch { /* ignore malformed config */ }
            }

            var platformId = config?.PlatformId;
            var threadId = config?.ThreadId;

            if (string.IsNullOrEmpty(platformId))
            {
                _logger.LogWarning("Binding {BindingId} has no PlatformId configured, skipping", binding.Id);
                continue;
            }

            try
            {
                await _channelGateway.SendAsync(channelType, platformId, threadId,
                    ChannelMessage.Text(text), ct);

                await _publisher.Publish(new ChannelMessageRoutedEvent(
                    agentId, AgentLogType.ChannelOut, channelType, text, correlationId, binding.ChannelConnectionId), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast failed for binding {BindingId}", binding.Id);

                await _publisher.Publish(new ChannelMessageRoutedEvent(
                    agentId, AgentLogType.Error, channelType,
                    $"Failed to deliver message via {channelType}: {ex.Message}", correlationId, binding.ChannelConnectionId), ct);
            }
        }
    }

    public async Task SendTestMessageAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter { Id = connectionId }, ct);
        if (connection is null) return;

        var message = $"✅ {connection.DisplayName} connected successfully!";

        try
        {
            // Test message — no specific platformId, sidecar adapter handles default delivery
            await _channelGateway.SendAsync(connection.ChannelType.ToStorageString(), "default", null,
                ChannelMessage.Text(message), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test message failed on connection {Id}", connectionId);
        }
    }

    public async Task<AgentChannelBindingRecord> BindAgentAsync(Guid agentId, Guid channelConnectionId, string? configJson, CancellationToken ct = default)
    {
        var connection = await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter { Id = channelConnectionId }, ct);
        if (connection is null)
            throw new InvalidOperationException("Channel connection not found.");

        // Return existing binding if already bound (idempotent)
        var existing = await _channelRepository.ListBindingsAsync(agentId, ct);
        var match = existing.FirstOrDefault(b => b.ChannelConnectionId == channelConnectionId);
        if (match is not null)
            return match;

        var record = new AgentChannelBindingRecord
        {
            AgentId = agentId,
            ChannelConnectionId = channelConnectionId,
            Config = configJson,
        };

        return await _channelRepository.CreateBindingAsync(record, ct);
    }

    public async Task<bool> UnbindAgentAsync(Guid agentId, Guid channelConnectionId, CancellationToken ct = default)
    {
        var bindings = await _channelRepository.ListBindingsAsync(agentId, ct);
        var match = bindings.FirstOrDefault(b => b.ChannelConnectionId == channelConnectionId);
        if (match is null) return false;
        return await _channelRepository.DeleteBindingAsync(match.Id, ct);
    }

    public async Task<AgentChannelBindingRecord> UpdateBindingConfigAsync(Guid agentId, Guid channelConnectionId, string configJson, CancellationToken ct = default)
    {
        var bindings = await _channelRepository.ListBindingsAsync(agentId, ct);
        var match = bindings.FirstOrDefault(b => b.ChannelConnectionId == channelConnectionId);
        if (match is null)
            throw new InvalidOperationException("Binding not found for agent + channel connection.");

        var updated = await _channelRepository.UpdateBindingAsync(match.Id, row =>
        {
            if (row.AgentId != agentId) return;
            row.Config = configJson;
        }, ct);

        if (updated is null)
            throw new InvalidOperationException("Binding not found.");

        return updated;
    }

    public async Task SaveChannelCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default)
    {
        var updated = await _channelRepository.UpdateConnectionAsync(connectionId, record =>
        {
            record.EncryptedCreds = _channelCredentialProtector.Protect(credsJson);
        }, ct);

        if (updated is null)
            throw new InvalidOperationException($"Channel connection '{connectionId}' not found.");

        if (updated.Enabled)
            await _channelGateway.ReloadAsync(ct);

        await _publisher.Publish(new ChannelCredsStoredEvent(connectionId), ct);
    }

    public async Task<IReadOnlyList<AgentChannelBindingRecord>> ListBindingsForOwnedAgentAsync(
        Guid agentId,
        Guid ownerId,
        Guid? workspaceId = null,
        CancellationToken ct = default)
    {
        var agent = await _agentRepository.GetByAsync(new AgentFilter { Id = agentId, WorkspaceId = workspaceId }, ct);
        if (agent is null)
            throw new InvalidOperationException("Agent not found.");

        return await _channelRepository.ListBindingsAsync(agentId, ct);
    }

    private async Task EnsureOwnedConnectionAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct)
    {
        var connection = await _channelRepository.GetConnectionByAsync(new ChannelConnectionFilter
        {
            Id = id,
            WorkspaceId = workspaceId,
        }, ct);

        if (connection is null)
            throw new InvalidOperationException("Channel connection not found.");
    }

    /// <summary>
    /// Extract the plain text from a Chat SDK JSON message envelope.
    /// Falls back to the raw string if it's not valid JSON or has no "text" field.
    /// </summary>
    private static string ExtractPlainText(string messageText)
    {
        if (string.IsNullOrEmpty(messageText) || messageText[0] != '{')
            return messageText;

        using var doc = JsonDocument.Parse(messageText);
        if (doc.RootElement.TryGetProperty("text", out var textProp) &&
            textProp.ValueKind == JsonValueKind.String)
        {
            var text = textProp.GetString();
            if (!string.IsNullOrEmpty(text))
                return text;
        }

        return messageText;
    }
}

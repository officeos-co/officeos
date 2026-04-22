namespace EnterpriseAgentOs.Application.Channel;

/// <summary>
/// Application-layer channel orchestration. Works exclusively with Domain
/// abstractions — no infrastructure dependencies beyond the pragmatic
/// ChannelConfigProtector reference.
/// </summary>
internal sealed class ChannelService : IChannelService
{
    private readonly IChannelRepository _channelRepository;
    private readonly IChannelGateway _gateway;
    private readonly ChannelConfigProtector _configProtector;
    private readonly ILogger<ChannelService> _logger;

    public ChannelService(
        IChannelRepository channelRepository,
        IChannelGateway gateway,
        ChannelConfigProtector configProtector,
        ILogger<ChannelService> logger)
    {
        _channelRepository = channelRepository;
        _gateway = gateway;
        _configProtector = configProtector;
        _logger = logger;
    }

    // ── Connection lifecycle ─────────────────────────────────────────

    public async Task<ChannelConnectionRecord> CreateConnectionAsync(
        string channelType, string displayName, string? configJson,
        string? defaultChannelId, Guid createdById, CancellationToken ct = default)
    {
        if (ChannelTypes.GetByType(channelType) is null)
            throw new InvalidOperationException($"Unknown channel type: {channelType}");

        string? encrypted = null;
        if (!string.IsNullOrWhiteSpace(configJson) && configJson.Trim() != "{}")
            encrypted = _configProtector.Protect(configJson);

        var record = new ChannelConnectionRecord
        {
            ChannelType = channelType.ToLowerInvariant(),
            DisplayName = displayName,
            EncryptedConfig = encrypted,
            CreatedById = createdById,
        };

        var created = await _channelRepository.CreateConnectionAsync(record, ct);

        // Start platform-specific connection (e.g. WhatsApp QR pairing)
        await _gateway.StartConnectionAsync(created.Id, created.ChannelType, ct);

        // For webhook-based channels that are immediately connected, send test message
        if (!string.Equals(channelType, "whatsapp", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(defaultChannelId))
        {
            await SendTestMessageAsync(created.Id, defaultChannelId, ct);
        }

        return created;
    }

    public async Task<ChannelConnectionRecord> UpdateConnectionAsync(
        Guid id, string? displayName, bool? enabled, string? configJson, CancellationToken ct = default)
    {
        var updated = await _channelRepository.UpdateConnectionAsync(id, row =>
        {
            if (displayName is not null) row.DisplayName = displayName;
            if (enabled.HasValue) row.Enabled = enabled.Value;
            if (!string.IsNullOrWhiteSpace(configJson))
                row.EncryptedConfig = _configProtector.Protect(configJson);
        }, ct);

        if (updated is null)
            throw new InvalidOperationException($"Channel connection '{id}' not found.");

        return updated;
    }

    public async Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _channelRepository.GetConnectionAsync(id, ct);
        if (existing is not null)
            await _gateway.StopConnectionAsync(id, existing.ChannelType, ct);

        return await _channelRepository.DeleteConnectionAsync(id, ct);
    }

    // ── Broadcasting ────────────────────────────────────────────────

    public async Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default)
    {
        var bindings = await _channelRepository.ListBindingsAsync(agentId, ct);

        foreach (var binding in bindings)
        {
            if (!binding.Enabled) continue;
            if (binding.ChannelConnection is null) continue;

            var destination = binding.LastChannelId ?? binding.LastSenderIdentifier;
            if (string.IsNullOrEmpty(destination)) continue;

            try
            {
                await _gateway.SendAsync(
                    binding.ChannelConnectionId,
                    binding.ChannelConnection.ChannelType,
                    destination, text, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to broadcast to {ChannelType} binding {BindingId} for agent {AgentId}",
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

    // ── WhatsApp creds ──────────────────────────────────────────────

    public async Task SaveWhatsAppCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default)
    {
        var existing = await _channelRepository.GetConnectionAsync(connectionId, ct);
        if (existing is null) return;

        // Check if test message was already sent (stored in the config dict)
        var testMessageSent = false;
        if (!string.IsNullOrEmpty(existing.EncryptedConfig))
        {
            try
            {
                var existingJson = _configProtector.Unprotect(existing.EncryptedConfig);
                var existingDict = JsonSerializer.Deserialize<Dictionary<string, string>>(existingJson);
                testMessageSent = existingDict?.ContainsKey("testMessageSent") == true;
            }
            catch { /* corrupt config — will be overwritten */ }
        }

        // Persist creds
        var configDict = new Dictionary<string, string> { ["credsJson"] = credsJson };
        if (testMessageSent)
            configDict["testMessageSent"] = "true";

        var encrypted = _configProtector.Protect(JsonSerializer.Serialize(configDict));

        await _channelRepository.UpdateConnectionAsync(connectionId, row =>
        {
            row.EncryptedConfig = encrypted;
        }, ct);

        // On each creds save, if me.id is present and test message hasn't been sent yet, try
        if (!testMessageSent)
        {
            try
            {
                using var doc = JsonDocument.Parse(credsJson);
                if (doc.RootElement.TryGetProperty("me", out var me) &&
                    me.TryGetProperty("id", out var idProp))
                {
                    var ownerJid = idProp.GetString();
                    if (!string.IsNullOrEmpty(ownerJid))
                    {
                        _logger.LogInformation("Sending WhatsApp test message for {Id}, owner: {Jid}", connectionId, ownerJid);
                        await _gateway.SendAsync(connectionId, "whatsapp", ownerJid,
                            $"✅ Connected successfully!\n\nThis WhatsApp channel is now active and ready to receive messages from your agents.", ct);

                        // Mark as sent so we don't retry
                        configDict["testMessageSent"] = "true";
                        var updated = _configProtector.Protect(JsonSerializer.Serialize(configDict));
                        await _channelRepository.UpdateConnectionAsync(connectionId, row =>
                        {
                            row.EncryptedConfig = updated;
                        }, ct);
                    }
                }
            }
            catch (Exception ex)
            {
                // Will retry on next creds save when the connection is ready
                _logger.LogDebug(ex, "Test message not sent yet for WhatsApp connection {Id} (will retry)", connectionId);
            }
        }
    }

    public async Task<string?> LoadWhatsAppCredsAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await _channelRepository.GetConnectionAsync(connectionId, ct);
        if (connection is null || string.IsNullOrEmpty(connection.EncryptedConfig))
            return null;

        try
        {
            var json = _configProtector.Unprotect(connection.EncryptedConfig);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return dict?.GetValueOrDefault("credsJson");
        }
        catch
        {
            return null;
        }
    }
}

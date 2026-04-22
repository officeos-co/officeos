using System.Collections.Concurrent;

namespace EnterpriseAgentOs.Application.Channel;

/// <summary>
/// Application-layer channel orchestration. Coordinates domain models,
/// repository, gateway, and config protector — no business logic here,
/// only orchestration of domain operations.
/// </summary>
internal sealed class ChannelService : IChannelService
{
    private readonly IChannelRepository _repo;
    private readonly IChannelGateway _gateway;
    private readonly IChannelConfigProtector _protector;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChannelService> _logger;

    /// <summary>Track in-flight test message attempts to avoid concurrent fires per connection.</summary>
    private static readonly ConcurrentDictionary<Guid, byte> _pendingTestMessages = new();

    public ChannelService(
        IChannelRepository repo,
        IChannelGateway gateway,
        IChannelConfigProtector protector,
        IServiceScopeFactory scopeFactory,
        ILogger<ChannelService> logger)
    {
        _repo = repo;
        _gateway = gateway;
        _protector = protector;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // ── Connection lifecycle ─────────────────────────────────────────

    public async Task<ChannelConnectionRecord> CreateConnectionAsync(
        string channelType, string displayName, string? configJson,
        string? defaultChannelId, Guid createdById, CancellationToken ct = default)
    {
        var record = ChannelConnectionRecord.Create(channelType, displayName, createdById);

        if (!string.IsNullOrWhiteSpace(configJson) && configJson.Trim() != "{}")
            record.EncryptedConfig = _protector.Protect(configJson);

        var created = await _repo.CreateConnectionAsync(record, ct);

        await _gateway.StartConnectionAsync(created.Id, created.ChannelType, ct);

        // Webhook-based channels are immediately connected; WhatsApp sends
        // its test message later via SaveWhatsAppCredsAsync when the sidecar is ready.
        if (!created.IsWhatsApp && !string.IsNullOrEmpty(defaultChannelId))
            await SendTestMessageAsync(created.Id, defaultChannelId, ct);

        return created;
    }

    public async Task<ChannelConnectionRecord> UpdateConnectionAsync(
        Guid id, string? displayName, bool? enabled, string? configJson, CancellationToken ct = default)
    {
        var updated = await _repo.UpdateConnectionAsync(id, row =>
        {
            row.ApplyUpdate(displayName, enabled);
            if (!string.IsNullOrWhiteSpace(configJson))
                row.EncryptedConfig = _protector.Protect(configJson);
        }, ct);

        return updated ?? throw new InvalidOperationException($"Channel connection '{id}' not found.");
    }

    public async Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _repo.GetConnectionAsync(id, ct);
        if (existing is not null)
            await _gateway.StopConnectionAsync(id, existing.ChannelType, ct);

        return await _repo.DeleteConnectionAsync(id, ct);
    }

    // ── Broadcasting ────────────────────────────────────────────────

    public async Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default)
    {
        var bindings = await _repo.ListBindingsAsync(agentId, ct);

        foreach (var binding in bindings)
        {
            if (!binding.Enabled || binding.ChannelConnection is null || !binding.HasReplyContext)
                continue;

            try
            {
                await _gateway.SendAsync(
                    binding.ChannelConnectionId,
                    binding.ChannelConnection.ChannelType,
                    binding.ReplyDestination!,
                    text, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast failed for {ChannelType} binding {BindingId}",
                    binding.ChannelConnection.ChannelType, binding.Id);
            }
        }
    }

    public async Task SendTestMessageAsync(Guid connectionId, string destination, CancellationToken ct = default)
    {
        var connection = await _repo.GetConnectionAsync(connectionId, ct);
        if (connection is null) return;

        var message = $"✅ {connection.DisplayName} connected successfully!\n\n"
                    + "This channel is now active and ready to receive messages from your agents.";

        try
        {
            await _gateway.SendAsync(connectionId, connection.ChannelType, destination, message, ct);
            connection.MarkTestMessageSent();
            await _repo.UpdateConnectionAsync(connectionId, row => row.MarkTestMessageSent(), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test message failed on {ChannelType} connection {Id}",
                connection.ChannelType, connectionId);
        }
    }

    // ── WhatsApp creds ──────────────────────────────────────────────

    public async Task SaveWhatsAppCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default)
    {
        var connection = await _repo.GetConnectionAsync(connectionId, ct);
        if (connection is null) return;

        // Persist creds
        var configDict = new Dictionary<string, string> { ["credsJson"] = credsJson };
        await _repo.UpdateConnectionAsync(connectionId, row =>
        {
            row.EncryptedConfig = _protector.Protect(JsonSerializer.Serialize(configDict));
        }, ct);

        // Fire-and-forget with a new DI scope (the current request scope will be
        // disposed before the background task runs). Guard against concurrent fires.
        if (connection.NeedsTestMessage && _pendingTestMessages.TryAdd(connectionId, 0))
        {
            var ownerJid = ChannelConnectionRecord.ExtractOwnerIdentifier(credsJson);
            if (!string.IsNullOrEmpty(ownerJid))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5));
                        using var scope = _scopeFactory.CreateScope();
                        var scopedService = scope.ServiceProvider.GetRequiredService<IChannelService>();
                        _logger.LogInformation("Attempting WhatsApp test message for {Id}, owner: {Jid}", connectionId, ownerJid);
                        await scopedService.SendTestMessageAsync(connectionId, ownerJid, CancellationToken.None);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Background test message failed for {Id} (will retry on next creds save)", connectionId);
                    }
                    finally
                    {
                        _pendingTestMessages.TryRemove(connectionId, out _);
                    }
                });
            }
            else
            {
                _pendingTestMessages.TryRemove(connectionId, out _);
            }
        }
    }

    public async Task<string?> LoadWhatsAppCredsAsync(Guid connectionId, CancellationToken ct = default)
    {
        var connection = await _repo.GetConnectionAsync(connectionId, ct);
        if (connection is null || !connection.IsConfigured) return null;

        try
        {
            var json = _protector.Unprotect(connection.EncryptedConfig!);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return dict?.GetValueOrDefault("credsJson");
        }
        catch { return null; }
    }
}

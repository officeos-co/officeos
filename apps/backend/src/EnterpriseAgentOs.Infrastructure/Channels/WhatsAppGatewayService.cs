using System.Collections.Concurrent;
using System.Text.Json;
using BaileysCSharp.Core.Events;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.Sending.NonMedia;
using BaileysCSharp.Core.Sockets;
using BaileysCSharp.Core.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
namespace EnterpriseAgentOs.Infrastructure.Channels;

/// <summary>
/// Singleton background service that manages WhatsApp Web connections via BaileysCSharp.
/// One WASocket per ChannelConnectionRecord of type "whatsapp".
/// </summary>
public sealed class WhatsAppGatewayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WhatsAppSessionStore _sessionStore;
    private readonly ILogger<WhatsAppGatewayService> _logger;

    private readonly ConcurrentDictionary<Guid, WhatsAppConnection> _connections = new();
    private readonly ConcurrentDictionary<Guid, string> _pendingQrCodes = new();
    private readonly ConcurrentDictionary<Guid, string> _connectionStates = new();

    /// <summary>
    /// Fired when a connection's status changes.
    /// Args: connectionId, status ("qr"|"connecting"|"open"|"closed"|"error"), qrCodeBase64 (nullable)
    /// </summary>
    public event Action<Guid, string, string?>? ConnectionStatusChanged;

    public WhatsAppGatewayService(
        IServiceScopeFactory scopeFactory,
        WhatsAppSessionStore sessionStore,
        ILogger<WhatsAppGatewayService> logger)
    {
        _scopeFactory = scopeFactory;
        _sessionStore = sessionStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // On startup, reconnect all existing WhatsApp connections that have saved sessions
        await ReconnectExistingConnectionsAsync(stoppingToken);

        // Keep-alive loop: check for dropped connections every 30s
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            await ReconnectDroppedConnectionsAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Start a new WhatsApp connection for a channel. Called when a user creates
    /// a WhatsApp channel connection from the dashboard.
    /// </summary>
    public void StartConnection(Guid connectionId)
    {
        if (_connections.ContainsKey(connectionId))
        {
            _logger.LogWarning("WhatsApp connection {Id} already active", connectionId);
            return;
        }

        _ = Task.Run(() => ConnectAsync(connectionId));
    }

    /// <summary>
    /// Stop and remove a WhatsApp connection.
    /// </summary>
    public void StopConnection(Guid connectionId)
    {
        if (_connections.TryRemove(connectionId, out var conn))
        {
            conn.Dispose();
            _pendingQrCodes.TryRemove(connectionId, out _);
            _connectionStates.TryRemove(connectionId, out _);
            _logger.LogInformation("WhatsApp connection {Id} stopped", connectionId);
        }
    }

    /// <summary>
    /// Send a text message through an active WhatsApp connection.
    /// </summary>
    public async Task SendMessageAsync(Guid connectionId, string jid, string text)
    {
        if (!_connections.TryGetValue(connectionId, out var conn))
        {
            _logger.LogWarning("Cannot send WhatsApp message — connection {Id} not active", connectionId);
            return;
        }

        try
        {
            await conn.Socket.SendMessage(jid, new TextMessageContent { Text = text });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message on connection {Id}", connectionId);
        }
    }

    public string? GetPendingQrCode(Guid connectionId)
        => _pendingQrCodes.GetValueOrDefault(connectionId);

    public string GetConnectionState(Guid connectionId)
        => _connectionStates.GetValueOrDefault(connectionId) ?? "closed";

    private async Task ReconnectExistingConnectionsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
            var connections = await repo.ListConnectionsAsync(ct);

            foreach (var connection in connections.Where(c => c.ChannelType == "whatsapp" && c.Enabled))
            {
                if (_sessionStore.HasSession(connection))
                {
                    _logger.LogInformation("Reconnecting WhatsApp connection {Id}", connection.Id);
                    _ = Task.Run(() => ConnectAsync(connection.Id), ct);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconnect existing WhatsApp connections");
        }
    }

    private async Task ReconnectDroppedConnectionsAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
            var connections = await repo.ListConnectionsAsync(ct);

            foreach (var connection in connections.Where(c => c.ChannelType == "whatsapp" && c.Enabled))
            {
                if (_connections.ContainsKey(connection.Id)) continue;
                if (!_sessionStore.HasSession(connection)) continue;

                _logger.LogInformation("Reconnecting dropped WhatsApp connection {Id}", connection.Id);
                _ = Task.Run(() => ConnectAsync(connection.Id), ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed during reconnect check");
        }
    }

    private async Task ConnectAsync(Guid connectionId)
    {
        try
        {
            _connectionStates[connectionId] = "connecting";
            ConnectionStatusChanged?.Invoke(connectionId, "connecting", null);

            // Load connection record and prepare session directory
            ChannelConnectionRecord? record;
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
                record = await repo.GetConnectionAsync(connectionId);
                if (record is null) return;
            }

            var sessionDir = _sessionStore.GetOrCreateSessionDir(record);
            var authState = _sessionStore.CreateAuthState(sessionDir);

            var config = new SocketConfig
            {
                SessionName = sessionDir,
                Auth = authState,
            };
            config.Logger.Level = BaileysCSharp.Core.Logging.LogLevel.Fatal;

            var socket = new WASocket(config);
            var wrapper = new WhatsAppConnection(connectionId, socket, sessionDir);
            _connections[connectionId] = wrapper;

            // QR code + connection state
            socket.EV.Connection.Update += (sender, state) =>
            {
                if (state.QR is not null)
                {
                    _pendingQrCodes[connectionId] = state.QR;
                    _connectionStates[connectionId] = "qr";
                    ConnectionStatusChanged?.Invoke(connectionId, "qr", state.QR);
                    _logger.LogInformation("QR code generated for WhatsApp connection {Id}", connectionId);
                }

                if (state.Connection == WAConnectionState.Open)
                {
                    _pendingQrCodes.TryRemove(connectionId, out _);
                    _connectionStates[connectionId] = "open";
                    ConnectionStatusChanged?.Invoke(connectionId, "open", null);
                    _logger.LogInformation("WhatsApp connection {Id} is now open", connectionId);

                    // Persist session on successful connection
                    PersistSessionAsync(connectionId, sessionDir);
                }

                if (state.Connection == WAConnectionState.Close)
                {
                    _connectionStates[connectionId] = "closed";
                    ConnectionStatusChanged?.Invoke(connectionId, "closed", null);
                    _connections.TryRemove(connectionId, out _);
                    _logger.LogWarning("WhatsApp connection {Id} closed", connectionId);
                }
            };

            // Persist credentials on auth updates
            socket.EV.Auth.Update += (sender, creds) =>
            {
                PersistSessionAsync(connectionId, sessionDir);
            };

            // Route inbound messages to agents
            socket.EV.Message.Upsert += (sender, msgEvent) =>
            {
                _ = Task.Run(async () =>
                {
                    foreach (var msg in msgEvent.Messages)
                    {
                        if (msg.Key?.FromMe == true) continue;

                        var text = msg.Message?.Conversation
                            ?? msg.Message?.ExtendedTextMessage?.Text;
                        if (string.IsNullOrEmpty(text)) continue;

                        var senderJid = msg.Key?.RemoteJid ?? "";
                        if (string.IsNullOrEmpty(senderJid)) continue;

                        _logger.LogDebug("WhatsApp message from {Jid} on connection {Id}", senderJid, connectionId);

                        try
                        {
                            using var msgScope = _scopeFactory.CreateScope();
                            var router = msgScope.ServiceProvider.GetRequiredService<ChannelMessageRouter>();
                            var responses = await router.RouteMessageAsync(connectionId, senderJid, text);

                            foreach (var (_, responseText) in responses)
                            {
                                if (!string.IsNullOrEmpty(responseText))
                                    await SendMessageAsync(connectionId, senderJid, responseText);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to route WhatsApp message from {Jid}", senderJid);
                        }
                    }
                });
            };

            // Start the connection
            socket.MakeSocket();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start WhatsApp connection {Id}", connectionId);
            _connectionStates[connectionId] = "error";
            ConnectionStatusChanged?.Invoke(connectionId, "error", ex.Message);
            _connections.TryRemove(connectionId, out _);
        }
    }

    private void PersistSessionAsync(Guid connectionId, string sessionDir)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var encrypted = _sessionStore.PersistSession(sessionDir);
                if (encrypted is null) return;

                using var persistScope = _scopeFactory.CreateScope();
                var repo = persistScope.ServiceProvider.GetRequiredService<IChannelRepository>();
                await repo.UpdateConnectionAsync(connectionId, row =>
                {
                    row.EncryptedConfig = encrypted;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist WhatsApp session for {Id}", connectionId);
            }
        });
    }

    public override void Dispose()
    {
        foreach (var conn in _connections.Values)
            conn.Dispose();
        _connections.Clear();
        base.Dispose();
    }
}

public sealed class WhatsAppConnection : IDisposable
{
    public Guid ConnectionId { get; }
    public WASocket Socket { get; }
    public string SessionDir { get; }

    public WhatsAppConnection(Guid connectionId, WASocket socket, string sessionDir)
    {
        ConnectionId = connectionId;
        Socket = socket;
        SessionDir = sessionDir;
    }

    public void Dispose()
    {
        try { Socket.Dispose(); } catch { }
    }
}

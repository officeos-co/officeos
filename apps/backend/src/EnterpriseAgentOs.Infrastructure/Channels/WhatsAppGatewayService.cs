using System.Collections.Concurrent;
using System.Text.Json;
using BaileysCSharp.Core.Events;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Models.Sending.NonMedia;
using BaileysCSharp.Core.Sockets;
using BaileysCSharp.Core.Types;
using Proto;
using EnterpriseAgentOs.Infrastructure.Channels.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Infrastructure.Channels;

public sealed class WhatsAppGatewayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WhatsAppSessionStore _sessionStore;
    private readonly InMemoryMessageDeduplicator _deduplicator;
    private readonly ILogger<WhatsAppGatewayService> _logger;

    private readonly ConcurrentDictionary<Guid, WhatsAppConnection> _connections = new();
    private readonly ConcurrentDictionary<Guid, string> _pendingQrCodes = new();
    private readonly ConcurrentDictionary<Guid, string> _connectionStates = new();
    private readonly ConcurrentDictionary<Guid, int> _retryCount = new();
    private readonly ConcurrentDictionary<string, DebounceState> _debounce = new();

    public event Action<Guid, string, string?>? ConnectionStatusChanged;

    public WhatsAppGatewayService(
        IServiceScopeFactory scopeFactory,
        WhatsAppSessionStore sessionStore,
        InMemoryMessageDeduplicator deduplicator,
        ILogger<WhatsAppGatewayService> logger)
    {
        _scopeFactory = scopeFactory;
        _sessionStore = sessionStore;
        _deduplicator = deduplicator;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ReconnectExistingConnectionsAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            await ReconnectDroppedConnectionsAsync(stoppingToken);
        }
    }

    public void StartConnection(Guid connectionId)
    {
        if (_connections.ContainsKey(connectionId))
        {
            _logger.LogWarning("WhatsApp connection {Id} already active", connectionId);
            return;
        }

        _retryCount[connectionId] = 0;
        _ = Task.Run(() => ConnectAsync(connectionId));
    }

    public void StopConnection(Guid connectionId)
    {
        if (_connections.TryRemove(connectionId, out var conn))
        {
            conn.Dispose();
            _pendingQrCodes.TryRemove(connectionId, out _);
            _connectionStates.TryRemove(connectionId, out _);
            _retryCount.TryRemove(connectionId, out _);
            _logger.LogInformation("WhatsApp connection {Id} stopped", connectionId);
        }
    }

    public async Task SendMessageAsync(Guid connectionId, string jid, string text)
    {
        if (!_connections.TryGetValue(connectionId, out var conn))
        {
            _logger.LogWarning("Cannot send WhatsApp message — connection {Id} not active", connectionId);
            return;
        }

        try
        {
            var converted = MarkdownFormatConverter.Convert(text, "whatsapp");
            var chunks = PlatformTextChunker.ChunkForPlatform(converted, "whatsapp");

            foreach (var chunk in chunks)
            {
                await conn.Socket.SendMessage(jid, new TextMessageContent { Text = chunk });
            }
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

                // Exponential backoff: 5s, 10s, 20s, 40s, 60s cap
                var retries = _retryCount.GetValueOrDefault(connection.Id, 0);
                var baseDelay = Math.Min(60, 5 * Math.Pow(2, retries));
                // Add jitter ±20%
                var jitter = baseDelay * 0.2 * (Random.Shared.NextDouble() * 2 - 1);
                var delay = TimeSpan.FromSeconds(baseDelay + jitter);

                _logger.LogInformation("Reconnecting dropped WhatsApp connection {Id} (retry {Retry}, delay {Delay}s)",
                    connection.Id, retries, delay.TotalSeconds);

                _retryCount.AddOrUpdate(connection.Id, 1, (_, v) => v + 1);

                await Task.Delay(delay, ct);
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

            socket.EV.Connection.Update += (sender, state) =>
            {
                if (state.QR is not null)
                {
                    _pendingQrCodes[connectionId] = state.QR;
                    _connectionStates[connectionId] = "qr";
                    ConnectionStatusChanged?.Invoke(connectionId, "qr", state.QR);
                }

                if (state.Connection == WAConnectionState.Open)
                {
                    _pendingQrCodes.TryRemove(connectionId, out _);
                    _connectionStates[connectionId] = "open";
                    _retryCount[connectionId] = 0;
                    ConnectionStatusChanged?.Invoke(connectionId, "open", null);
                    _logger.LogInformation("WhatsApp connection {Id} is now open", connectionId);
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

            socket.EV.Auth.Update += (sender, creds) =>
            {
                PersistSessionAsync(connectionId, sessionDir);
            };

            socket.EV.Message.Upsert += (sender, msgEvent) =>
            {
                _ = Task.Run(async () =>
                {
                    foreach (var msg in msgEvent.Messages)
                    {
                        try
                        {
                            await HandleInboundMessageAsync(connectionId, msg);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to handle WhatsApp message on connection {Id}", connectionId);
                        }
                    }
                });
            };

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

    private async Task HandleInboundMessageAsync(Guid connectionId, WebMessageInfo msg)
    {
        // Skip own messages
        if (msg.Key?.FromMe == true) return;

        var senderJid = msg.Key?.RemoteJid ?? "";
        if (string.IsNullOrEmpty(senderJid)) return;

        // Filter status broadcasts
        if (senderJid == "status@broadcast") return;

        // Deduplication
        var msgId = msg.Key?.Id ?? "";
        if (!string.IsNullOrEmpty(msgId) && _deduplicator.IsDuplicate("whatsapp", msgId)) return;

        // Extract text
        var text = msg.Message?.Conversation
            ?? msg.Message?.ExtendedTextMessage?.Text
            ?? "";

        // Extract media info
        List<ChannelMediaAttachment>? media = null;

        if (msg.Message?.ImageMessage is { } img)
        {
            media ??= new();
            media.Add(new ChannelMediaAttachment("image", img.Url, img.Mimetype, null));
            if (string.IsNullOrEmpty(text)) text = img.Caption ?? "[image]";
        }

        if (msg.Message?.VideoMessage is { } vid)
        {
            media ??= new();
            media.Add(new ChannelMediaAttachment("video", vid.Url, vid.Mimetype, null));
            if (string.IsNullOrEmpty(text)) text = vid.Caption ?? "[video]";
        }

        if (msg.Message?.AudioMessage is { } aud)
        {
            media ??= new();
            media.Add(new ChannelMediaAttachment("audio", aud.Url, aud.Mimetype, null));
            if (string.IsNullOrEmpty(text)) text = "[audio]";
        }

        if (msg.Message?.DocumentMessage is { } doc)
        {
            media ??= new();
            media.Add(new ChannelMediaAttachment("document", doc.Url, doc.Mimetype, doc.FileName));
            if (string.IsNullOrEmpty(text)) text = doc.FileName ?? "[document]";
        }

        if (string.IsNullOrEmpty(text) && (media is null || media.Count == 0)) return;

        // Group vs DM detection
        var isGroup = senderJid.EndsWith("@g.us");
        var participantJid = isGroup ? msg.Key?.Participant : null;
        var actualSender = isGroup ? (participantJid ?? senderJid) : senderJid;

        _logger.LogDebug("WhatsApp message from {Sender} on connection {Id} (group: {IsGroup})",
            actualSender, connectionId, isGroup);

        // Debounce: buffer rapid messages from same sender
        var debounceKey = $"{connectionId}:{actualSender}";
        var state = _debounce.GetOrAdd(debounceKey, _ => new DebounceState());

        lock (state)
        {
            state.Messages.Add(text);
            state.ConnectionId = connectionId;
            state.SenderJid = senderJid;
            state.ActualSender = actualSender;
            state.IsGroup = isGroup;
            state.ParticipantJid = participantJid;
            state.Media ??= new();
            if (media is not null) state.Media.AddRange(media);

            state.Timer?.Dispose();
            state.Timer = new Timer(_ => _ = FlushDebounceAsync(debounceKey), null,
                TimeSpan.FromMilliseconds(1500), Timeout.InfiniteTimeSpan);
        }
    }

    private async Task FlushDebounceAsync(string debounceKey)
    {
        if (!_debounce.TryRemove(debounceKey, out var state)) return;

        string combinedText;
        Guid connectionId;
        string senderJid;
        List<ChannelMediaAttachment>? media;

        lock (state)
        {
            state.Timer?.Dispose();
            combinedText = string.Join("\n", state.Messages);
            connectionId = state.ConnectionId;
            senderJid = state.SenderJid;
            media = state.Media?.Count > 0 ? state.Media : null;
        }

        try
        {
            using var msgScope = _scopeFactory.CreateScope();
            var router = msgScope.ServiceProvider.GetRequiredService<ChannelMessageRouter>();
            var responses = await router.RouteMessageAsync(connectionId, state.ActualSender, combinedText);

            foreach (var (_, responseText) in responses)
            {
                if (!string.IsNullOrEmpty(responseText))
                    await SendMessageAsync(connectionId, senderJid, responseText);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to route debounced WhatsApp message from {Sender}", state.ActualSender);
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
        foreach (var state in _debounce.Values)
            state.Timer?.Dispose();
        _debounce.Clear();

        foreach (var conn in _connections.Values)
            conn.Dispose();
        _connections.Clear();
        base.Dispose();
    }
}

internal sealed class DebounceState
{
    public Timer? Timer;
    public List<string> Messages = new();
    public Guid ConnectionId;
    public string SenderJid = "";
    public string ActualSender = "";
    public bool IsGroup;
    public string? ParticipantJid;
    public List<ChannelMediaAttachment>? Media;
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

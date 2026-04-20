# WhatsApp Web (BaileysCSharp) Integration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the WhatsApp Business API adapter with BaileysCSharp (WhatsApp Web multi-device) so users connect by scanning a QR code instead of entering Meta API tokens.

**Architecture:** BaileysCSharp is added as a git submodule under `apps/backend/lib/BaileysCSharp/`. A new `WhatsAppGatewayService` (singleton background service) manages persistent WASocket connections per ChannelConnectionRecord. The dashboard onboarding dialog shows a live QR code via a new GraphQL subscription. The old webhook-based WhatsAppAdapter is deleted.

**Tech Stack:** BaileysCSharp (WhiskeySockets), QRCoder NuGet, ASP.NET Core BackgroundService, HotChocolate GraphQL subscriptions, React + Apollo subscriptions

---

## File Map

### Backend — New Files
| File | Purpose |
|------|---------|
| `apps/backend/lib/BaileysCSharp/` | Git submodule of WhiskeySockets/BaileysCSharp |
| `src/EnterpriseAgentOs.Infrastructure/Adapters/Channels/WhatsApp/WhatsAppGatewayService.cs` | Singleton BackgroundService — owns WASocket instances, QR generation, reconnect loop, message routing |
| `src/EnterpriseAgentOs.Infrastructure/Adapters/Channels/WhatsApp/WhatsAppSessionStore.cs` | Persists Baileys auth credentials to ChannelConnectionRecord.EncryptedConfig |
| `src/EnterpriseAgentOs.Api/GraphQL/Subscriptions/WhatsAppSubscriptions.cs` | `whatsAppConnectionStatus` subscription — streams QR codes and connection state to dashboard |
| `src/EnterpriseAgentOs.Api/GraphQL/Types/WhatsAppTypes.cs` | GraphQL types: WhatsAppConnectionStatus, WhatsAppQrPayload |

### Backend — Modified Files
| File | Change |
|------|--------|
| `src/EnterpriseAgentOs.Infrastructure/EnterpriseAgentOs.Infrastructure.csproj` | Add ProjectReference to BaileysCSharp + QRCoder NuGet |
| `src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs` | Remove WhatsAppAdapter singleton, add WhatsAppGatewayService hosted service |
| `src/EnterpriseAgentOs.Application/DTOs/Channels/ChannelDto.cs` | Update WhatsApp ChannelTypeDefinition — remove config fields, add QR onboarding step, update description |
| `src/EnterpriseAgentOs.Api/GraphQL/Mutations/ChannelMutations.cs` | In CreateChannelConnection: if whatsapp, trigger gateway to start socket + QR generation |
| `src/EnterpriseAgentOs.Api/Controllers/ChannelWebhooksController.cs` | Remove WhatsApp-specific GET verification endpoint logic (no longer needed) |

### Backend — Deleted Files
| File | Reason |
|------|--------|
| `src/EnterpriseAgentOs.Infrastructure/Adapters/Channels/Adapters/WhatsAppAdapter.cs` | Replaced by WhatsAppGatewayService |

### Dashboard — Modified Files
| File | Change |
|------|--------|
| `src/features/agents/components/channel-onboarding-dialog.tsx` | Add WhatsApp-specific QR code flow with live subscription |
| `src/features/agents/api/useChannels.ts` | Add `useWhatsAppConnectionStatus` subscription hook |

---

## Task 1: Add BaileysCSharp as Git Submodule

**Files:**
- Create: `apps/backend/lib/BaileysCSharp/` (submodule)
- Modify: `apps/backend/src/EnterpriseAgentOs.Infrastructure/EnterpriseAgentOs.Infrastructure.csproj`

- [ ] **Step 1: Add the git submodule**

```bash
cd /Users/harrokrog/Desktop/EnterpriseAgentOs
git submodule add https://github.com/WhiskeySockets/BaileysCSharp.git apps/backend/lib/BaileysCSharp
```

- [ ] **Step 2: Add ProjectReference + QRCoder to Infrastructure csproj**

In `apps/backend/src/EnterpriseAgentOs.Infrastructure/EnterpriseAgentOs.Infrastructure.csproj`, add inside the `<ItemGroup>` with ProjectReferences:

```xml
<ProjectReference Include="..\..\lib\BaileysCSharp\BaileysCSharp\BaileysCSharp.csproj" />
```

Add inside the `<ItemGroup>` with PackageReferences:

```xml
<PackageReference Include="QRCoder" Version="1.6.0" />
```

- [ ] **Step 3: Verify the solution builds**

```bash
cd apps/backend && dotnet build EnterpriseAgentOs.sln
```

Expected: Build succeeds. BaileysCSharp targets net8.0 which is compatible with the net9.0 host.

- [ ] **Step 4: Commit**

```bash
git add .gitmodules apps/backend/lib/BaileysCSharp apps/backend/src/EnterpriseAgentOs.Infrastructure/EnterpriseAgentOs.Infrastructure.csproj
git commit -m "feat: add BaileysCSharp submodule and QRCoder for WhatsApp Web integration"
```

---

## Task 2: WhatsApp Session Store

Persists BaileysCSharp auth state (creds.json) to the database via ChannelConnectionRecord.EncryptedConfig.

**Files:**
- Create: `src/EnterpriseAgentOs.Infrastructure/Adapters/Channels/WhatsApp/WhatsAppSessionStore.cs`

- [ ] **Step 1: Create the session store**

```csharp
using System.Text.Json;
using BaileysCSharp.Core.NoSQL;
using BaileysCSharp.Core.Signal;
using EnterpriseAgentOs.Domain.Models;
using EnterpriseAgentOs.Infrastructure.Security;
using Microsoft.Extensions.Logging;

namespace EnterpriseAgentOs.Infrastructure.Adapters.Channels.WhatsApp;

/// <summary>
/// Adapts BaileysCSharp's file-based credential storage to our encrypted
/// database-backed ChannelConnectionRecord.EncryptedConfig.
/// </summary>
public sealed class WhatsAppSessionStore
{
    private readonly ChannelConfigProtector _protector;
    private readonly ILogger<WhatsAppSessionStore> _logger;

    public WhatsAppSessionStore(ChannelConfigProtector protector, ILogger<WhatsAppSessionStore> logger)
    {
        _protector = protector;
        _logger = logger;
    }

    /// <summary>
    /// Load credentials from the connection record's encrypted config.
    /// Returns null if no session exists yet (first-time pairing).
    /// </summary>
    public string? LoadCredsJson(ChannelConnectionRecord connection)
    {
        if (string.IsNullOrEmpty(connection.EncryptedConfig))
            return null;

        try
        {
            var json = _protector.Unprotect(connection.EncryptedConfig);
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return dict?.GetValueOrDefault("credsJson");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load WhatsApp session for connection {Id}", connection.Id);
            return null;
        }
    }

    /// <summary>
    /// Persist the Baileys creds.json content to the connection record.
    /// Returns the encrypted string to be saved to EncryptedConfig.
    /// </summary>
    public string SaveCredsJson(string credsJson)
    {
        var dict = new Dictionary<string, string> { ["credsJson"] = credsJson };
        var json = JsonSerializer.Serialize(dict);
        return _protector.Protect(json);
    }
}
```

- [ ] **Step 2: Verify build**

```bash
cd apps/backend && dotnet build EnterpriseAgentOs.sln
```

- [ ] **Step 3: Commit**

```bash
cd apps/backend
git add src/EnterpriseAgentOs.Infrastructure/Adapters/Channels/WhatsApp/WhatsAppSessionStore.cs
git commit -m "feat: add WhatsAppSessionStore for encrypted credential persistence"
```

---

## Task 3: WhatsApp Gateway Service

The core background service that manages WASocket connections, QR code generation, reconnection, inbound message routing, and outbound sending.

**Files:**
- Create: `src/EnterpriseAgentOs.Infrastructure/Adapters/Channels/WhatsApp/WhatsAppGatewayService.cs`

- [ ] **Step 1: Create the gateway service**

```csharp
using System.Collections.Concurrent;
using System.Text.Json;
using BaileysCSharp.Core.Models;
using BaileysCSharp.Core.Sockets;
using EnterpriseAgentOs.Domain.Interfaces.Channels;
using EnterpriseAgentOs.Domain.Models;
using EnterpriseAgentOs.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QRCoder;

namespace EnterpriseAgentOs.Infrastructure.Adapters.Channels.WhatsApp;

/// <summary>
/// Singleton background service that manages WhatsApp Web connections via BaileysCSharp.
/// One WASocket per ChannelConnectionRecord of type "whatsapp".
/// </summary>
public sealed class WhatsAppGatewayService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly WhatsAppSessionStore _sessionStore;
    private readonly ILogger<WhatsAppGatewayService> _logger;

    // connectionId → active socket wrapper
    private readonly ConcurrentDictionary<Guid, WhatsAppConnection> _connections = new();

    // connectionId → latest QR base64 PNG (consumed by GraphQL subscription)
    private readonly ConcurrentDictionary<Guid, string> _pendingQrCodes = new();

    // connectionId → connection state ("qr", "connecting", "open", "closed")
    private readonly ConcurrentDictionary<Guid, string> _connectionStates = new();

    // Event for subscription push
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
        // On startup, reconnect all existing WhatsApp connections
        await ReconnectExistingConnectionsAsync(stoppingToken);

        // Keep alive — reconnect loop
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            await ReconnectDroppedConnectionsAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Start a new WhatsApp connection for a channel. Called when user creates
    /// a WhatsApp channel connection from the dashboard.
    /// </summary>
    public void StartConnection(Guid connectionId)
    {
        if (_connections.ContainsKey(connectionId))
        {
            _logger.LogWarning("WhatsApp connection {Id} already active", connectionId);
            return;
        }

        _ = Task.Run(() => ConnectAsync(connectionId, CancellationToken.None));
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
        if (!_connections.TryGetValue(connectionId, out var conn) || conn.Socket is null)
        {
            _logger.LogWarning("Cannot send WhatsApp message — connection {Id} not active", connectionId);
            return;
        }

        try
        {
            await conn.Socket.SendMessage(jid, new BaileysCSharp.Core.Types.TextMessageContent { Text = text });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message on connection {Id}", connectionId);
        }
    }

    /// <summary>
    /// Get the current QR code (base64 PNG) for a connection that is pairing.
    /// Returns null if no QR is pending (already connected or not started).
    /// </summary>
    public string? GetPendingQrCode(Guid connectionId)
        => _pendingQrCodes.GetValueOrDefault(connectionId);

    /// <summary>
    /// Get the current connection state for a connection.
    /// </summary>
    public string GetConnectionState(Guid connectionId)
        => _connectionStates.GetValueOrDefault(connectionId) ?? "closed";

    private async Task ReconnectExistingConnectionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
        var connections = await repo.ListConnectionsAsync(ct);
        var whatsappConnections = connections
            .Where(c => c.ChannelType == "whatsapp" && c.Enabled)
            .ToList();

        _logger.LogInformation("Reconnecting {Count} existing WhatsApp connections", whatsappConnections.Count);

        foreach (var connection in whatsappConnections)
        {
            // Only reconnect if we have saved credentials
            var credsJson = _sessionStore.LoadCredsJson(connection);
            if (credsJson is not null)
            {
                _ = Task.Run(() => ConnectAsync(connection.Id, ct), ct);
            }
        }
    }

    private async Task ReconnectDroppedConnectionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
        var connections = await repo.ListConnectionsAsync(ct);
        var whatsappConnections = connections
            .Where(c => c.ChannelType == "whatsapp" && c.Enabled)
            .ToList();

        foreach (var connection in whatsappConnections)
        {
            if (_connections.ContainsKey(connection.Id)) continue;

            var credsJson = _sessionStore.LoadCredsJson(connection);
            if (credsJson is not null)
            {
                _logger.LogInformation("Reconnecting dropped WhatsApp connection {Id}", connection.Id);
                _ = Task.Run(() => ConnectAsync(connection.Id, ct), ct);
            }
        }
    }

    private async Task ConnectAsync(Guid connectionId, CancellationToken ct)
    {
        try
        {
            _connectionStates[connectionId] = "connecting";
            ConnectionStatusChanged?.Invoke(connectionId, "connecting", null);

            // Load existing credentials if any
            string? credsJson = null;
            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
                var record = await repo.GetConnectionAsync(connectionId, ct);
                if (record is null) return;
                credsJson = _sessionStore.LoadCredsJson(record);
            }

            // Create session directory for Baileys
            var sessionDir = Path.Combine(Path.GetTempPath(), "eaos-whatsapp", connectionId.ToString("N"));
            Directory.CreateDirectory(sessionDir);

            // Write existing creds if we have them
            if (credsJson is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(sessionDir, "creds.json"), credsJson, ct);
            }

            var config = new SocketConfig
            {
                SessionName = sessionDir
            };

            var socket = new WASocket(config);
            var wrapper = new WhatsAppConnection(connectionId, socket);
            _connections[connectionId] = wrapper;

            // Handle QR code
            socket.EV.Connection.Update += (sender, state) =>
            {
                if (state.QR is not null)
                {
                    // Generate QR code as base64 PNG
                    using var qrGenerator = new QRCodeGenerator();
                    var qrData = qrGenerator.CreateQrCode(state.QR, QRCodeGenerator.ECCLevel.M);
                    using var qrCode = new PngByteQRCode(qrData);
                    var pngBytes = qrCode.GetGraphic(10);
                    var base64 = Convert.ToBase64String(pngBytes);

                    _pendingQrCodes[connectionId] = base64;
                    _connectionStates[connectionId] = "qr";
                    ConnectionStatusChanged?.Invoke(connectionId, "qr", base64);
                    _logger.LogInformation("QR code generated for WhatsApp connection {Id}", connectionId);
                }

                if (state.Connection == WAConnectionState.Open)
                {
                    _pendingQrCodes.TryRemove(connectionId, out _);
                    _connectionStates[connectionId] = "open";
                    ConnectionStatusChanged?.Invoke(connectionId, "open", null);
                    _logger.LogInformation("WhatsApp connection {Id} is now open", connectionId);
                }

                if (state.Connection == WAConnectionState.Close)
                {
                    _connectionStates[connectionId] = "closed";
                    ConnectionStatusChanged?.Invoke(connectionId, "closed", null);
                    _connections.TryRemove(connectionId, out _);
                    _logger.LogWarning("WhatsApp connection {Id} closed", connectionId);
                }
            };

            // Handle auth credential updates — persist to DB
            socket.EV.Auth.Update += async (sender, args) =>
            {
                try
                {
                    var credsPath = Path.Combine(sessionDir, "creds.json");
                    if (File.Exists(credsPath))
                    {
                        var updatedCreds = await File.ReadAllTextAsync(credsPath);
                        var encrypted = _sessionStore.SaveCredsJson(updatedCreds);

                        using var scope = _scopeFactory.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<IChannelRepository>();
                        await repo.UpdateConnectionAsync(connectionId, row =>
                        {
                            row.EncryptedConfig = encrypted;
                        }, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to persist WhatsApp credentials for {Id}", connectionId);
                }
            };

            // Handle inbound messages
            socket.EV.Message.Upsert += async (sender, args) =>
            {
                foreach (var msg in args.Messages)
                {
                    // Skip outgoing messages and non-text messages
                    if (msg.Key?.FromMe == true) continue;

                    var text = msg.Message?.Conversation
                        ?? msg.Message?.ExtendedTextMessage?.Text;

                    if (string.IsNullOrEmpty(text)) continue;

                    var senderJid = msg.Key?.RemoteJid ?? "";
                    if (string.IsNullOrEmpty(senderJid)) continue;

                    _logger.LogDebug("WhatsApp message from {Jid}: {Text}", senderJid, text);

                    try
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var router = scope.ServiceProvider.GetRequiredService<ChannelMessageRouter>();
                        var responses = await router.RouteMessageAsync(connectionId, senderJid, text, CancellationToken.None);

                        foreach (var (_, responseText) in responses)
                        {
                            if (!string.IsNullOrEmpty(responseText))
                            {
                                await SendMessageAsync(connectionId, senderJid, responseText);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to route WhatsApp message from {Jid}", senderJid);
                    }
                }
            };

            // Start the socket
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

    public override void Dispose()
    {
        foreach (var conn in _connections.Values)
            conn.Dispose();
        _connections.Clear();
        base.Dispose();
    }
}

/// <summary>
/// Wraps a WASocket instance with its connection ID for lifecycle management.
/// </summary>
internal sealed class WhatsAppConnection : IDisposable
{
    public Guid ConnectionId { get; }
    public WASocket Socket { get; }

    public WhatsAppConnection(Guid connectionId, WASocket socket)
    {
        ConnectionId = connectionId;
        Socket = socket;
    }

    public void Dispose()
    {
        try { Socket.EndConnection(false); } catch { }
    }
}
```

Note: The exact BaileysCSharp event API may differ slightly from what's shown here. The event handler signatures (`Connection.Update`, `Auth.Update`, `Message.Upsert`) follow the patterns found in the WhatsSocketConsole example project. After Task 1 (adding the submodule), inspect the actual event types in `BaileysCSharp/Core/Events/` and adjust handler signatures if needed.

- [ ] **Step 2: Verify build**

```bash
cd apps/backend && dotnet build EnterpriseAgentOs.sln
```

- [ ] **Step 3: Commit**

```bash
cd apps/backend
git add src/EnterpriseAgentOs.Infrastructure/Adapters/Channels/WhatsApp/
git commit -m "feat: add WhatsAppGatewayService — persistent WASocket connections with QR pairing"
```

---

## Task 4: GraphQL Subscription + Types for WhatsApp Status

**Files:**
- Create: `src/EnterpriseAgentOs.Api/GraphQL/Types/WhatsAppTypes.cs`
- Create: `src/EnterpriseAgentOs.Api/GraphQL/Subscriptions/WhatsAppSubscriptions.cs`

- [ ] **Step 1: Create WhatsApp GraphQL types**

In `src/EnterpriseAgentOs.Api/GraphQL/Types/WhatsAppTypes.cs`:

```csharp
namespace EnterpriseAgentOs.Api.GraphQL.Types;

public sealed record WhatsAppConnectionStatusPayload(
    Guid ConnectionId,
    string Status,
    string? QrCodeBase64);
```

- [ ] **Step 2: Create WhatsApp subscription**

In `src/EnterpriseAgentOs.Api/GraphQL/Subscriptions/WhatsAppSubscriptions.cs`:

```csharp
using System.Runtime.CompilerServices;
using EnterpriseAgentOs.Api.GraphQL.Types;
using EnterpriseAgentOs.Infrastructure.Adapters.Channels.WhatsApp;

namespace EnterpriseAgentOs.Api.GraphQL.Subscriptions;

[ExtendObjectType(typeof(GraphQLSubscriptions))]
public class WhatsAppSubscriptions
{
    [Subscribe(With = nameof(WhatsAppConnectionStatusStream))]
    public WhatsAppConnectionStatusPayload WhatsAppConnectionStatus(
        Guid connectionId,
        [EventMessage] WhatsAppConnectionStatusPayload payload)
        => payload;

    public async IAsyncEnumerable<WhatsAppConnectionStatusPayload> WhatsAppConnectionStatusStream(
        Guid connectionId,
        [Service] WhatsAppGatewayService gateway,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Emit current state immediately
        var currentState = gateway.GetConnectionState(connectionId);
        var currentQr = gateway.GetPendingQrCode(connectionId);
        yield return new WhatsAppConnectionStatusPayload(connectionId, currentState, currentQr);

        // Then stream updates
        var channel = System.Threading.Channels.Channel.CreateUnbounded<WhatsAppConnectionStatusPayload>();

        void OnStatusChanged(Guid id, string status, string? qr)
        {
            if (id == connectionId)
                channel.Writer.TryWrite(new WhatsAppConnectionStatusPayload(id, status, qr));
        }

        gateway.ConnectionStatusChanged += OnStatusChanged;
        try
        {
            await foreach (var payload in channel.Reader.ReadAllAsync(ct))
            {
                yield return payload;
                if (payload.Status == "open") break; // Done pairing
            }
        }
        finally
        {
            gateway.ConnectionStatusChanged -= OnStatusChanged;
        }
    }
}
```

- [ ] **Step 3: Verify build**

```bash
cd apps/backend && dotnet build EnterpriseAgentOs.sln
```

- [ ] **Step 4: Commit**

```bash
cd apps/backend
git add src/EnterpriseAgentOs.Api/GraphQL/Types/WhatsAppTypes.cs src/EnterpriseAgentOs.Api/GraphQL/Subscriptions/WhatsAppSubscriptions.cs
git commit -m "feat: add GraphQL subscription for WhatsApp QR code + connection status"
```

---

## Task 5: Wire Up DI + Delete Old Adapter

**Files:**
- Modify: `src/EnterpriseAgentOs.Api/Extensions/ServiceCollectionExtensions.cs`
- Delete: `src/EnterpriseAgentOs.Infrastructure/Adapters/Channels/Adapters/WhatsAppAdapter.cs`

- [ ] **Step 1: Remove WhatsAppAdapter registration and add gateway service**

In `ServiceCollectionExtensions.cs`, remove this line:

```csharp
services.AddSingleton<IChannelAdapter, Infrastructure.Adapters.Channels.Adapters.WhatsAppAdapter>();
```

In `AddBackgroundServices()`, add:

```csharp
services.AddSingleton<WhatsAppSessionStore>();
services.AddSingleton<WhatsAppGatewayService>();
services.AddHostedService(sp => sp.GetRequiredService<WhatsAppGatewayService>());
```

The double registration (singleton + hosted service) ensures other services can inject `WhatsAppGatewayService` directly while the host manages its lifecycle.

Add the using at the top of the file or in GlobalUsings:

```csharp
using EnterpriseAgentOs.Infrastructure.Adapters.Channels.WhatsApp;
```

- [ ] **Step 2: Delete the old WhatsApp adapter**

```bash
rm apps/backend/src/EnterpriseAgentOs.Infrastructure/Adapters/Channels/Adapters/WhatsAppAdapter.cs
```

- [ ] **Step 3: Verify build**

```bash
cd apps/backend && dotnet build EnterpriseAgentOs.sln
```

- [ ] **Step 4: Commit**

```bash
cd apps/backend
git add -A
git commit -m "feat: replace WhatsAppAdapter with WhatsAppGatewayService in DI"
```

---

## Task 6: Update Channel Type Definition + Mutation Hook

**Files:**
- Modify: `src/EnterpriseAgentOs.Application/DTOs/Channels/ChannelDto.cs`
- Modify: `src/EnterpriseAgentOs.Api/GraphQL/Mutations/ChannelMutations.cs`

- [ ] **Step 1: Update WhatsApp channel type definition**

In `ChannelDto.cs`, replace the WhatsApp `ChannelTypeDefinition` entry (the one with `"whatsapp"`) with:

```csharp
new ChannelTypeDefinition("whatsapp", "WhatsApp", "Connect via WhatsApp Web — scan a QR code to link your phone",
    "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M17.47 14.38c-.3-.15-1.76-.87-2.03-.97-.27-.1-.47-.15-.67.15-.2.3-.77.97-.95 1.17-.17.2-.35.22-.65.07-.3-.15-1.27-.47-2.42-1.49-.9-.8-1.5-1.78-1.67-2.08-.18-.3-.02-.46.13-.61.13-.13.3-.35.45-.52.15-.17.2-.3.3-.5.1-.2.05-.37-.02-.52-.08-.15-.67-1.62-.92-2.22-.24-.58-.49-.5-.67-.51h-.58c-.2 0-.52.07-.8.37-.27.3-1.04 1.02-1.04 2.49s1.07 2.89 1.22 3.09c.15.2 2.1 3.2 5.08 4.49.71.31 1.27.49 1.7.63.71.23 1.36.2 1.87.12.57-.09 1.76-.72 2.01-1.42.25-.7.25-1.29.17-1.42-.07-.13-.27-.2-.57-.35zm-5.42 7.4A9.87 9.87 0 0 1 7 20.07l-.36-.21-3.73.98.99-3.63-.24-.37a9.87 9.87 0 0 1-1.51-5.26c0-5.45 4.44-9.89 9.9-9.89a9.89 9.89 0 0 1 9.89 9.9c0 5.45-4.44 9.88-9.9 9.88zm8.41-18.29A11.82 11.82 0 0 0 12.05 0C5.47 0 .1 5.37.1 11.95c0 2.1.55 4.16 1.6 5.97L0 24l6.24-1.64a11.94 11.94 0 0 0 5.81 1.49c6.58 0 11.94-5.37 11.94-11.95a11.87 11.87 0 0 0-3.53-8.41z\"/></svg>",
    Array.Empty<ChannelConfigField>()),
```

No config fields — authentication happens via QR code, not token input.

- [ ] **Step 2: Hook CreateChannelConnection to start gateway**

In `ChannelMutations.cs`, modify `CreateChannelConnection` to trigger the gateway when creating a WhatsApp connection. After `var created = await repo.CreateConnectionAsync(record, ct);`, add:

```csharp
// For WhatsApp, start the gateway connection (QR code pairing)
if (string.Equals(input.ChannelType, "whatsapp", StringComparison.OrdinalIgnoreCase))
{
    var gateway = context.Services.GetRequiredService<WhatsAppGatewayService>();
    gateway.StartConnection(created.Id);
}
```

Add the using at the top:

```csharp
using EnterpriseAgentOs.Infrastructure.Adapters.Channels.WhatsApp;
```

Similarly, in `DeleteChannelConnection`, before `var result = await repo.DeleteConnectionAsync(id, ct);`, add:

```csharp
// Stop WhatsApp connection if applicable
var existing = await repo.GetConnectionAsync(id, ct);
if (existing is not null && string.Equals(existing.ChannelType, "whatsapp", StringComparison.OrdinalIgnoreCase))
{
    var gateway = context.Services.GetRequiredService<WhatsAppGatewayService>();
    gateway.StopConnection(id);
}
```

- [ ] **Step 3: Clean up webhook controller**

In `ChannelWebhooksController.cs`, the `HandleWebhookVerification` GET endpoint currently serves WhatsApp's `hub.verify_token` challenge. This is no longer needed for WhatsApp (we don't use webhooks), but the endpoint is generic and other platforms may use GET verification in the future. Leave the endpoint as-is — it's harmless and doesn't need WhatsApp-specific removal.

- [ ] **Step 4: Verify build**

```bash
cd apps/backend && dotnet build EnterpriseAgentOs.sln
```

- [ ] **Step 5: Commit**

```bash
cd apps/backend
git add -A
git commit -m "feat: update WhatsApp channel definition to QR-only + wire gateway into mutations"
```

---

## Task 7: Dashboard — WhatsApp QR Code Onboarding Flow

**Files:**
- Modify: `apps/dashboard/src/features/agents/api/useChannels.ts`
- Modify: `apps/dashboard/src/features/agents/components/channel-onboarding-dialog.tsx`

- [ ] **Step 1: Add WhatsApp subscription hook**

In `apps/dashboard/src/features/agents/api/useChannels.ts`, add after the existing imports and queries:

```typescript
const WHATSAPP_STATUS_SUBSCRIPTION = gql`
  subscription WhatsAppConnectionStatus($connectionId: UUID!) {
    whatsAppConnectionStatus(connectionId: $connectionId) {
      connectionId
      status
      qrCodeBase64
    }
  }
`

export function useWhatsAppConnectionStatus(connectionId: string | null) {
  const { data, loading, error } = useSubscription(WHATSAPP_STATUS_SUBSCRIPTION, {
    variables: { connectionId },
    skip: !connectionId,
  })

  return {
    status: (data?.whatsAppConnectionStatus?.status as string) ?? "connecting",
    qrCodeBase64: (data?.whatsAppConnectionStatus?.qrCodeBase64 as string) ?? null,
    loading,
    error: error ?? undefined,
  }
}
```

Add `useSubscription` to the Apollo import:

```typescript
import { gql, useMutation, useQuery, useSubscription } from "@apollo/client"
```

- [ ] **Step 2: Update the onboarding dialog for WhatsApp QR flow**

In `apps/dashboard/src/features/agents/components/channel-onboarding-dialog.tsx`, update the imports:

```typescript
import { useCreateChannelConnection, useWhatsAppConnectionStatus } from "../api/useChannels"
```

Add state for WhatsApp QR flow. After `const [error, setError] = useState<string | null>(null)`:

```typescript
const [whatsappConnectionId, setWhatsappConnectionId] = useState<string | null>(null)
const isWhatsApp = channel.slug === "whatsapp"
const { status: waStatus, qrCodeBase64 } = useWhatsAppConnectionStatus(
  isWhatsApp ? whatsappConnectionId : null
)
```

Update the `reset` function to also clear WhatsApp state:

```typescript
function reset() {
  setStep(0)
  setInputs({})
  setError(null)
  setConnecting(false)
  setWhatsappConnectionId(null)
}
```

Replace the `handleComplete` function with one that handles the WhatsApp flow:

```typescript
async function handleComplete() {
  setConnecting(true)
  setError(null)
  try {
    if (isWhatsApp) {
      // Create connection — backend starts WASocket and generates QR
      const result = await createChannelConnection({
        channelType: channel.slug,
        displayName: channel.name,
        config: {},
      })
      setWhatsappConnectionId(result.id)
      // Don't close dialog — wait for QR scan + connection
    } else {
      await createChannelConnection({
        channelType: channel.slug,
        displayName: channel.name,
        config: inputs,
      })
      onComplete()
      reset()
      onOpenChange(false)
    }
  } catch (e) {
    setError(e instanceof Error ? e.message : "Failed to connect channel")
    setConnecting(false)
  }
}
```

Add an effect to close the dialog when WhatsApp connects:

```typescript
React.useEffect(() => {
  if (isWhatsApp && waStatus === "open") {
    onComplete()
    reset()
    onOpenChange(false)
  }
}, [waStatus])
```

Add `React` to imports (or use `useEffect` directly — it's already imported from React).

Now add the WhatsApp QR rendering. Before the `{!hasSteps ? (` block in the JSX, add:

```tsx
{isWhatsApp && whatsappConnectionId ? (
  <div className="space-y-4 pt-2">
    {waStatus === "qr" && qrCodeBase64 ? (
      <>
        <p className="text-sm text-muted-foreground">
          Scan this QR code with WhatsApp on your phone to connect.
        </p>
        <div className="rounded-lg border border-border p-6 flex flex-col items-center gap-3">
          <img
            src={`data:image/png;base64,${qrCodeBase64}`}
            alt="WhatsApp QR Code"
            className="size-48"
          />
          <p className="text-xs text-muted-foreground">
            Open WhatsApp → Settings → Linked Devices → Link a Device
          </p>
        </div>
      </>
    ) : waStatus === "open" ? (
      <div className="flex flex-col items-center gap-2 py-4">
        <div className="size-12 rounded-full bg-emerald-100 flex items-center justify-center">
          <svg className="size-6 text-emerald-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
          </svg>
        </div>
        <p className="text-sm font-medium">Connected!</p>
      </div>
    ) : waStatus === "error" ? (
      <p className="text-sm text-destructive">Connection failed. Please try again.</p>
    ) : (
      <div className="flex flex-col items-center gap-2 py-6">
        <LoaderIcon className="size-6 animate-spin text-muted-foreground" />
        <p className="text-sm text-muted-foreground">Connecting to WhatsApp...</p>
      </div>
    )}
    <div className="flex items-center gap-2 pt-2">
      <Button size="sm" variant="ghost" onClick={() => { reset(); onOpenChange(false) }}>
        Cancel
      </Button>
    </div>
  </div>
) : isWhatsApp && !whatsappConnectionId ? (
  <div className="space-y-3 pt-2">
    <p className="text-sm text-muted-foreground">
      Connect your WhatsApp account by scanning a QR code. Your phone stays connected — this works like WhatsApp Web.
    </p>
    <div className="flex items-center gap-2 pt-2">
      <Button size="sm" onClick={handleComplete} disabled={connecting}>
        {connecting && <LoaderIcon className="size-3 animate-spin" />}
        Generate QR Code
      </Button>
      <Button size="sm" variant="ghost" onClick={() => onOpenChange(false)} className="ml-auto" disabled={connecting}>Cancel</Button>
    </div>
  </div>
) : !hasSteps ? (
```

And close the new conditional properly — change the existing `{!hasSteps ? (` to just be part of the else branch above. The full conditional chain becomes:

```
{isWhatsApp && whatsappConnectionId ? (
  ... QR flow ...
) : isWhatsApp && !whatsappConnectionId ? (
  ... "Generate QR Code" button ...
) : !hasSteps ? (
  ... existing no-steps flow ...
) : (
  ... existing multi-step flow ...
)}
```

- [ ] **Step 3: Verify TypeScript compiles**

```bash
cd apps/dashboard && npx tsc --noEmit
```

- [ ] **Step 4: Commit**

```bash
cd apps/dashboard
git add src/features/agents/api/useChannels.ts src/features/agents/components/channel-onboarding-dialog.tsx
git commit -m "feat: WhatsApp onboarding dialog with live QR code subscription"
```

---

## Task 8: Build Verification + Integration Test

**Files:**
- No new files — verify everything works together

- [ ] **Step 1: Full backend build**

```bash
cd apps/backend && dotnet build EnterpriseAgentOs.sln
```

- [ ] **Step 2: Run existing tests**

```bash
cd apps/backend && dotnet test EnterpriseAgentOs.Api.Tests/EnterpriseAgentOs.Api.Tests.csproj
```

Fix any compilation errors from the deleted WhatsAppAdapter — if any test references it, update to use the new WhatsAppGatewayService or remove the test.

- [ ] **Step 3: Full dashboard type check**

```bash
cd apps/dashboard && npx tsc --noEmit
```

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "chore: verify full build after WhatsApp Web integration"
```

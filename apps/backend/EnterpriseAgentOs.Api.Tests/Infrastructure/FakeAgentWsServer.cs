using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace EnterpriseAgentOs.Api.Tests.Infrastructure;

/// <summary>
/// A lightweight in-process WebSocket server that mimics the zeroclaw agent-core
/// WS gateway. Accepts a connection, reads a user_message, and replies with
/// assistant_delta + turn_complete.
/// </summary>
public sealed class FakeAgentWsServer : IAsyncDisposable
{
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _acceptLoop;
    private string _responseText = "Hello from agent";

    public string Url { get; }
    public int Port { get; }

    public FakeAgentWsServer()
    {
        // Find a free port
        var tempListener = new TcpListener(IPAddress.Loopback, 0);
        tempListener.Start();
        Port = ((IPEndPoint)tempListener.LocalEndpoint).Port;
        tempListener.Stop();

        Url = $"ws://localhost:{Port}";
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://localhost:{Port}/");
        _listener.Start();
        _acceptLoop = AcceptLoopAsync(_cts.Token);
    }

    /// <summary>Sets the text the fake agent will respond with on the next message.</summary>
    public void SetResponse(string text) => _responseText = text;

    /// <summary>Returns a ServiceUrl suitable for ChannelMessageRouter (includes /ws/chat path).</summary>
    public string ServiceUrl => $"{Url}/ws/chat";

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }

            if (!context.Request.IsWebSocketRequest)
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
                continue;
            }

            _ = HandleConnectionAsync(context, ct);
        }
    }

    private async Task HandleConnectionAsync(HttpListenerContext context, CancellationToken ct)
    {
        var wsContext = await context.AcceptWebSocketAsync(subProtocol: "zeroclaw.v1");
        var ws = wsContext.WebSocket;
        var buffer = new byte[4096];

        try
        {
            // Read incoming user_message
            var result = await ws.ReceiveAsync(buffer, ct);
            if (result.MessageType == WebSocketMessageType.Close) return;

            var inbound = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var json = JsonSerializer.Deserialize<JsonElement>(inbound);
            var turnId = json.TryGetProperty("id", out var idProp)
                ? idProp.GetString() ?? Guid.NewGuid().ToString("N")
                : Guid.NewGuid().ToString("N");

            // Send assistant_delta
            var delta = JsonSerializer.Serialize(new
            {
                type = "assistant_delta",
                turn_id = turnId,
                text = _responseText,
            });
            await ws.SendAsync(
                Encoding.UTF8.GetBytes(delta),
                WebSocketMessageType.Text, true, ct);

            // Send turn_complete
            var complete = JsonSerializer.Serialize(new
            {
                type = "turn_complete",
                turn_id = turnId,
                cancelled = false,
            });
            await ws.SendAsync(
                Encoding.UTF8.GetBytes(complete),
                WebSocketMessageType.Text, true, ct);

            // Wait for client to close
            try
            {
                var closeResult = await ws.ReceiveAsync(buffer, ct);
                if (closeResult.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
                }
            }
            catch (WebSocketException) { }
        }
        catch (OperationCanceledException) { }
        catch (WebSocketException) { }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        try { await _acceptLoop; } catch { }
        _cts.Dispose();
    }
}

using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace EnterpriseAgentOs.Api.Entities.Agents;

public static class AgentProxyEndpoints
{
    private const int ZeroclawPort = 42617;
    private const int BufferSize = 8 * 1024;

    public static void MapAgentProxyEndpoints(this IEndpointRouteBuilder app)
    {
        app.Map("/api/agents/{id:guid}/ws", HandleWebSocketAsync);
    }

    private static async Task HandleWebSocketAsync(
        HttpContext context,
        Guid id,
        IAgentService agents,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Expected WebSocket request", ct);
            return;
        }

        var agent = await agents.GetAsync(id, ct);
        if (agent is null || string.IsNullOrEmpty(agent.PodName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Agent not found or pod not deployed", ct);
            return;
        }

        var subProtocol = context.WebSockets.WebSocketRequestedProtocols
            .FirstOrDefault(p => p == "zeroclaw.v1");

        using var browser = subProtocol is null
            ? await context.WebSockets.AcceptWebSocketAsync()
            : await context.WebSockets.AcceptWebSocketAsync(subProtocol);

        var upstreamUri = new Uri(
            $"ws://{agent.PodName}.default.svc.cluster.local:{ZeroclawPort}/ws/chat");

        using var upstream = new ClientWebSocket();
        upstream.Options.AddSubProtocol("zeroclaw.v1");

        try
        {
            await upstream.ConnectAsync(upstreamUri, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to connect upstream to agent {AgentId} at {Uri}", id, upstreamUri);
            await browser.CloseAsync(
                WebSocketCloseStatus.EndpointUnavailable,
                "Agent pod not reachable",
                CancellationToken.None);
            return;
        }

        var browserToUpstream = PumpAsync(browser, upstream, ct);
        var upstreamToBrowser = PumpAsync(upstream, browser, ct);

        await Task.WhenAny(browserToUpstream, upstreamToBrowser);

        if (upstream.State == WebSocketState.Open)
        {
            await upstream.CloseAsync(WebSocketCloseStatus.NormalClosure, "client closed", CancellationToken.None);
        }
        if (browser.State == WebSocketState.Open)
        {
            await browser.CloseAsync(WebSocketCloseStatus.NormalClosure, "upstream closed", CancellationToken.None);
        }
    }

    private static async Task PumpAsync(WebSocket from, WebSocket to, CancellationToken ct)
    {
        var buffer = new byte[BufferSize];
        try
        {
            while (from.State == WebSocketState.Open && to.State == WebSocketState.Open)
            {
                var result = await from.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return;
                }
                await to.SendAsync(
                    new ArraySegment<byte>(buffer, 0, result.Count),
                    result.MessageType,
                    result.EndOfMessage,
                    ct);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }
    }
}

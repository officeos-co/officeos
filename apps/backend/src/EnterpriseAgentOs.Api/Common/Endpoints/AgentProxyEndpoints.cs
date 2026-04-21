namespace EnterpriseAgentOs.Api.Common.Endpoints;

public static class AgentProxyEndpoints
{
    private const int ZeroclawPort = 42617;
    private const int BufferSize = 8 * 1024;

    public static void MapAgentProxyEndpoints(this IEndpointRouteBuilder app)
    {
        app.Map("/api/agents/{id:guid}/ws", HandleWebSocketAsync);
        app.Map("/api/agents/{id:guid}/proxy/{**path}", HandleHttpProxyAsync);
    }

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization",
        "TE", "Trailers", "Transfer-Encoding", "Upgrade",
        "Host", "Content-Length",
    };

    private static async Task HandleHttpProxyAsync(
        HttpContext context,
        Guid id,
        string? path,
        IAgentService agents,
        IHttpClientFactory httpClientFactory,
        ILogger<Program> logger,
        CancellationToken ct)
    {
        var agent = await agents.GetAsync(id, ct);
        if (agent is null || string.IsNullOrEmpty(agent.PodName))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("Agent not found or pod not deployed", ct);
            return;
        }

        var upstreamPath = string.IsNullOrEmpty(path) ? string.Empty : $"/{path}";
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var upstream = new Uri(
            $"http://{agent.PodName}.default.svc.cluster.local:{ZeroclawPort}{upstreamPath}{query}");

        var upstreamRequest = new HttpRequestMessage(
            new HttpMethod(context.Request.Method),
            upstream);

        if (!HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method)
            && !HttpMethods.IsDelete(context.Request.Method))
        {
            upstreamRequest.Content = new StreamContent(context.Request.Body);
            foreach (var header in context.Request.Headers)
            {
                if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                {
                    upstreamRequest.Content.Headers.TryAddWithoutValidation(
                        header.Key, header.Value.ToArray());
                }
            }
        }

        foreach (var header in context.Request.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key)) continue;
            if (header.Key.StartsWith("Content-", StringComparison.OrdinalIgnoreCase)) continue;
            upstreamRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        }

        var client = httpClientFactory.CreateClient("agent-proxy");
        HttpResponseMessage upstreamResponse;
        try
        {
            upstreamResponse = await client.SendAsync(
                upstreamRequest,
                HttpCompletionOption.ResponseHeadersRead,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Agent proxy upstream failed for {AgentId} → {Uri}", id, upstream);
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsync($"Agent pod not reachable: {ex.Message}", ct);
            return;
        }

        context.Response.StatusCode = (int)upstreamResponse.StatusCode;
        foreach (var header in upstreamResponse.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key)) continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        foreach (var header in upstreamResponse.Content.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key)) continue;
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
        context.Response.Headers.Remove("transfer-encoding");

        await upstreamResponse.Content.CopyToAsync(context.Response.Body, ct);
        upstreamResponse.Dispose();
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

        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;
        var upstreamUri = new Uri(
            $"ws://{agent.PodName}.default.svc.cluster.local:{ZeroclawPort}/ws/chat{query}");

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

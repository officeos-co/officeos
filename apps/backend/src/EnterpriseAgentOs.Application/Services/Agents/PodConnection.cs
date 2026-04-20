using System.Net.WebSockets;

namespace EnterpriseAgentOs.Application.Services.Agents;

public sealed class PodConnection : IDisposable
{
    private readonly ClientWebSocket _ws = new();
    private readonly string _promptMarker;
    private static int _counter;

    public PodConnection(string promptMarker = "__EAOS_DONE:")
    {
        _promptMarker = promptMarker;
    }

    public async Task<AgentResult<bool>> ConnectAsync(string podName, string ns, Guid agentId, CancellationToken ct)
    {
        try
        {
            var uri = new Uri($"ws://{podName}.{ns}.svc.cluster.local:42617/ws?token={agentId}");
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(10));
            await _ws.ConnectAsync(uri, cts.Token);

            // Set custom PS1 for completion detection.
            await SendRawAsync(JsonSerializer.Serialize(new { id = "init", input = $"export PS1='{_promptMarker}$?__\\n$ '\n" }), ct);
            await ReadUntilPromptAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.PodConnection, $"Failed to connect to pod: {ex.Message}", ex.ToString());
        }
    }

    public async Task<AgentResult<(string Output, int ExitCode)>> ExecuteAsync(string command, CancellationToken ct)
    {
        try
        {
            var id = $"cmd-{Interlocked.Increment(ref _counter)}";
            var request = JsonSerializer.Serialize(new { id, input = command + "\n" });
            await SendRawAsync(request, ct);
            return await ReadUntilPromptAsync(ct);
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.PodConnection, $"Command execution failed: {ex.Message}", ex.ToString());
        }
    }

    private async Task SendRawAsync(string text, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private async Task<(string Output, int ExitCode)> ReadUntilPromptAsync(CancellationToken ct)
    {
        var output = new StringBuilder();
        var buf = new byte[64 * 1024];
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromMinutes(5));

        while (true)
        {
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
            if (result.MessageType == WebSocketMessageType.Close) break;

            var text = Encoding.UTF8.GetString(buf, 0, result.Count);

            try
            {
                using var doc = JsonDocument.Parse(text);
                var data = doc.RootElement.GetProperty("data").GetString() ?? "";
                output.Append(data);
            }
            catch (JsonException)
            {
                output.Append(text);
            }

            var full = output.ToString();
            var markerIdx = full.LastIndexOf(_promptMarker, StringComparison.Ordinal);
            if (markerIdx >= 0)
            {
                var afterMarker = full[(markerIdx + _promptMarker.Length)..];
                var endIdx = afterMarker.IndexOf("__", StringComparison.Ordinal);
                var exitCodeStr = endIdx >= 0 ? afterMarker[..endIdx] : "0";
                int.TryParse(exitCodeStr, out var exitCode);

                var cleanOutput = full[..markerIdx].TrimEnd('\n', '\r');
                return (cleanOutput, exitCode);
            }
        }

        return (output.ToString(), -1);
    }

    public void Dispose()
    {
        if (_ws.State == WebSocketState.Open)
        {
            try
            {
                _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "turn complete",
                    CancellationToken.None).GetAwaiter().GetResult();
            }
            catch { /* best-effort close */ }
        }
        _ws.Dispose();
    }
}

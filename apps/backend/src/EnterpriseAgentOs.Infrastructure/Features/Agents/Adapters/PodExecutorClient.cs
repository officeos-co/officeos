namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class PodExecutorClient
{
    private const string WorkspacePath = "/workspace";

    private readonly HttpClient _http;

    public PodExecutorClient()
        : this(new HttpClient())
    {
    }

    internal PodExecutorClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<AgentResult<AgentSandboxCommandResult>> ExecuteAsync(
        string sandboxId,
        string serviceUrl,
        string command,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            var body = new ExecuteRequest(
                command,
                WorkspacePath,
                null,
                Math.Max(1, (int)Math.Ceiling(timeout.TotalSeconds)));

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                BuildEndpointUri(serviceUrl, "process/execute"))
            {
                Content = JsonContent.Create(body),
            };
            AddBearer(request, sandboxId);

            using var response = await _http.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                return await ToAgentErrorAsync("Pod executor command execution failed", response, cts.Token);

            var payload = await response.Content.ReadFromJsonAsync<ExecuteResponse>(cts.Token);
            return new AgentSandboxCommandResult(payload?.Result ?? string.Empty, payload?.ExitCode ?? -1);
        }
        catch (OperationCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return new AgentError(
                AgentErrorCategory.PodConnection,
                $"Pod executor command timed out after {timeout.TotalSeconds:0}s",
                ex.ToString());
        }
        catch (Exception ex)
        {
            return new AgentError(
                AgentErrorCategory.PodConnection,
                $"Pod executor command execution failed: {ex.Message}",
                ex.ToString());
        }
    }

    public async Task<AgentResult<string>> ReadFileAsync(
        string sandboxId,
        string serviceUrl,
        string path,
        CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                BuildEndpointUri(serviceUrl, $"files/download?path={Uri.EscapeDataString(path)}"));
            AddBearer(request, sandboxId);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return await ToAgentErrorAsync($"Failed to read file '{path}'", response, ct);

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            return new AgentError(
                AgentErrorCategory.ToolExecution,
                $"Failed to read file '{path}': {ex.Message}",
                ex.ToString());
        }
    }

    public async Task<AgentResult<bool>> WriteFileAsync(
        string sandboxId,
        string serviceUrl,
        string path,
        string content,
        CancellationToken ct = default)
    {
        try
        {
            var parent = ParentDirectory(path.Replace('\\', '/'));
            if (parent is not null)
            {
                using var mkdirRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    BuildEndpointUri(serviceUrl, $"files/folder?path={Uri.EscapeDataString(parent)}&mode=0755"));
                AddBearer(mkdirRequest, sandboxId);

                using var mkdirResponse = await _http.SendAsync(mkdirRequest, ct);
                if (!mkdirResponse.IsSuccessStatusCode && mkdirResponse.StatusCode != HttpStatusCode.Conflict)
                    return await ToAgentErrorAsync($"Failed to create parent folder for '{path}'", mkdirResponse, ct);
            }

            using var form = new MultipartFormDataContent();
            var file = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(file, "file", Path.GetFileName(path));

            using var uploadRequest = new HttpRequestMessage(
                HttpMethod.Post,
                BuildEndpointUri(serviceUrl, $"files/upload?path={Uri.EscapeDataString(path)}"))
            {
                Content = form,
            };
            AddBearer(uploadRequest, sandboxId);

            using var uploadResponse = await _http.SendAsync(uploadRequest, ct);
            if (!uploadResponse.IsSuccessStatusCode)
                return await ToAgentErrorAsync($"Failed to write file '{path}'", uploadResponse, ct);

            return true;
        }
        catch (Exception ex)
        {
            return new AgentError(
                AgentErrorCategory.ToolExecution,
                $"Failed to write file '{path}': {ex.Message}",
                ex.ToString());
        }
    }

    internal static Uri BuildEndpointUri(string serviceUrl, string endpoint)
    {
        if (string.IsNullOrWhiteSpace(serviceUrl))
            throw new InvalidOperationException("Pod executor service URL is required.");

        var builder = new UriBuilder(serviceUrl);
        builder.Scheme = builder.Scheme switch
        {
            "http" or "https" => builder.Scheme,
            "ws" => "http",
            "wss" => "https",
            _ => throw new InvalidOperationException($"Unsupported pod executor URL scheme '{builder.Scheme}'."),
        };

        var basePath = builder.Path.TrimEnd('/');
        if (string.Equals(basePath, "/ws", StringComparison.OrdinalIgnoreCase))
            basePath = string.Empty;

        var endpointParts = endpoint.Split('?', 2);
        var endpointPath = endpointParts[0].TrimStart('/');
        builder.Path = string.IsNullOrEmpty(basePath)
            ? endpointPath
            : $"{basePath.TrimStart('/')}/{endpointPath}";
        builder.Query = endpointParts.Length == 2 ? endpointParts[1] : string.Empty;

        return builder.Uri;
    }

    private static void AddBearer(HttpRequestMessage request, string sandboxId)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sandboxId);
    }

    private static async Task<AgentError> ToAgentErrorAsync(
        string message,
        HttpResponseMessage response,
        CancellationToken ct)
    {
        var detail = await response.Content.ReadAsStringAsync(ct);
        return new AgentError(
            AgentErrorCategory.ToolExecution,
            $"{message} ({response.StatusCode})",
            detail);
    }

    private static string? ParentDirectory(string path)
    {
        var index = path.LastIndexOf('/');
        return index > 0 ? path[..index] : null;
    }

    private sealed record ExecuteRequest(
        [property: JsonPropertyName("command")] string Command,
        [property: JsonPropertyName("cwd")] string Cwd,
        [property: JsonPropertyName("envs")] IReadOnlyDictionary<string, string>? Envs,
        [property: JsonPropertyName("timeout")] int Timeout);

    private sealed record ExecuteResponse(
        [property: JsonPropertyName("result")] string Result,
        [property: JsonPropertyName("exitCode")] int ExitCode);
}

using System.Net;

namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class DaytonaSandboxProvider : IAgentSandbox
{
    private readonly HttpClient _http;
    private readonly DaytonaConfig _config;
    private readonly ILogger<DaytonaSandboxProvider> _logger;

    public DaytonaSandboxProvider(HttpClient http, DaytonaConfig config, ILogger<DaytonaSandboxProvider> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<AgentSandboxDeployment> CreateAsync(
        Guid agentId,
        AgentTemplateRecord? template,
        IReadOnlyDictionary<string, string> environment,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        EnsureApiConfigured();

        var env = new Dictionary<string, string>(environment);
        foreach (var (key, value) in metadata)
            env[$"EAOS_{NormalizeEnvKey(key)}"] = value;

        var labels = new Dictionary<string, string>
        {
            ["managed-by"] = "eaos",
            ["agent-id"] = agentId.ToString(),
        };

        var body = new Dictionary<string, object?>
        {
            ["env"] = env,
            ["labels"] = labels,
        };
        if (!string.IsNullOrWhiteSpace(_config.Target))
            body["target"] = _config.Target;
        if (!string.IsNullOrWhiteSpace(_config.Snapshot))
            body["snapshot"] = _config.Snapshot;

        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_config.ApiBaseUri, "sandbox"))
        {
            Content = JsonContent.Create(body),
        };
        AddBearer(request);

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            throw await ToExceptionAsync("Daytona sandbox creation failed", response, ct);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var sandboxId = ReadString(json, "id")
            ?? throw new InvalidOperationException("Daytona create response did not include sandbox id.");
        var toolboxProxyUrl = ReadString(json, "toolboxProxyUrl")
            ?? throw new InvalidOperationException("Daytona create response did not include toolboxProxyUrl.");

        _logger.LogInformation("Created Daytona sandbox {SandboxId} for agent {AgentId}", sandboxId, agentId);
        return new AgentSandboxDeployment(sandboxId, toolboxProxyUrl);
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
            EnsureApiConfigured();
            var toolboxBaseUri = BuildToolboxUri(serviceUrl, sandboxId);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(timeout);

            var body = new
            {
                command,
                cwd = _config.Workdir,
                timeout = Math.Max(1, (int)Math.Ceiling(timeout.TotalSeconds)),
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(toolboxBaseUri, "process/execute"))
            {
                Content = JsonContent.Create(body),
            };
            AddBearer(request);

            using var response = await _http.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
                return await ToAgentErrorAsync("Daytona command execution failed", response, cts.Token);

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cts.Token);
            var output = ReadString(json, "result") ?? string.Empty;
            var exitCode = ReadInt(json, "exitCode") ?? 0;
            return new AgentSandboxCommandResult(output, exitCode);
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.PodConnection, $"Daytona command execution failed: {ex.Message}", ex.ToString());
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
            EnsureApiConfigured();
            var toolboxBaseUri = BuildToolboxUri(serviceUrl, sandboxId);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                new Uri(toolboxBaseUri, $"files/download?path={Uri.EscapeDataString(path)}"));
            AddBearer(request);

            using var response = await _http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return await ToAgentErrorAsync("Daytona file read failed", response, ct);

            return await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, $"Daytona file read failed: {ex.Message}", ex.ToString());
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
            EnsureApiConfigured();
            var toolboxBaseUri = BuildToolboxUri(serviceUrl, sandboxId);
            var parent = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                using var mkdirRequest = new HttpRequestMessage(
                    HttpMethod.Post,
                    new Uri(toolboxBaseUri, $"files/folder?path={Uri.EscapeDataString(parent)}&mode=0755"));
                AddBearer(mkdirRequest);

                using var mkdirResponse = await _http.SendAsync(mkdirRequest, ct);
                if (!mkdirResponse.IsSuccessStatusCode && mkdirResponse.StatusCode != HttpStatusCode.Conflict)
                    return await ToAgentErrorAsync("Daytona parent directory creation failed", mkdirResponse, ct);
            }

            using var form = new MultipartFormDataContent();
            var bytes = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
            bytes.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(bytes, "file", Path.GetFileName(path));

            using var writeRequest = new HttpRequestMessage(
                HttpMethod.Post,
                new Uri(toolboxBaseUri, $"files/upload?path={Uri.EscapeDataString(path)}"))
            {
                Content = form,
            };
            AddBearer(writeRequest);

            using var writeResponse = await _http.SendAsync(writeRequest, ct);
            if (!writeResponse.IsSuccessStatusCode)
                return await ToAgentErrorAsync("Daytona file write failed", writeResponse, ct);

            return true;
        }
        catch (Exception ex)
        {
            return new AgentError(AgentErrorCategory.ToolExecution, $"Daytona file write failed: {ex.Message}", ex.ToString());
        }
    }

    public async Task<bool> TerminateAsync(string sandboxId, CancellationToken ct = default)
    {
        try
        {
            EnsureApiConfigured();
            using var request = new HttpRequestMessage(
                HttpMethod.Delete,
                new Uri(_config.ApiBaseUri, $"sandbox/{Uri.EscapeDataString(sandboxId)}"));
            AddBearer(request);

            using var response = await _http.SendAsync(request, ct);
            return response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to terminate Daytona sandbox {SandboxId}", sandboxId);
            return false;
        }
    }

    private void EnsureApiConfigured()
    {
        if (string.IsNullOrWhiteSpace(_config.ApiUrl))
            throw new InvalidOperationException("DAYTONA_API_URL is required.");
        if (string.IsNullOrWhiteSpace(_config.ApiKey))
            throw new InvalidOperationException("DAYTONA_API_KEY is required.");
        if (string.IsNullOrWhiteSpace(_config.Workdir))
            throw new InvalidOperationException("DAYTONA_WORKDIR is required.");
    }

    private static Uri BuildToolboxUri(string serviceUrl, string sandboxId)
    {
        if (string.IsNullOrWhiteSpace(serviceUrl))
            throw new InvalidOperationException("Daytona toolboxProxyUrl is required.");

        var trimmed = serviceUrl.TrimEnd('/');
        var escapedSandboxId = Uri.EscapeDataString(sandboxId);
        if (!trimmed.EndsWith("/" + sandboxId, StringComparison.Ordinal)
            && !trimmed.EndsWith("/" + escapedSandboxId, StringComparison.Ordinal))
        {
            trimmed = $"{trimmed}/{escapedSandboxId}";
        }

        return new Uri(trimmed + "/");
    }

    private void AddBearer(HttpRequestMessage request)
        => request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.ApiKey);

    private static async Task<AgentError> ToAgentErrorAsync(string message, HttpResponseMessage response, CancellationToken ct)
    {
        var detail = await response.Content.ReadAsStringAsync(ct);
        return new AgentError(AgentErrorCategory.ToolExecution, $"{message} ({response.StatusCode})", detail);
    }

    private static async Task<Exception> ToExceptionAsync(string message, HttpResponseMessage response, CancellationToken ct)
    {
        var detail = await response.Content.ReadAsStringAsync(ct);
        return new InvalidOperationException($"{message} ({response.StatusCode}): {detail}");
    }

    private static string? ReadString(JsonElement json, string property)
        => json.ValueKind == JsonValueKind.Object && json.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement json, string property)
        => json.ValueKind == JsonValueKind.Object && json.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var parsed)
            ? parsed
            : null;

    private static string NormalizeEnvKey(string key)
    {
        var builder = new StringBuilder(key.Length);
        foreach (var ch in key)
            builder.Append(char.IsLetterOrDigit(ch) ? char.ToUpperInvariant(ch) : '_');
        return builder.ToString();
    }
}

namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class DockerAgentSandbox : IAgentSandbox, IAgentDeployer
{
    private const int PodExecutorPort = 42617;
    private const string WorkspacePath = "/workspace";

    private readonly HttpClient _docker;
    private readonly DockerConfig _config;
    private readonly PodExecutorClient _executor;
    private readonly ILogger<DockerAgentSandbox> _logger;

    public DockerAgentSandbox(
        DockerConfig config,
        PodExecutorClient executor,
        ILogger<DockerAgentSandbox> logger)
        : this(CreateDockerClient(config.SocketPath), config, executor, logger)
    {
    }

    internal DockerAgentSandbox(
        HttpClient docker,
        DockerConfig config,
        PodExecutorClient executor,
        ILogger<DockerAgentSandbox> logger)
    {
        _docker = docker;
        _config = config;
        _executor = executor;
        _logger = logger;
    }

    public async Task<AgentSandboxDeployment> CreateAsync(
        Guid agentId,
        AgentTemplateRecord? template,
        IReadOnlyDictionary<string, string> environment,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken ct = default)
    {
        var deployment = await DeployAsync(agentId, ct);
        return new AgentSandboxDeployment(deployment.PodName, deployment.ServiceUrl);
    }

    public async Task<AgentDeployment> DeployAsync(Guid agentId, CancellationToken ct = default)
    {
        var sandboxId = SandboxName(agentId);
        _logger.LogInformation(
            "Deploying agent {AgentId} as Docker pod executor {SandboxId} using image {Image}",
            agentId,
            sandboxId,
            _config.Image);

        using var content = JsonContent.Create(BuildCreateContainerBody(agentId, _config));
        var createResponse = await _docker.PostAsync($"/containers/create?name={sandboxId}", content, ct);

        if (!createResponse.IsSuccessStatusCode && createResponse.StatusCode != HttpStatusCode.Conflict)
        {
            var error = await createResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Docker create failed ({createResponse.StatusCode}): {error}");
        }

        var startResponse = await _docker.PostAsync($"/containers/{sandboxId}/start", null, ct);
        if (!startResponse.IsSuccessStatusCode && startResponse.StatusCode != HttpStatusCode.NotModified)
        {
            var error = await startResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Docker start failed ({startResponse.StatusCode}): {error}");
        }

        return new AgentDeployment(sandboxId, ServiceUrl(sandboxId));
    }

    public Task<AgentResult<AgentSandboxCommandResult>> ExecuteAsync(
        string sandboxId,
        string serviceUrl,
        string command,
        TimeSpan timeout,
        CancellationToken ct = default)
        => _executor.ExecuteAsync(sandboxId, serviceUrl, command, timeout, ct);

    public Task<AgentResult<string>> ReadFileAsync(
        string sandboxId,
        string serviceUrl,
        string path,
        CancellationToken ct = default)
        => _executor.ReadFileAsync(sandboxId, serviceUrl, path, ct);

    public Task<AgentResult<bool>> WriteFileAsync(
        string sandboxId,
        string serviceUrl,
        string path,
        string content,
        CancellationToken ct = default)
        => _executor.WriteFileAsync(sandboxId, serviceUrl, path, content, ct);

    public Task<bool> TerminateAsync(string sandboxId, CancellationToken ct = default)
        => RemoveAsync(sandboxId, ct);

    public async Task<bool> RemoveAsync(string podName, CancellationToken ct = default)
    {
        try
        {
            await _docker.PostAsync($"/containers/{podName}/stop", null, ct);
            await _docker.DeleteAsync($"/containers/{podName}?force=true&v=true", ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove Docker pod executor {SandboxId}", podName);
            return false;
        }
    }

    public async Task<string> GetStatusAsync(string podName, CancellationToken ct = default)
    {
        try
        {
            var response = await _docker.GetAsync($"/containers/{podName}/json", ct);
            if (!response.IsSuccessStatusCode)
                return "not_found";

            var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
            var status = doc!.RootElement.GetProperty("State").GetProperty("Status").GetString();

            return status switch
            {
                "running" => "running",
                "created" => "pending",
                "restarting" => "pending",
                "exited" => "stopped",
                "dead" => "failed",
                _ => "unknown",
            };
        }
        catch
        {
            return "not_found";
        }
    }

    public async Task<string> GetLogsAsync(string podName, int tailLines = 200, CancellationToken ct = default)
    {
        try
        {
            var response = await _docker.GetAsync(
                $"/containers/{podName}/logs?stdout=true&stderr=true&tail={tailLines}",
                ct);
            return response.IsSuccessStatusCode
                ? await response.Content.ReadAsStringAsync(ct)
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    internal static string SandboxName(Guid id) => $"eaos-agent-{id.ToString("N")[..8]}";

    internal static string ServiceUrl(string sandboxId) => $"http://{sandboxId}:{PodExecutorPort}";

    internal static object BuildCreateContainerBody(Guid agentId, DockerConfig config)
    {
        var sandboxId = SandboxName(agentId);
        return new
        {
            Image = config.Image,
            WorkingDir = WorkspacePath,
            Env = new[]
            {
                $"AGENT_TOKEN={sandboxId}",
                $"PORT={PodExecutorPort}",
                $"HOME={WorkspacePath}",
                $"WORKSPACE={WorkspacePath}",
            },
            HostConfig = new
            {
                NetworkMode = config.Network,
                Binds = new[] { $"eaos-agent-data-{agentId.ToString("N")[..8]}:{WorkspacePath}" },
                RestartPolicy = new { Name = "unless-stopped" },
            },
            ExposedPorts = new Dictionary<string, object>
            {
                [$"{PodExecutorPort}/tcp"] = new { },
            },
            Labels = new Dictionary<string, string>
            {
                ["app"] = "eaos-agent-runtime",
                ["managed-by"] = "eaos",
                ["agent-id"] = agentId.ToString(),
            },
        };
    }

    private static HttpClient CreateDockerClient(string socketPath)
    {
        return new HttpClient(new SocketsHttpHandler
        {
            ConnectCallback = async (_, ct) =>
            {
                var socket = new System.Net.Sockets.Socket(
                    System.Net.Sockets.AddressFamily.Unix,
                    System.Net.Sockets.SocketType.Stream,
                    System.Net.Sockets.ProtocolType.Unspecified);
                await socket.ConnectAsync(
                    new System.Net.Sockets.UnixDomainSocketEndPoint(socketPath),
                    ct);
                return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
            },
        })
        {
            BaseAddress = new Uri("http://localhost"),
        };
    }
}

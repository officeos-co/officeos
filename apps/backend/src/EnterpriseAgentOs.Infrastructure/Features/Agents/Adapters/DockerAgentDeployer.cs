namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class DockerAgentDeployer : IAgentDeployer
{
    private const int ZeroclawPort = 42617;

    private readonly HttpClient _docker;
    private readonly DockerConfig _config;
    private readonly ILogger<DockerAgentDeployer> _logger;

    public DockerAgentDeployer(DockerConfig config, ILogger<DockerAgentDeployer> logger)
    {
        _config = config;
        _logger = logger;
        _docker = new HttpClient(new SocketsHttpHandler
        {
            ConnectCallback = async (context, ct) =>
            {
                var socket = new System.Net.Sockets.Socket(
                    System.Net.Sockets.AddressFamily.Unix,
                    System.Net.Sockets.SocketType.Stream,
                    System.Net.Sockets.ProtocolType.Unspecified);
                await socket.ConnectAsync(
                    new System.Net.Sockets.UnixDomainSocketEndPoint(config.SocketPath), ct);
                return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
            },
        })
        {
            BaseAddress = new Uri("http://localhost"),
        };
    }

    private static string ContainerName(Guid id) => $"zeroclaw-{id.ToString("N")[..8]}";

    public async Task<AgentDeployment> DeployAsync(Guid agentId, CancellationToken ct = default)
    {
        var name = ContainerName(agentId);
        _logger.LogInformation("Deploying agent {AgentId} as container {Container} using image {Image} on network {Network}",
            agentId, name, _config.Image, _config.Network);

        var createBody = new
        {
            Image = _config.Image,
            Env = new[] { $"AGENT_TOKEN={agentId}" },
            HostConfig = new
            {
                NetworkMode = _config.Network,
                Binds = new[] { $"zeroclaw-data-{agentId.ToString("N")[..8]}:/home" },
                RestartPolicy = new { Name = "unless-stopped" },
            },
            ExposedPorts = new Dictionary<string, object>
            {
                [$"{ZeroclawPort}/tcp"] = new { },
            },
            Labels = new Dictionary<string, string>
            {
                ["app"] = "zeroclaw",
                ["managed-by"] = "eaos-v2",
                ["agent-id"] = agentId.ToString(),
            },
        };

        var json = JsonSerializer.Serialize(createBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var createResp = await _docker.PostAsync(
                $"/containers/create?name={name}", content, ct);

            if (!createResp.IsSuccessStatusCode)
            {
                var err = await createResp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Docker create failed for agent {AgentId}: {StatusCode} {Error}",
                    agentId, createResp.StatusCode, err);
                throw new InvalidOperationException(
                    $"Docker create failed ({createResp.StatusCode}): {err}");
            }

            _logger.LogDebug("Container {Container} created, starting...", name);

            var startResp = await _docker.PostAsync(
                $"/containers/{name}/start", null, ct);

            if (!startResp.IsSuccessStatusCode && startResp.StatusCode != System.Net.HttpStatusCode.NotModified)
            {
                var err = await startResp.Content.ReadAsStringAsync(ct);
                _logger.LogError("Docker start failed for agent {AgentId}: {StatusCode} {Error}",
                    agentId, startResp.StatusCode, err);
                throw new InvalidOperationException(
                    $"Docker start failed ({startResp.StatusCode}): {err}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Cannot connect to Docker daemon at {SocketPath} for agent {AgentId}",
                _config.SocketPath, agentId);
            throw new InvalidOperationException(
                $"Cannot connect to Docker daemon at '{_config.SocketPath}'. Is Docker running?", ex);
        }

        _logger.LogInformation("Deployed agent {AgentId} as container {Container}", agentId, name);

        var serviceUrl = $"ws://{name}:{ZeroclawPort}/ws";
        return new AgentDeployment(name, serviceUrl);
    }

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
            _logger.LogWarning(ex, "Failed to remove container {Container}", podName);
            return false;
        }
    }

    public async Task<string> GetStatusAsync(string podName, CancellationToken ct = default)
    {
        try
        {
            var resp = await _docker.GetAsync($"/containers/{podName}/json", ct);
            if (!resp.IsSuccessStatusCode) return "not_found";

            var doc = await resp.Content.ReadFromJsonAsync<JsonDocument>(ct);
            var state = doc!.RootElement.GetProperty("State");
            var status = state.GetProperty("Status").GetString();

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
            var resp = await _docker.GetAsync(
                $"/containers/{podName}/logs?stdout=true&stderr=true&tail={tailLines}", ct);
            return resp.IsSuccessStatusCode
                ? await resp.Content.ReadAsStringAsync(ct)
                : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

using OffceOs.Configuration;
using OffceOs.Features.AgentHarness.Domain;
using OffceOs.Common.Domain.Primitives;
namespace OffceOs.Features.AgentHarness.Infrastructure;

internal sealed class DockerAgentSandbox : IAgentSandbox, IAgentDeployer, IAgentRuntimeCleaner
{
    private const int PodExecutorPort = 42617;
    private const string WorkspacePath = "/workspace";
    private const string AppLabelValue = "eaos-agent-runtime";
    private const string ManagedByLabelValue = "eaos";

    private readonly HttpClient _httpClient;
    private readonly DockerConfig _dockerConfig;
    private readonly PodExecutorClient _podExecutorClient;
    private readonly IAgentWorkspaceStore _agentWorkspaceStore;

    public DockerAgentSandbox(
        DockerConfig config,
        PodExecutorClient executor,
        IAgentWorkspaceStore workspaceStore)
        : this(CreateDockerClient(config.SocketPath), config, executor, workspaceStore)
    {
    }

    internal DockerAgentSandbox(
        HttpClient docker,
        DockerConfig config,
        PodExecutorClient executor,
        IAgentWorkspaceStore workspaceStore)
    {
        _httpClient = docker;
        _dockerConfig = config;
        _podExecutorClient = executor;
        _agentWorkspaceStore = workspaceStore;
    }

    public async Task<AgentSandboxDeployment> CreateAsync(
        Guid agentId,
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
        using var content = JsonContent.Create(BuildCreateContainerBody(agentId, _dockerConfig));
        var createResponse = await _httpClient.PostAsync($"/containers/create?name={sandboxId}", content, ct);

        if (!createResponse.IsSuccessStatusCode && createResponse.StatusCode != HttpStatusCode.Conflict)
        {
            var error = await createResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Docker create failed ({createResponse.StatusCode}): {error}");
        }

        var startResponse = await _httpClient.PostAsync($"/containers/{sandboxId}/start", null, ct);
        if (!startResponse.IsSuccessStatusCode && startResponse.StatusCode != HttpStatusCode.NotModified)
        {
            var error = await startResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Docker start failed ({startResponse.StatusCode}): {error}");
        }

        var serviceUrl = await ResolveServiceUrlAsync(sandboxId, ct);
        if (!await _podExecutorClient.WaitUntilAvailableAsync(serviceUrl, TimeSpan.FromSeconds(60), ct))
            throw new InvalidOperationException($"Pod executor {sandboxId} did not become available.");

        await _agentWorkspaceStore.RestoreAsync(sandboxId, serviceUrl, ct);

        return new AgentDeployment(sandboxId, serviceUrl);
    }

    public Task<AgentResult<AgentSandboxCommandResult>> ExecuteAsync(
        string sandboxId,
        string serviceUrl,
        string command,
        TimeSpan timeout,
        CancellationToken ct = default)
        => _podExecutorClient.ExecuteAsync(sandboxId, serviceUrl, command, timeout, ct);

    public Task<AgentResult<string>> ReadFileAsync(
        string sandboxId,
        string serviceUrl,
        string path,
        CancellationToken ct = default)
        => _podExecutorClient.ReadFileAsync(sandboxId, serviceUrl, path, ct);

    public Task<AgentResult<bool>> WriteFileAsync(
        string sandboxId,
        string serviceUrl,
        string path,
        string content,
        CancellationToken ct = default)
        => _podExecutorClient.WriteFileAsync(sandboxId, serviceUrl, path, content, ct);

    public Task<bool> TerminateAsync(string sandboxId, CancellationToken ct = default)
        => RemoveAsync(sandboxId, ct);

    public async Task<bool> RemoveAsync(string podName, CancellationToken ct = default)
    {
        try
        {
            await _agentWorkspaceStore.CheckpointAsync(podName, await ResolveServiceUrlAsync(podName, ct), ct);
            await _httpClient.PostAsync($"/containers/{podName}/stop", null, ct);
            await _httpClient.DeleteAsync($"/containers/{podName}?force=true&v=true", ct);
            await DeleteVolumesAsync(podName, ct);
            return true;
        }
        catch (Exception ex)
        {
            _ = ex;
            return false;
        }
    }

    public async Task<string> GetStatusAsync(string podName, CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/containers/{podName}/json", ct);
            if (!response.IsSuccessStatusCode)
                return "not_found";

            var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
            var status = doc!.RootElement.GetProperty("State").GetProperty("Status").GetString();

            return status switch
            {
                "running" => "running",
                "created" => "booting",
                "restarting" => "restarting",
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
            var response = await _httpClient.GetAsync(
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

    public async Task<AgentRuntimeCleanupResult> CleanupUnusedAsync(
        IReadOnlySet<Guid> activeAgentIds,
        CancellationToken ct = default)
    {
        var deletedContainers = await DeleteUnusedContainersAsync(activeAgentIds, ct);
        var deletedVolumes = await DeleteUnusedVolumesAsync(activeAgentIds, ct);

        return new AgentRuntimeCleanupResult(deletedContainers, 0, deletedVolumes);
    }

    internal static string SandboxName(Guid id) => $"eaos-agent-{id.ToString("N")[..8]}";

    internal static string ServiceUrl(string sandboxId) => $"http://{sandboxId}:{PodExecutorPort}";

    internal static string HostServiceUrl(string host, string hostPort) => $"http://{host}:{hostPort}";

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
            HostConfig = BuildHostConfig(config),
            ExposedPorts = new Dictionary<string, object>
            {
                [$"{PodExecutorPort}/tcp"] = new { },
            },
            Labels = new Dictionary<string, string>
            {
                ["app"] = AppLabelValue,
                ["managed-by"] = ManagedByLabelValue,
                ["agent-id"] = agentId.ToString(),
            },
        };
    }

    private static Dictionary<string, object> BuildHostConfig(DockerConfig config)
    {
        var hostConfig = new Dictionary<string, object>
        {
            ["NetworkMode"] = config.Network,
            ["RestartPolicy"] = new { Name = "unless-stopped" },
        };

        if (config.PublishHostPort)
        {
            hostConfig["PortBindings"] = new Dictionary<string, object[]>
            {
                [$"{PodExecutorPort}/tcp"] =
                [
                    new
                    {
                        HostIp = string.IsNullOrWhiteSpace(config.Host) ? "127.0.0.1" : config.Host,
                        HostPort = string.Empty,
                    },
                ],
            };
        }

        return hostConfig;
    }

    private async Task<string> ResolveServiceUrlAsync(string sandboxId, CancellationToken ct)
    {
        if (!_dockerConfig.PublishHostPort)
            return ServiceUrl(sandboxId);

        var hostPort = await GetPublishedPortAsync(sandboxId, ct);
        return string.IsNullOrWhiteSpace(hostPort)
            ? ServiceUrl(sandboxId)
            : HostServiceUrl(_dockerConfig.Host, hostPort);
    }

    private async Task<string?> GetPublishedPortAsync(string sandboxId, CancellationToken ct)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/containers/{sandboxId}/json", ct);
            if (!response.IsSuccessStatusCode)
                return null;

            var doc = await response.Content.ReadFromJsonAsync<JsonDocument>(ct);
            var ports = doc?.RootElement
                .GetProperty("NetworkSettings")
                .GetProperty("Ports");
            if (ports is null)
                return null;

            var portKey = $"{PodExecutorPort}/tcp";
            if (!ports.Value.TryGetProperty(portKey, out var bindings)
                || bindings.ValueKind != JsonValueKind.Array
                || bindings.GetArrayLength() == 0)
                return null;

            var first = bindings[0];
            return first.TryGetProperty("HostPort", out var hostPort)
                   && hostPort.ValueKind == JsonValueKind.String
                ? hostPort.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<int> DeleteUnusedContainersAsync(IReadOnlySet<Guid> activeAgentIds, CancellationToken ct)
    {
        var activeSandboxNames = ActiveSandboxNames(activeAgentIds);
        var response = await _httpClient.GetAsync("/containers/json?all=true", ct);
        if (!response.IsSuccessStatusCode)
            return 0;

        var containers = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (containers.ValueKind != JsonValueKind.Array)
            return 0;

        var deleted = 0;
        foreach (var container in containers.EnumerateArray())
        {
            var name = GetDockerName(container);
            if (string.IsNullOrWhiteSpace(name)
                || !IsUnusedRuntimeResource(container, name, activeAgentIds, activeSandboxNames))
                continue;

            await TryDockerDeleteAsync(() => _httpClient.PostAsync($"/containers/{name}/stop", null, ct));
            await TryDockerDeleteAsync(() => _httpClient.DeleteAsync($"/containers/{name}?force=true&v=true", ct));
            deleted++;
        }

        return deleted;
    }

    private async Task<int> DeleteUnusedVolumesAsync(IReadOnlySet<Guid> activeAgentIds, CancellationToken ct)
    {
        var activeSandboxNames = ActiveSandboxNames(activeAgentIds);
        var response = await _httpClient.GetAsync("/volumes", ct);
        if (!response.IsSuccessStatusCode)
            return 0;

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (!body.TryGetProperty("Volumes", out var volumes) || volumes.ValueKind != JsonValueKind.Array)
            return 0;

        var deleted = 0;
        foreach (var volume in volumes.EnumerateArray())
        {
            if (!volume.TryGetProperty("Name", out var nameProperty)
                || nameProperty.ValueKind != JsonValueKind.String
                || !IsUnusedRuntimeResource(volume, nameProperty.GetString(), activeAgentIds, activeSandboxNames))
                continue;

            await TryDockerDeleteAsync(() => _httpClient.DeleteAsync($"/volumes/{Uri.EscapeDataString(nameProperty.GetString()!)}?force=true", ct));
            deleted++;
        }

        return deleted;
    }

    private async Task DeleteVolumesAsync(string sandboxId, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync("/volumes", ct);
        if (!response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (!body.TryGetProperty("Volumes", out var volumes) || volumes.ValueKind != JsonValueKind.Array)
            return;

        foreach (var volume in volumes.EnumerateArray())
        {
            if (!volume.TryGetProperty("Name", out var nameProperty)
                || nameProperty.ValueKind != JsonValueKind.String
                || !IsSandboxStorageName(nameProperty.GetString(), sandboxId))
                continue;

            await TryDockerDeleteAsync(() => _httpClient.DeleteAsync($"/volumes/{Uri.EscapeDataString(nameProperty.GetString()!)}?force=true", ct));
        }
    }

    internal static string RuntimeFilterQuery()
    {
        var filters = JsonSerializer.Serialize(new Dictionary<string, string[]>
        {
            ["label"] = [$"managed-by={ManagedByLabelValue}", $"app={AppLabelValue}"],
        });
        return Uri.EscapeDataString(filters);
    }

    private static bool TryGetAgentId(JsonElement resource, out Guid agentId)
    {
        agentId = default;
        if (!resource.TryGetProperty("Labels", out var labels)
            || labels.ValueKind != JsonValueKind.Object
            || !labels.TryGetProperty("agent-id", out var agentIdProperty)
            || agentIdProperty.ValueKind != JsonValueKind.String)
            return false;

        return Guid.TryParse(agentIdProperty.GetString(), out agentId);
    }

    private static string? GetDockerName(JsonElement container)
    {
        if (!container.TryGetProperty("Names", out var names)
            || names.ValueKind != JsonValueKind.Array
            || names.GetArrayLength() == 0)
            return null;

        var name = names[0].GetString();
        return string.IsNullOrWhiteSpace(name)
            ? null
            : name.TrimStart('/');
    }

    private static bool IsSandboxStorageName(string? name, string sandboxId)
        => name == sandboxId || name?.StartsWith($"{sandboxId}-", StringComparison.Ordinal) == true;

    private static bool IsUnusedRuntimeResource(
        JsonElement resource,
        string? name,
        IReadOnlySet<Guid> activeAgentIds,
        IReadOnlySet<string> activeSandboxNames)
    {
        if (!IsRuntimeLabels(resource))
            return false;

        if (TryGetAgentId(resource, out var agentId))
            return !activeAgentIds.Contains(agentId);

        return IsUnusedSandboxName(name, activeSandboxNames);
    }

    private static bool IsRuntimeLabels(JsonElement resource)
    {
        if (!resource.TryGetProperty("Labels", out var labels) || labels.ValueKind != JsonValueKind.Object)
            return false;

        return labels.TryGetProperty("managed-by", out var managedBy)
               && managedBy.GetString() == ManagedByLabelValue
               && labels.TryGetProperty("app", out var app)
               && app.GetString() == AppLabelValue;
    }

    private static bool IsUnusedSandboxName(string? name, IReadOnlySet<string> activeSandboxNames)
        => name?.StartsWith("eaos-agent-", StringComparison.Ordinal) == true
           && activeSandboxNames.All(active => !IsSandboxStorageName(name, active));

    private static HashSet<string> ActiveSandboxNames(IReadOnlySet<Guid> activeAgentIds)
        => activeAgentIds.Select(SandboxName).ToHashSet(StringComparer.Ordinal);

    private static async Task TryDockerDeleteAsync(Func<Task<HttpResponseMessage>> operation)
    {
        try
        {
            using var response = await operation();
        }
        catch
        {
            // Runtime cleanup is best-effort; resources may already be gone.
        }
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

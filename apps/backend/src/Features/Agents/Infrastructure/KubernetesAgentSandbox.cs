namespace OffceOs.Infrastructure.Features.Agents;

internal sealed class KubernetesAgentSandbox : IAgentSandbox, IAgentDeployer, IAgentRuntimeCleaner
{
    internal const int PodExecutorPort = 42617;
    internal const string WorkspacePath = "/workspace";
    private const string AppLabelValue = "eaos-agent-runtime";
    private const string ManagedByLabelValue = "eaos";

    private readonly IKubernetes _kubernetes;
    private readonly KubernetesConfig _kubernetesConfig;
    private readonly PodExecutorClient _podExecutorClient;
    private readonly IAgentWorkspaceStore _agentWorkspaceStore;
    private readonly ILogger<KubernetesAgentSandbox> _logger;

    public KubernetesAgentSandbox(
        IKubernetes kubernetes,
        KubernetesConfig config,
        PodExecutorClient executor,
        IAgentWorkspaceStore workspaceStore,
        ILogger<KubernetesAgentSandbox> logger)
    {
        _kubernetes = kubernetes;
        _kubernetesConfig = config;
        _podExecutorClient = executor;
        _agentWorkspaceStore = workspaceStore;
        _logger = logger;
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
        var labels = Labels(agentId);

        await _kubernetes.CoreV1.CreateNamespacedPodAsync(
            BuildPod(agentId, _kubernetesConfig.Image, labels),
            _kubernetesConfig.Namespace,
            cancellationToken: ct);

        await _kubernetes.CoreV1.CreateNamespacedServiceAsync(
            BuildService(agentId, labels),
            _kubernetesConfig.Namespace,
            cancellationToken: ct);

        var serviceUrl = ServiceUrl(sandboxId, _kubernetesConfig.Namespace);
        if (!await _podExecutorClient.WaitUntilAvailableAsync(serviceUrl, TimeSpan.FromSeconds(60), ct))
            throw new InvalidOperationException($"Pod executor {sandboxId} did not become available.");

        await _agentWorkspaceStore.RestoreAsync(sandboxId, serviceUrl, ct);

        _logger.LogInformation("Deployed agent {AgentId} as pod executor {SandboxId}", agentId, sandboxId);
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
            await _agentWorkspaceStore.CheckpointAsync(podName, ServiceUrl(podName, _kubernetesConfig.Namespace), ct);
            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedPodAsync(
                podName,
                _kubernetesConfig.Namespace,
                cancellationToken: ct));
            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedServiceAsync(
                podName,
                _kubernetesConfig.Namespace,
                cancellationToken: ct));
            await DeletePersistentVolumeClaimsAsync(podName, ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove pod executor {SandboxId}", podName);
            return false;
        }
    }

    public async Task<string> GetStatusAsync(string podName, CancellationToken ct = default)
    {
        try
        {
            var pod = await _kubernetes.CoreV1.ReadNamespacedPodAsync(
                podName,
                _kubernetesConfig.Namespace,
                cancellationToken: ct);

            return pod.Status?.Phase?.ToLowerInvariant() switch
            {
                "running" => "running",
                "pending" => "booting",
                "succeeded" => "stopped",
                "failed" => "failed",
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
            using var stream = await _kubernetes.CoreV1.ReadNamespacedPodLogAsync(
                podName,
                _kubernetesConfig.Namespace,
                tailLines: tailLines,
                cancellationToken: ct);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(ct);
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
        var deletedPods = await DeleteUnusedPodsAsync(activeAgentIds, ct);
        var deletedServices = await DeleteUnusedServicesAsync(activeAgentIds, ct);
        var deletedClaims = await DeleteUnusedPersistentVolumeClaimsAsync(activeAgentIds, ct);

        return new AgentRuntimeCleanupResult(deletedPods, deletedServices, deletedClaims);
    }

    internal static string SandboxName(Guid id) => $"eaos-agent-{id.ToString("N")[..8]}";

    internal static string ServiceUrl(string sandboxId, string namespaceName)
        => $"http://{sandboxId}.{namespaceName}.svc.cluster.local:{PodExecutorPort}";

    internal static V1Pod BuildPod(
        Guid agentId,
        string image,
        IReadOnlyDictionary<string, string>? labels = null)
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = SandboxName(agentId),
                Labels = ToDictionary(labels ?? Labels(agentId)),
            },
            Spec = new V1PodSpec
            {
                RestartPolicy = "Always",
                Containers =
                [
                    new V1Container
                    {
                        Name = "pod-executor",
                        Image = image,
                        WorkingDir = WorkspacePath,
                        Ports = [new V1ContainerPort(PodExecutorPort)],
                        Env =
                        [
                            new V1EnvVar("AGENT_TOKEN", SandboxName(agentId)),
                            new V1EnvVar("PORT", PodExecutorPort.ToString()),
                            new V1EnvVar("HOME", WorkspacePath),
                            new V1EnvVar("WORKSPACE", WorkspacePath),
                        ],
                        Resources = new V1ResourceRequirements
                        {
                            Requests = new Dictionary<string, ResourceQuantity>
                            {
                                ["memory"] = new("64Mi"),
                                ["cpu"] = new("100m"),
                            },
                            Limits = new Dictionary<string, ResourceQuantity>
                            {
                                ["memory"] = new("512Mi"),
                                ["cpu"] = new("2"),
                            },
                        },
                        ReadinessProbe = HealthProbe(),
                        LivenessProbe = HealthProbe(),
                        VolumeMounts = [new V1VolumeMount(WorkspacePath, "workspace")],
                    },
                ],
                Volumes =
                [
                    new V1Volume
                    {
                        Name = "workspace",
                        EmptyDir = new V1EmptyDirVolumeSource(),
                    },
                ],
            },
        };

    internal static V1Service BuildService(
        Guid agentId,
        IReadOnlyDictionary<string, string>? labels = null)
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = SandboxName(agentId),
                Labels = ToDictionary(labels ?? Labels(agentId)),
            },
            Spec = new V1ServiceSpec
            {
                Type = "ClusterIP",
                Selector = new Dictionary<string, string>
                {
                    ["app"] = "eaos-agent-runtime",
                    ["agent-id"] = agentId.ToString(),
                },
                Ports =
                [
                    new V1ServicePort(PodExecutorPort)
                    {
                        TargetPort = PodExecutorPort,
                    },
                ],
            },
        };

    private static IReadOnlyDictionary<string, string> Labels(Guid agentId)
        => new Dictionary<string, string>
        {
            ["app"] = AppLabelValue,
            ["managed-by"] = ManagedByLabelValue,
            ["agent-id"] = agentId.ToString(),
        };

    internal static string RuntimeLabelSelector()
        => $"managed-by={ManagedByLabelValue},app={AppLabelValue}";

    private static Dictionary<string, string> ToDictionary(IReadOnlyDictionary<string, string> labels)
        => new(labels);

    private static V1Probe HealthProbe()
        => new()
        {
            HttpGet = new V1HTTPGetAction
            {
                Path = "/health",
                Port = PodExecutorPort,
            },
            InitialDelaySeconds = 5,
            PeriodSeconds = 10,
            FailureThreshold = 3,
        };

    private async Task<int> DeleteUnusedPodsAsync(IReadOnlySet<Guid> activeAgentIds, CancellationToken ct)
    {
        var activeSandboxNames = ActiveSandboxNames(activeAgentIds);
        var pods = await _kubernetes.CoreV1.ListNamespacedPodAsync(
            _kubernetesConfig.Namespace,
            cancellationToken: ct);

        var deleted = 0;
        foreach (var pod in pods.Items)
        {
            var name = pod.Metadata?.Name;
            if (string.IsNullOrWhiteSpace(name)
                || !IsUnusedRuntimeResource(pod.Metadata, activeAgentIds, activeSandboxNames))
                continue;

            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedPodAsync(
                name,
                _kubernetesConfig.Namespace,
                cancellationToken: ct));
            deleted++;
        }

        return deleted;
    }

    private async Task<int> DeleteUnusedServicesAsync(IReadOnlySet<Guid> activeAgentIds, CancellationToken ct)
    {
        var activeSandboxNames = ActiveSandboxNames(activeAgentIds);
        var services = await _kubernetes.CoreV1.ListNamespacedServiceAsync(
            _kubernetesConfig.Namespace,
            cancellationToken: ct);

        var deleted = 0;
        foreach (var service in services.Items)
        {
            var name = service.Metadata?.Name;
            if (string.IsNullOrWhiteSpace(name)
                || !IsUnusedRuntimeResource(service.Metadata, activeAgentIds, activeSandboxNames))
                continue;

            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedServiceAsync(
                name,
                _kubernetesConfig.Namespace,
                cancellationToken: ct));
            deleted++;
        }

        return deleted;
    }

    private async Task<int> DeleteUnusedPersistentVolumeClaimsAsync(
        IReadOnlySet<Guid> activeAgentIds,
        CancellationToken ct)
    {
        var activeSandboxNames = ActiveSandboxNames(activeAgentIds);
        var claims = await _kubernetes.CoreV1.ListNamespacedPersistentVolumeClaimAsync(
            _kubernetesConfig.Namespace,
            cancellationToken: ct);

        var deleted = 0;
        foreach (var claim in claims.Items)
        {
            var name = claim.Metadata?.Name;
            if (string.IsNullOrWhiteSpace(name)
                || !IsUnusedRuntimeResource(claim.Metadata, activeAgentIds, activeSandboxNames))
                continue;

            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedPersistentVolumeClaimAsync(
                name,
                _kubernetesConfig.Namespace,
                cancellationToken: ct));
            deleted++;
        }

        return deleted;
    }

    private async Task DeletePersistentVolumeClaimsAsync(string sandboxId, CancellationToken ct)
    {
        var claims = await _kubernetes.CoreV1.ListNamespacedPersistentVolumeClaimAsync(
            _kubernetesConfig.Namespace,
            cancellationToken: ct);

        foreach (var claim in claims.Items.Where(c => IsSandboxStorageName(c.Metadata?.Name, sandboxId)))
        {
            var name = claim.Metadata?.Name;
            if (string.IsNullOrWhiteSpace(name))
                continue;

            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedPersistentVolumeClaimAsync(
                name,
                _kubernetesConfig.Namespace,
                cancellationToken: ct));
        }
    }

    private static bool IsUnusedRuntimeResource(
        V1ObjectMeta? metadata,
        IReadOnlySet<Guid> activeAgentIds,
        IReadOnlySet<string> activeSandboxNames)
    {
        if (metadata is null)
            return false;

        var hasRuntimeLabels = metadata.Labels is not null && IsRuntimeLabels(metadata.Labels);
        if (!hasRuntimeLabels)
            return false;

        if (metadata.Labels is not null
            && metadata.Labels.TryGetValue("agent-id", out var agentIdText)
            && Guid.TryParse(agentIdText, out var agentId))
            return !activeAgentIds.Contains(agentId);

        return IsUnusedSandboxName(metadata.Name, activeSandboxNames);
    }

    private static bool IsSandboxStorageName(string? name, string sandboxId)
        => name == sandboxId || name?.StartsWith($"{sandboxId}-", StringComparison.Ordinal) == true;

    private static bool IsUnusedSandboxName(string? name, IReadOnlySet<string> activeSandboxNames)
        => name?.StartsWith("eaos-agent-", StringComparison.Ordinal) == true
           && activeSandboxNames.All(active => !IsSandboxStorageName(name, active));

    private static bool IsRuntimeLabels(IDictionary<string, string> labels)
        => labels.TryGetValue("managed-by", out var managedBy)
           && managedBy == ManagedByLabelValue
           && labels.TryGetValue("app", out var app)
           && app == AppLabelValue;

    private static HashSet<string> ActiveSandboxNames(IReadOnlySet<Guid> activeAgentIds)
        => activeAgentIds.Select(SandboxName).ToHashSet(StringComparer.Ordinal);

    private static async Task TryDeleteAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch
        {
            // Runtime cleanup is best-effort; deletion events may race each other.
        }
    }
}

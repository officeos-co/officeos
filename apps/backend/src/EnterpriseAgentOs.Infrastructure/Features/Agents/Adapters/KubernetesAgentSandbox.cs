namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class KubernetesAgentSandbox : IAgentSandbox, IAgentDeployer
{
    internal const int PodExecutorPort = 42617;
    internal const string WorkspacePath = "/workspace";

    private readonly IKubernetes _kubernetes;
    private readonly KubernetesConfig _config;
    private readonly PodExecutorClient _executor;
    private readonly IAgentWorkspaceStore _workspaceStore;
    private readonly ILogger<KubernetesAgentSandbox> _logger;

    public KubernetesAgentSandbox(
        IKubernetes kubernetes,
        KubernetesConfig config,
        PodExecutorClient executor,
        IAgentWorkspaceStore workspaceStore,
        ILogger<KubernetesAgentSandbox> logger)
    {
        _kubernetes = kubernetes;
        _config = config;
        _executor = executor;
        _workspaceStore = workspaceStore;
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
            BuildPod(agentId, _config.Image, labels),
            _config.Namespace,
            cancellationToken: ct);

        await _kubernetes.CoreV1.CreateNamespacedServiceAsync(
            BuildService(agentId, labels),
            _config.Namespace,
            cancellationToken: ct);

        var serviceUrl = ServiceUrl(sandboxId, _config.Namespace);
        if (!await _executor.WaitUntilAvailableAsync(serviceUrl, TimeSpan.FromSeconds(60), ct))
            throw new InvalidOperationException($"Pod executor {sandboxId} did not become available.");

        await _workspaceStore.RestoreAsync(sandboxId, serviceUrl, ct);

        _logger.LogInformation("Deployed agent {AgentId} as pod executor {SandboxId}", agentId, sandboxId);
        return new AgentDeployment(sandboxId, serviceUrl);
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
            await _workspaceStore.CheckpointAsync(podName, ServiceUrl(podName, _config.Namespace), ct);
            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedPodAsync(
                podName,
                _config.Namespace,
                cancellationToken: ct));
            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedServiceAsync(
                podName,
                _config.Namespace,
                cancellationToken: ct));

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
                _config.Namespace,
                cancellationToken: ct);

            return pod.Status?.Phase?.ToLowerInvariant() switch
            {
                "running" => "running",
                "pending" => "pending",
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
                _config.Namespace,
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
            ["app"] = "eaos-agent-runtime",
            ["managed-by"] = "eaos",
            ["agent-id"] = agentId.ToString(),
        };

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

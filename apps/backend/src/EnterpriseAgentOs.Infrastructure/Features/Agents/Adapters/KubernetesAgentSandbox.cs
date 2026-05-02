namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class KubernetesAgentSandbox : IAgentSandbox, IAgentDeployer
{
    internal const int PodExecutorPort = 42617;
    internal const string WorkspacePath = "/workspace";
    private const string StorageSize = "1Gi";

    private readonly IKubernetes _kubernetes;
    private readonly KubernetesConfig _config;
    private readonly PodExecutorClient _executor;
    private readonly ILogger<KubernetesAgentSandbox> _logger;

    public KubernetesAgentSandbox(
        IKubernetes kubernetes,
        KubernetesConfig config,
        PodExecutorClient executor,
        ILogger<KubernetesAgentSandbox> logger)
    {
        _kubernetes = kubernetes;
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
        var labels = Labels(agentId);

        await _kubernetes.CoreV1.CreateNamespacedPersistentVolumeClaimAsync(
            BuildPersistentVolumeClaim(agentId, labels),
            _config.Namespace,
            cancellationToken: ct);

        await _kubernetes.CoreV1.CreateNamespacedPodAsync(
            BuildPod(agentId, _config.Image, labels),
            _config.Namespace,
            cancellationToken: ct);

        await _kubernetes.CoreV1.CreateNamespacedServiceAsync(
            BuildService(agentId, labels),
            _config.Namespace,
            cancellationToken: ct);

        _logger.LogInformation("Deployed agent {AgentId} as pod executor {SandboxId}", agentId, sandboxId);
        return new AgentDeployment(sandboxId, ServiceUrl(sandboxId, _config.Namespace));
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
            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedPodAsync(
                podName,
                _config.Namespace,
                cancellationToken: ct));
            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedServiceAsync(
                podName,
                _config.Namespace,
                cancellationToken: ct));
            await TryDeleteAsync(() => _kubernetes.CoreV1.DeleteNamespacedPersistentVolumeClaimAsync(
                PersistentVolumeClaimName(podName),
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

    internal static string PersistentVolumeClaimName(Guid id) => $"eaos-agent-data-{id.ToString("N")[..8]}";

    internal static string PersistentVolumeClaimName(string sandboxId)
        => sandboxId.StartsWith("eaos-agent-", StringComparison.Ordinal)
            ? "eaos-agent-data-" + sandboxId["eaos-agent-".Length..]
            : sandboxId.StartsWith("zeroclaw-", StringComparison.Ordinal)
                ? "zeroclaw-data-" + sandboxId["zeroclaw-".Length..]
                : sandboxId;

    internal static string ServiceUrl(string sandboxId, string namespaceName)
        => $"http://{sandboxId}.{namespaceName}.svc.cluster.local:{PodExecutorPort}";

    internal static V1PersistentVolumeClaim BuildPersistentVolumeClaim(
        Guid agentId,
        IReadOnlyDictionary<string, string>? labels = null)
        => new()
        {
            Metadata = new V1ObjectMeta
            {
                Name = PersistentVolumeClaimName(agentId),
                Labels = ToDictionary(labels ?? Labels(agentId)),
            },
            Spec = new V1PersistentVolumeClaimSpec
            {
                AccessModes = ["ReadWriteOnce"],
                Resources = new V1VolumeResourceRequirements
                {
                    Requests = new Dictionary<string, ResourceQuantity>
                    {
                        ["storage"] = new(StorageSize),
                    },
                },
            },
        };

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
                        PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource(PersistentVolumeClaimName(agentId)),
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

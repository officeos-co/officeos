using k8s;
using k8s.Models;
using EnterpriseAgentOs.Api.Properties;

namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed class KubernetesAgentDeployer : IAgentDeployer
{
    private const int ZeroclawPort = 42617;
    private const string StorageSize = "1Gi";

    private readonly IKubernetes _k8s;
    private readonly ILogger<KubernetesAgentDeployer> _logger;
    private readonly string _namespace;
    private readonly string _image;

    public KubernetesAgentDeployer(IKubernetes k8s, ILogger<KubernetesAgentDeployer> logger)
    {
        _k8s = k8s;
        _logger = logger;
        _namespace = ValueManager.GetValue<string>("KubernetesNamespace");
        _image = ValueManager.GetValue<string>("ZeroclawImage");
    }

    private static string PodName(Guid id) => $"zeroclaw-{id.ToString("N").Substring(0, 8)}";
    private static string PvcName(Guid id) => $"zeroclaw-data-{id.ToString("N").Substring(0, 8)}";
    private string ServiceUrl(Guid id) =>
        $"ws://{PodName(id)}.{_namespace}.svc.cluster.local:{ZeroclawPort}/ws/chat";

    public async Task<AgentDeployment> DeployAsync(
        Guid agentId,
        string provider,
        string apiKey,
        string? model,
        CancellationToken ct = default)
    {
        var pod = PodName(agentId);
        var pvc = PvcName(agentId);
        var labels = new Dictionary<string, string>
        {
            ["app"] = "zeroclaw",
            ["managed-by"] = "eaos-v2",
            ["agent-id"] = agentId.ToString(),
        };

        var pvcManifest = new V1PersistentVolumeClaim
        {
            Metadata = new V1ObjectMeta { Name = pvc, Labels = labels },
            Spec = new V1PersistentVolumeClaimSpec
            {
                AccessModes = new[] { "ReadWriteOnce" },
                Resources = new V1VolumeResourceRequirements
                {
                    Requests = new Dictionary<string, ResourceQuantity>
                    {
                        ["storage"] = new ResourceQuantity(StorageSize),
                    },
                },
            },
        };
        await _k8s.CoreV1.CreateNamespacedPersistentVolumeClaimAsync(pvcManifest, _namespace, cancellationToken: ct);

        var env = new List<V1EnvVar>
        {
            new("API_KEY", (apiKey ?? string.Empty).Trim()),
            new("PROVIDER", provider),
            new("ZEROCLAW_GATEWAY_PORT", ZeroclawPort.ToString()),
            new("ZEROCLAW_GATEWAY_HOST", "0.0.0.0"),
            new("ZEROCLAW_ALLOW_PUBLIC_BIND", "true"),
            new("ZEROCLAW_REQUIRE_PAIRING", "false"),
        };
        if (!string.IsNullOrWhiteSpace(model))
        {
            env.Add(new V1EnvVar("ZEROCLAW_MODEL", model));
        }

        var podManifest = new V1Pod
        {
            Metadata = new V1ObjectMeta { Name = pod, Labels = labels },
            Spec = new V1PodSpec
            {
                RestartPolicy = "Always",
                Containers = new[]
                {
                    new V1Container
                    {
                        Name = "zeroclaw",
                        Image = _image,
                        Command = new[] { "sh", "-c", "zeroclaw daemon" },
                        Ports = new[] { new V1ContainerPort(ZeroclawPort) },
                        Env = env,
                        Resources = new V1ResourceRequirements
                        {
                            Requests = new Dictionary<string, ResourceQuantity>
                            {
                                ["memory"] = new ResourceQuantity("64Mi"),
                                ["cpu"] = new ResourceQuantity("100m"),
                            },
                            Limits = new Dictionary<string, ResourceQuantity>
                            {
                                ["memory"] = new ResourceQuantity("512Mi"),
                                ["cpu"] = new ResourceQuantity("2"),
                            },
                        },
                        VolumeMounts = new[]
                        {
                            new V1VolumeMount("/zeroclaw-data", "zeroclaw-data"),
                        },
                    },
                },
                Volumes = new[]
                {
                    new V1Volume
                    {
                        Name = "zeroclaw-data",
                        PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource(pvc),
                    },
                },
            },
        };
        await _k8s.CoreV1.CreateNamespacedPodAsync(podManifest, _namespace, cancellationToken: ct);

        var svcManifest = new V1Service
        {
            Metadata = new V1ObjectMeta { Name = pod, Labels = labels },
            Spec = new V1ServiceSpec
            {
                Type = "ClusterIP",
                Selector = new Dictionary<string, string>
                {
                    ["app"] = "zeroclaw",
                    ["agent-id"] = agentId.ToString(),
                },
                Ports = new[]
                {
                    new V1ServicePort(ZeroclawPort) { TargetPort = ZeroclawPort },
                },
            },
        };
        await _k8s.CoreV1.CreateNamespacedServiceAsync(svcManifest, _namespace, cancellationToken: ct);

        _logger.LogInformation("Deployed agent {AgentId} as pod {Pod}", agentId, pod);
        return new AgentDeployment(pod, ServiceUrl(agentId));
    }

    public async Task<bool> RemoveAsync(string podName, CancellationToken ct = default)
    {
        try
        {
            await TryDelete(() => _k8s.CoreV1.DeleteNamespacedPodAsync(podName, _namespace, cancellationToken: ct));
            await TryDelete(() => _k8s.CoreV1.DeleteNamespacedServiceAsync(podName, _namespace, cancellationToken: ct));
            var pvc = podName.StartsWith("zeroclaw-")
                ? "zeroclaw-data-" + podName.Substring("zeroclaw-".Length)
                : podName;
            await TryDelete(() => _k8s.CoreV1.DeleteNamespacedPersistentVolumeClaimAsync(pvc, _namespace, cancellationToken: ct));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove pod {Pod}", podName);
            return false;
        }
    }

    public async Task<string> GetStatusAsync(string podName, CancellationToken ct = default)
    {
        try
        {
            var pod = await _k8s.CoreV1.ReadNamespacedPodAsync(podName, _namespace, cancellationToken: ct);
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
            using var stream = await _k8s.CoreV1.ReadNamespacedPodLogAsync(
                podName, _namespace, tailLines: tailLines, cancellationToken: ct);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync(ct);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static async Task TryDelete(Func<Task> op)
    {
        try { await op(); } catch { /* swallow */ }
    }
}

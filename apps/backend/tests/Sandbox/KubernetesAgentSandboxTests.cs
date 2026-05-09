using OffceOs.Infrastructure.Features.Agents;
using Xunit;

namespace OffceOs.Tests.Sandbox;

public sealed class KubernetesAgentSandboxTests
{
    private static readonly Guid AgentId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void BuildPod_uses_pod_executor_image_workspace_and_token()
    {
        var pod = KubernetesAgentSandbox.BuildPod(AgentId, "repo/pod-executor:test");

        Assert.Equal("eaos-agent-11111111", pod.Metadata.Name);
        Assert.Equal("eaos-agent-runtime", pod.Metadata.Labels["app"]);
        Assert.Equal("eaos", pod.Metadata.Labels["managed-by"]);
        Assert.Equal(AgentId.ToString(), pod.Metadata.Labels["agent-id"]);

        var container = Assert.Single(pod.Spec.Containers);
        Assert.Equal("pod-executor", container.Name);
        Assert.Equal("repo/pod-executor:test", container.Image);
        Assert.Equal("/workspace", container.WorkingDir);
        Assert.Contains(container.Env, env => env.Name == "AGENT_TOKEN" && env.Value == "eaos-agent-11111111");
        Assert.Contains(container.Env, env => env.Name == "WORKSPACE" && env.Value == "/workspace");
        Assert.Equal("/workspace", Assert.Single(container.VolumeMounts).MountPath);
        Assert.NotNull(Assert.Single(pod.Spec.Volumes).EmptyDir);
    }

    [Fact]
    public void BuildService_routes_to_matching_agent_pod()
    {
        var service = KubernetesAgentSandbox.BuildService(AgentId);

        Assert.Equal("eaos-agent-11111111", service.Metadata.Name);
        Assert.Equal("eaos", service.Metadata.Labels["managed-by"]);
        Assert.Equal("ClusterIP", service.Spec.Type);
        Assert.Equal("eaos-agent-runtime", service.Spec.Selector["app"]);
        Assert.Equal(AgentId.ToString(), service.Spec.Selector["agent-id"]);
        Assert.Equal(42617, Assert.Single(service.Spec.Ports).Port);
    }

    [Fact]
    public void RuntimeLabelSelector_targets_only_eaos_agent_runtimes()
    {
        Assert.Equal("managed-by=eaos,app=eaos-agent-runtime", KubernetesAgentSandbox.RuntimeLabelSelector());
    }

    [Fact]
    public void ServiceUrl_points_to_rest_toolbox_base_url()
    {
        Assert.Equal(
            "http://eaos-agent-11111111.default.svc.cluster.local:42617",
            KubernetesAgentSandbox.ServiceUrl("eaos-agent-11111111", "default"));
    }
}

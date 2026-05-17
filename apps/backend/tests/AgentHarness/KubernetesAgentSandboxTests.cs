using OffceOs.Features.AgentHarness.Infrastructure;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.AgentHarness;

public sealed class KubernetesAgentSandboxTests
{
    [Fact]
    public void BuildPod_uses_pod_executor_image_workspace_and_token()
    {
        var pod = KubernetesAgentSandbox.BuildPod(TestIds.SandboxAgentId, "repo/pod-executor:test");

        Assert.Equal("eaos-session-111111112222", pod.Metadata.Name);
        Assert.Equal("eaos-agent-runtime", pod.Metadata.Labels["app"]);
        Assert.Equal("eaos", pod.Metadata.Labels["managed-by"]);
        Assert.Equal(TestIds.SandboxAgentId.ToString(), pod.Metadata.Labels["session-id"]);

        var container = Assert.Single(pod.Spec.Containers);
        Assert.Equal("pod-executor", container.Name);
        Assert.Equal("repo/pod-executor:test", container.Image);
        Assert.Equal("/workspace", container.WorkingDir);
        Assert.Contains(container.Env, env => env.Name == "AGENT_TOKEN" && env.Value == "eaos-session-111111112222");
        Assert.Contains(container.Env, env => env.Name == "WORKSPACE" && env.Value == "/workspace");
        Assert.Equal("/workspace", Assert.Single(container.VolumeMounts).MountPath);
        Assert.NotNull(Assert.Single(pod.Spec.Volumes).EmptyDir);
    }

    [Fact]
    public void BuildService_routes_to_matching_agent_pod()
    {
        var service = KubernetesAgentSandbox.BuildService(TestIds.SandboxAgentId);

        Assert.Equal("eaos-session-111111112222", service.Metadata.Name);
        Assert.Equal("eaos", service.Metadata.Labels["managed-by"]);
        Assert.Equal("ClusterIP", service.Spec.Type);
        Assert.Equal("eaos-agent-runtime", service.Spec.Selector["app"]);
        Assert.Equal(TestIds.SandboxAgentId.ToString(), service.Spec.Selector["session-id"]);
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
            "http://eaos-session-111111112222.default.svc.cluster.local:42617",
            KubernetesAgentSandbox.ServiceUrl("eaos-session-111111112222", "default"));
    }
}

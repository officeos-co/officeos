using System.Text.Json;
using EnterpriseAgentOs.Configuration;
using EnterpriseAgentOs.Infrastructure.Features.Agents;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Sandbox;

public sealed class DockerAgentSandboxTests
{
    private static readonly Guid AgentId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void BuildCreateContainerBody_labels_agent_runtime()
    {
        var body = DockerAgentSandbox.BuildCreateContainerBody(AgentId, new DockerConfig
        {
            Image = "repo/pod-executor:test",
            Network = "eaos",
        });

        var json = JsonSerializer.SerializeToElement(body);
        var labels = json.GetProperty("Labels");

        Assert.Equal("eaos-agent-runtime", labels.GetProperty("app").GetString());
        Assert.Equal("eaos", labels.GetProperty("managed-by").GetString());
        Assert.Equal(AgentId.ToString(), labels.GetProperty("agent-id").GetString());
    }

    [Fact]
    public void RuntimeFilterQuery_targets_only_eaos_agent_runtimes()
    {
        var decoded = Uri.UnescapeDataString(DockerAgentSandbox.RuntimeFilterQuery());
        using var doc = JsonDocument.Parse(decoded);
        var labels = doc.RootElement.GetProperty("label").EnumerateArray()
            .Select(label => label.GetString())
            .ToHashSet();

        Assert.Contains("managed-by=eaos", labels);
        Assert.Contains("app=eaos-agent-runtime", labels);
    }
}

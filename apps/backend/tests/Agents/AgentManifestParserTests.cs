using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Features.Agents;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class AgentManifestParserTests
{
    [Fact]
    public void ToDefinitionConfig_converts_agent_manifest_to_existing_definition_config()
    {
        var parser = new AgentManifestParser(new AgentDefinitionParser());

        var manifest = parser.ParseMany(
            """
            apiVersion: eaos.io/v1alpha1
            kind: Agent
            metadata:
              name: support-agent
            spec:
              provider: anthropic
              model: claude-sonnet-4-6
              description: Answers customer questions.
              system: Answer from sources.
              mcpServers:
                - name: notion
                  type: registered
              tools:
                - type: agent_toolset_20260401
                - type: mcp_toolset
                  mcpServerName: notion
                  defaultConfig:
                    permissionPolicy:
                      type: always_allow
            """).Single();

        var config = parser.ToDefinitionConfig(manifest);

        Assert.Equal("support-agent", config.Name);
        Assert.Equal("claude-sonnet-4-6", config.Model);
        Assert.Equal("Answer from sources.", config.System);
        Assert.Single(config.McpServers);
        Assert.Contains(config.Tools, tool => tool.Type == AgentToolsetKinds.Mcp);
    }
}

using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Features.Agents;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class AgentDefinitionParserTests
{
    [Fact]
    public void Parse_accepts_claude_style_support_agent_config()
    {
        var parser = new AgentDefinitionParser();

        var config = parser.Parse(
            """
            {
              "name": "Support agent",
              "description": "Answers customer questions from docs.",
              "model": "claude-sonnet-4-6",
              "system": "Answer from sources.",
              "mcp_servers": [
                { "name": "notion", "type": "url", "url": "https://mcp.notion.com/mcp" },
                { "name": "slack", "type": "url", "url": "https://mcp.slack.com/mcp" }
              ],
              "tools": [
                { "type": "agent_toolset_20260401" },
                {
                  "type": "mcp_toolset",
                  "mcp_server_name": "notion",
                  "default_config": {
                    "permission_policy": { "type": "always_allow" }
                  }
                },
                {
                  "type": "mcp_toolset",
                  "mcp_server_name": "slack",
                  "default_config": {
                    "permission_policy": { "type": "always_allow" }
                  }
                }
              ],
              "metadata": { "template": "support-agent" }
            }
            """);

        Assert.Equal("Support agent", config.Name);
        Assert.Equal("claude-sonnet-4-6", config.Model);
        Assert.Equal(2, config.McpServers.Count);
        Assert.Equal(3, config.Tools.Count);
    }

    [Fact]
    public void Parse_rejects_mcp_toolset_without_matching_server()
    {
        var parser = new AgentDefinitionParser();

        var ex = Assert.Throws<InvalidOperationException>(() => parser.Parse(
            """
            {
              "name": "Support agent",
              "model": "gpt-4o-mini",
              "tools": [
                { "type": "mcp_toolset", "mcp_server_name": "notion" }
              ]
            }
            """));

        Assert.Contains("unknown MCP server", ex.Message);
    }

    [Fact]
    public void Parse_rejects_empty_allow_list()
    {
        var parser = new AgentDefinitionParser();

        var ex = Assert.Throws<InvalidOperationException>(() => parser.Parse(
            """
            {
              "name": "Support agent",
              "model": "gpt-4o-mini",
              "tools": [
                {
                  "type": "agent_toolset_20260401",
                  "default_config": {
                    "permission_policy": { "type": "allow_list", "tools": [] }
                  }
                }
              ]
            }
            """));

        Assert.Contains("requires at least one tool", ex.Message);
    }
}

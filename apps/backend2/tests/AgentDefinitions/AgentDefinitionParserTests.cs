using OffceOs.Application.Features.AgentDefinitions;
using OffceOs.Domain.Features.AgentDefinitions;
using Xunit;

namespace OffceOs.Tests.AgentDefinitions;

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
    public void Parse_accepts_yaml_agent_config()
    {
        var parser = new AgentDefinitionParser();

        var config = parser.Parse(
            """
            name: Browser support agent
            description: Answers customer questions from docs.
            model: claude-sonnet-4-6
            system: |-
              Answer from sources.
            mcp_servers:
              - name: notion
                type: registered
            tools:
              - type: agent_toolset_20260401
              - type: browser_toolset
              - type: mcp_toolset
                mcp_server_name: notion
                default_config:
                  permission_policy:
                    type: always_allow
            metadata:
              template: support-agent
            """);

        Assert.Equal("Browser support agent", config.Name);
        Assert.Equal("claude-sonnet-4-6", config.Model);
        Assert.Single(config.McpServers);
        Assert.Contains(config.Tools, tool => tool.Type == AgentToolsetKinds.Browser);
    }

    [Fact]
    public void SerializeYaml_round_trips_agent_config()
    {
        var parser = new AgentDefinitionParser();
        var config = parser.Parse(
            """
            name: Support agent
            model: gpt-4o-mini
            tools:
              - type: agent_toolset_20260401
            metadata:
              template: support-agent
            """);

        var yaml = parser.SerializeYaml(config);
        var reparsed = parser.Parse(yaml);

        Assert.Equal(config.Name, reparsed.Name);
        Assert.Equal(config.Model, reparsed.Model);
        Assert.Equal(config.Tools.Count, reparsed.Tools.Count);
    }

    [Fact]
    public void Parse_accepts_resource_attachments_and_routines()
    {
        var parser = new AgentDefinitionParser();
        var browserId = Guid.NewGuid();
        var memoryId = Guid.NewGuid();

        var config = parser.Parse(
            $$"""
            name: Workflow agent
            model: gpt-4o-mini
            tools:
              - type: agent_toolset_20260401
            resources:
              - type: browser
                resource_id: {{browserId}}
                access_mode: read_write
                instructions: Verify the UI.
              - type: memory_store
                resource_id: {{memoryId}}
                access_mode: read_only
            routines:
              - name: Daily summary
                prompt: Summarize active work.
                schedule_triggers:
                  - name: Morning
                    expression: "0 9 * * 1-5"
            """);

        Assert.Equal(2, config.Resources?.Count);
        Assert.Equal(browserId, config.Resources?[0].ResourceId);
        var routine = Assert.Single(config.Routines!);
        Assert.Equal("Daily summary", routine.Name);
        Assert.Single(routine.ScheduleTriggers!);
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

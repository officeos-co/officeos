using EnterpriseAgentOs.Application.Features.Agents;
using EnterpriseAgentOs.Domain.Events;
using EnterpriseAgentOs.Domain.Features.Agents.Integrations;
using MediatR;
using System.Text.Json;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Agents;

public sealed class McpLazyToolTests
{
    [Fact]
    public void Lazy_mcp_tool_uses_catalog_metadata_without_connecting()
    {
        var server = new IntegrationDefinitionRecord { Name = "google-docs" };
        var connection = new LazyMcpServerConnection(
            server,
            _ => throw new InvalidOperationException("Credentials should not load during catalog setup."),
            new ThrowingMcpClientManager(),
            new TurnEventPublisher(new NoopPublisher()),
            Guid.NewGuid(),
            "correlation-1");

        var tool = new LazyMcpTool(server, new McpCatalogTool("create_document", "Create a new Google Doc", null), connection);

        Assert.Equal("google_docs__create_document", tool.Name);
        Assert.Equal("google-docs:create_document", tool.PermissionScope);
        Assert.Equal(AgentToolKind.Mcp, tool.Kind);
        Assert.True(((IAgentTool)tool).ShouldDefer);
    }

    [Fact]
    public async Task Tool_search_hydrates_lazy_mcp_tool_schema_from_server_discovery()
    {
        var server = new IntegrationDefinitionRecord { Name = "google-docs" };
        var manager = new HydratingMcpClientManager();
        var connection = new LazyMcpServerConnection(
            server,
            _ => Task.FromResult(new Dictionary<string, string>()),
            manager,
            new TurnEventPublisher(new NoopPublisher()),
            Guid.NewGuid(),
            "correlation-1");
        var tool = new LazyMcpTool(server, new McpCatalogTool("create_document", "Create a new Google Doc", null), connection);
        var search = new ToolSearchTool([tool]);

        var result = await search.ExecuteAsync(JsonSerializer.SerializeToElement(new { query = "google_docs" }));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Success);
        Assert.Equal(1, manager.ConnectCount);
        Assert.Contains("initialContent", result.Value.Output);

        var schema = JsonSerializer.SerializeToElement(tool.Schema.Parameters);
        var properties = schema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("title", out _));
        Assert.True(properties.TryGetProperty("initialContent", out _));
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
    }

    private sealed class ThrowingMcpClientManager : IMcpClientManager
    {
        public Task<McpConnectionResult> ConnectAsync(
            IntegrationDefinitionRecord server,
            Dictionary<string, string> credentials,
            CancellationToken ct = default)
            => throw new InvalidOperationException("MCP should not connect during catalog setup.");
    }

    private sealed class HydratingMcpClientManager : IMcpClientManager
    {
        public int ConnectCount { get; private set; }

        public Task<McpConnectionResult> ConnectAsync(
            IntegrationDefinitionRecord server,
            Dictionary<string, string> credentials,
            CancellationToken ct = default)
        {
            ConnectCount++;
            return Task.FromResult(new McpConnectionResult
            {
                Tools =
                [
                    new McpDiscoveredTool
                    {
                        IntegrationName = server.Name,
                        Name = "create_document",
                        Description = "Create a new Google Document with optional initial content",
                        JsonSchema = """
                            {
                              "type": "object",
                              "properties": {
                                "title": { "type": "string" },
                                "initialContent": { "type": "string" }
                              },
                              "required": ["title"],
                              "additionalProperties": false
                            }
                            """,
                        NativeHandle = new object()
                    }
                ],
            });
        }
    }

    private sealed class NoopPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;
    }
}

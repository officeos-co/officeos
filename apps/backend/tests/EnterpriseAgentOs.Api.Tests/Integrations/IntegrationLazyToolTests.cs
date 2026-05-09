using EnterpriseAgentOs.Application.Features.Agents;
using EnterpriseAgentOs.Domain.Events;
using EnterpriseAgentOs.Domain.Features.Integrations;
using MediatR;
using System.Text.Json;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Integrations;

public sealed class IntegrationLazyToolTests
{
    [Fact]
    public void Lazy_integration_tool_uses_catalog_metadata_without_connecting()
    {
        var server = new IntegrationDefinitionRecord { Name = "google-docs" };
        var connection = new LazyIntegrationConnection(
            server,
            _ => throw new InvalidOperationException("Credentials should not load during catalog setup."),
            new ThrowingIntegrationClientManager(),
            new TurnEventPublisher(new NoopPublisher()),
            Guid.NewGuid(),
            "correlation-1");

        var tool = new LazyIntegrationTool(server, new IntegrationCatalogTool("create_document", "Create a new Google Doc", null), connection);

        Assert.Equal("google_docs__create_document", tool.Name);
        Assert.Equal("google-docs:create_document", tool.PermissionScope);
        Assert.Equal(AgentToolKind.Integration, tool.Kind);
        Assert.True(((IAgentTool)tool).ShouldDefer);
    }

    [Fact]
    public async Task Tool_search_hydrates_lazy_integration_tool_schema_from_discovery()
    {
        var server = new IntegrationDefinitionRecord { Name = "google-docs" };
        var manager = new HydratingIntegrationClientManager();
        var connection = new LazyIntegrationConnection(
            server,
            _ => Task.FromResult(new Dictionary<string, string>()),
            manager,
            new TurnEventPublisher(new NoopPublisher()),
            Guid.NewGuid(),
            "correlation-1");
        var tool = new LazyIntegrationTool(server, new IntegrationCatalogTool("create_document", "Create a new Google Doc", null), connection);
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

    private sealed class ThrowingIntegrationClientManager : IIntegrationClientManager
    {
        public Task<IntegrationConnectionResult> ConnectAsync(
            IntegrationDefinitionRecord server,
            Dictionary<string, string> credentials,
            CancellationToken ct = default)
            => throw new InvalidOperationException("Integration should not connect during catalog setup.");
    }

    private sealed class HydratingIntegrationClientManager : IIntegrationClientManager
    {
        public int ConnectCount { get; private set; }

        public Task<IntegrationConnectionResult> ConnectAsync(
            IntegrationDefinitionRecord server,
            Dictionary<string, string> credentials,
            CancellationToken ct = default)
        {
            ConnectCount++;
            return Task.FromResult(new IntegrationConnectionResult
            {
                Tools =
                [
                    new IntegrationDiscoveredTool
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

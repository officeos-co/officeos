using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Events;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Tests.Shared;
using System.Text.Json;
using Xunit;

namespace OffceOs.Tests.Integrations;

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

}

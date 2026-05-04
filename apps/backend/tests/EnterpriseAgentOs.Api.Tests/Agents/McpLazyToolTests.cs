using EnterpriseAgentOs.Application.Features.Agents;
using EnterpriseAgentOs.Domain.Events;
using EnterpriseAgentOs.Domain.Features.Mcp;
using MediatR;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Agents;

public sealed class McpLazyToolTests
{
    [Fact]
    public void Lazy_mcp_tool_uses_catalog_metadata_without_connecting()
    {
        var server = new McpServerRecord { Name = "google-docs" };
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

    private sealed class ThrowingMcpClientManager : IMcpClientManager
    {
        public Task<McpConnectionResult> ConnectAsync(
            McpServerRecord server,
            Dictionary<string, string> credentials,
            CancellationToken ct = default)
            => throw new InvalidOperationException("MCP should not connect during catalog setup.");
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

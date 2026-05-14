using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Integrations;
using OffceOs.Domain.Features.Integrations;
using System.Text.Json;

namespace OffceOs.Tests.Shared;

public sealed class ThrowingIntegrationClientManager : IIntegrationClientManager
{
    public Task<IntegrationConnectionResult> ConnectAsync(
        IntegrationDefinitionRecord server,
        Dictionary<string, string> credentials,
        CancellationToken ct = default) =>
        throw new InvalidOperationException("Integration should not connect during catalog setup.");
}

public sealed class HydratingIntegrationClientManager : IIntegrationClientManager
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
                    Parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            title = new { type = "string" },
                            initialContent = new { type = "string" },
                        },
                        required = new[] { "title" },
                        additionalProperties = false,
                    },
                    NativeHandle = new object()
                }
            ],
        });
    }
}

public sealed class FakeIntegrationRuntimeService : IIntegrationRuntimeService
{
    public Task<ToolResult> ExecuteToolAsync(
        IntegrationDefinitionRecord integration,
        string toolName,
        JsonElement args,
        Guid? ownerId,
        Guid? workspaceId,
        CancellationToken ct = default) =>
        Task.FromResult(new ToolResult(true, "{}"));
}

using System.Text.Json;
using EnterpriseAgentOs.Application.Features.Agents.Integrations;
using EnterpriseAgentOs.Domain.Features.Agents.Integrations;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Atlas;

public sealed class AtlasConnectorExecuteContractTests
{
    [Fact]
    public void Connector_execute_tool_schema_matches_context_interface()
    {
        var tool = new IntegrationExecuteTool(new CapturingExecutionService());
        var schema = JsonSerializer.SerializeToElement(tool.Schema.Parameters);
        var properties = schema.GetProperty("properties");
        var required = schema.GetProperty("required").EnumerateArray().Select(x => x.GetString()!).ToArray();

        Assert.Equal("integration_execute", tool.Name);
        Assert.True(tool.AlwaysLoad);
        Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
        Assert.Equal(["source_id", "entity", "action", "params"], required);
        Assert.Equal("uuid", properties.GetProperty("source_id").GetProperty("format").GetString());
        Assert.True(properties.TryGetProperty("entity", out _));
        Assert.True(properties.TryGetProperty("action", out var action));
        Assert.True(properties.TryGetProperty("params", out _));
        Assert.True(properties.TryGetProperty("select_fields", out _));
        Assert.Contains("context_store_search", action.GetProperty("enum").EnumerateArray().Select(x => x.GetString()));
    }

    [Fact]
    public async Task Connector_execute_tool_accepts_context_search_example()
    {
        var execution = new CapturingExecutionService();
        var tool = new IntegrationExecuteTool(execution);
        var args = JsonSerializer.SerializeToElement(new
        {
            source_id = "8f6648f2-0335-4c71-bc75-de3feb9b7af4",
            entity = "repositories",
            action = "context_store_search",
            @params = new
            {
                query = new
                {
                    filter = new
                    {
                        fuzzy = new { name = "OfficeOS" }
                    }
                }
            },
            select_fields = new[] { "name", "owner", "description", "createdAt", "updatedAt", "url" }
        });

        var validation = await tool.ValidateAsync(args);
        var result = await tool.ExecuteAsync(args);

        Assert.True(validation.IsValid);
        Assert.True(result.IsSuccess);
        Assert.Equal(Guid.Parse("8f6648f2-0335-4c71-bc75-de3feb9b7af4"), execution.Request!.SourceId);
        Assert.Equal("repositories", execution.Request.Entity);
        Assert.Equal("context_store_search", execution.Request.Action);
        Assert.Equal(["name", "owner", "description", "createdAt", "updatedAt", "url"], execution.Request.SelectFields);
    }

    private sealed class CapturingExecutionService : IIntegrationExecutionService
    {
        public IntegrationExecuteRequest? Request { get; private set; }

        public Task<JsonElement> ExecuteAsync(IntegrationExecuteRequest request, CancellationToken ct = default)
        {
            Request = request;
            return Task.FromResult(JsonSerializer.SerializeToElement(new
            {
                status = "success",
                result = Array.Empty<object>(),
                connector_metadata = (object?)null,
                execution_metadata = new
                {
                    connector_instance_id = $"source_id:{request.SourceId}",
                    execution_time_ms = 1,
                },
            }));
        }
    }
}

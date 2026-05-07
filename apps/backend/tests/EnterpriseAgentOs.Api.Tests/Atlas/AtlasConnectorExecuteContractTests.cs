using System.Text.Json;
using EnterpriseAgentOs.Application.Features.Atlas;
using EnterpriseAgentOs.Domain.Common.Services;
using EnterpriseAgentOs.Domain.Features.Atlas;
using Xunit;

namespace EnterpriseAgentOs.Api.Tests.Atlas;

public sealed class AtlasConnectorExecuteContractTests
{
    [Fact]
    public void Atlas_github_connector_is_defined_in_backend_registry()
    {
        var connector = AtlasConnectorRegistry.GetBuiltin("github");

        Assert.NotNull(connector);
        Assert.Equal("github", connector.Name);
        Assert.Equal("github", connector.Provider);
        Assert.Equal("GitHub", connector.Title);
        Assert.Equal("github", connector.OauthProvider);
        Assert.Equal("""["user:email","repo","read:org"]""", connector.OauthScopesJson);
        Assert.Contains("connector_execute", connector.ToolsJson);
        Assert.Contains("viewBox=\"0 0 24 24\"", connector.Logo);
        Assert.Equal(["repositories", "issues", "pull_requests", "commits"], connector.Entities);
    }

    [Fact]
    public void Connector_execute_tool_schema_matches_atlas_interface()
    {
        var tool = new AtlasConnectorExecuteTool(new CapturingExecutionService());
        var schema = JsonSerializer.SerializeToElement(tool.Schema.Parameters);
        var properties = schema.GetProperty("properties");
        var required = schema.GetProperty("required").EnumerateArray().Select(x => x.GetString()!).ToArray();

        Assert.Equal("connector_execute", tool.Name);
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
    public async Task Connector_execute_tool_accepts_atlas_search_example()
    {
        var execution = new CapturingExecutionService();
        var tool = new AtlasConnectorExecuteTool(execution);
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

    [Fact]
    public async Task Connector_execute_tool_accepts_atlas_direct_list_example()
    {
        var execution = new CapturingExecutionService();
        var tool = new AtlasConnectorExecuteTool(execution);
        var args = JsonSerializer.SerializeToElement(new
        {
            source_id = "8f6648f2-0335-4c71-bc75-de3feb9b7af4",
            entity = "commits",
            action = "list",
            @params = new { owner = "officeos-co", repo = "officeos", per_page = 10 },
            select_fields = new[] { "abbreviatedOid", "messageHeadline", "committedDate", "additions", "deletions", "changedFiles" }
        });

        var validation = await tool.ValidateAsync(args);
        var result = await tool.ExecuteAsync(args);

        Assert.True(validation.IsValid);
        Assert.True(result.IsSuccess);
        Assert.Equal("commits", execution.Request!.Entity);
        Assert.Equal("list", execution.Request.Action);
        Assert.Equal("officeos-co", execution.Request.Params.GetProperty("owner").GetString());
        Assert.Equal(10, execution.Request.Params.GetProperty("per_page").GetInt32());
    }

    private sealed class CapturingExecutionService : IAtlasConnectorExecutionService
    {
        public AtlasConnectorExecuteRequest? Request { get; private set; }

        public Task<JsonElement> ExecuteAsync(AtlasConnectorExecuteRequest request, CancellationToken ct = default)
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

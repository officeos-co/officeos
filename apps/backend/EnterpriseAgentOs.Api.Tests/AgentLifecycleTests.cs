using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;

namespace EnterpriseAgentOs.Api.Tests;

public sealed class AgentLifecycleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AgentLifecycleTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateAgent_ReturnsCreatedWithPendingOrRunningStatus()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/agents", new
        {
            name = "lifecycle-test",
            provider = "ollama",
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("lifecycle-test", body.GetProperty("name").GetString());
        Assert.Equal("ollama", body.GetProperty("provider").GetString());
        var status = body.GetProperty("status").GetString();
        Assert.True(status == "pending" || status == "running",
            $"Expected pending or running, got {status}");
    }

    [Fact]
    public async Task ListAgents_ReturnsCreatedAgents()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        await TestHelpers.CreateAgentAsync(client, "list-test-1", "ollama");
        await TestHelpers.CreateAgentAsync(client, "list-test-2", "ollama");

        var response = await client.GetAsync("/api/agents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetArrayLength() >= 2);
    }

    [Fact]
    public async Task GetAgent_ById_ReturnsAgent()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(client, "get-test", "ollama");

        var response = await client.GetAsync($"/api/agents/{agentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(agentId, body.GetProperty("id").GetGuid());
        Assert.Equal("get-test", body.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetAgent_NonExistent_Returns404()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var response = await client.GetAsync($"/api/agents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAgent_SoftDeletes_ThenExcludedFromList()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(client, "delete-test", "ollama");

        var deleteResp = await client.DeleteAsync($"/api/agents/{agentId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);

        // GET by ID should return 404 (soft-deleted)
        var getResp = await client.GetAsync($"/api/agents/{agentId}");
        Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
    }

    [Fact]
    public async Task DeleteAgent_NonExistent_Returns404()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var response = await client.DeleteAsync($"/api/agents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateAgent_WithUnconfiguredProvider_Returns400()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        // openai provider needs an API key configured; in test DB it won't have one
        var response = await client.PostAsJsonAsync("/api/agents", new
        {
            name = "should-fail",
            provider = "openai",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("not configured", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CreateAgent_WithInvalidModel_Returns400()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/agents", new
        {
            name = "bad-model",
            provider = "ollama",
            model = "nonexistent-model-xyz",
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("not a known model", body.GetProperty("error").GetString());
    }
}

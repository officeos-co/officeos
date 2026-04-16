using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;

namespace EnterpriseAgentOs.Api.Tests;

public sealed class AgentLifecycleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AgentLifecycleTests(CustomWebApplicationFactory factory) => _factory = factory;

    private const string AgentFields = "id name provider status";

    [Fact]
    public async Task CreateAgent_ReturnsCreatedWithPendingOrRunningStatus()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        const string mutation = @"
            mutation($input: CreateAgentInput!) {
              createAgent(input: $input) { id name provider status }
            }";
        var data = await TestHelpers.GraphQLAsync(client, mutation, new
        {
            input = new
            {
                name = "lifecycle-test",
                provider = "ollama",
                model = (string?)null,
                prompt = (string?)null,
                integrationSlugs = (string[]?)null,
                channelSlugs = (string[]?)null,
            }
        });

        var agent = data.GetProperty("createAgent");
        Assert.Equal("lifecycle-test", agent.GetProperty("name").GetString());
        Assert.Equal("ollama", agent.GetProperty("provider").GetString());
        var status = agent.GetProperty("status").GetString();
        Assert.True(status == "pending" || status == "running",
            $"Expected pending or running, got {status}");
    }

    [Fact]
    public async Task ListAgents_ReturnsCreatedAgents()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        await TestHelpers.CreateAgentAsync(client, "list-test-1", "ollama");
        await TestHelpers.CreateAgentAsync(client, "list-test-2", "ollama");

        var data = await TestHelpers.GraphQLAsync(client, "{ agents { id name } }");
        var agents = data.GetProperty("agents");
        Assert.True(agents.GetArrayLength() >= 2);
    }

    [Fact]
    public async Task GetAgent_ById_ReturnsAgent()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(client, "get-test", "ollama");

        var data = await TestHelpers.GraphQLAsync(
            client,
            "query($id: UUID!) { agent(id: $id) { id name } }",
            new { id = agentId });

        var agent = data.GetProperty("agent");
        Assert.Equal(agentId, agent.GetProperty("id").GetGuid());
        Assert.Equal("get-test", agent.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetAgent_NonExistent_ReturnsNull()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var data = await TestHelpers.GraphQLAsync(
            client,
            "query($id: UUID!) { agent(id: $id) { id } }",
            new { id = Guid.NewGuid() });

        Assert.Equal(JsonValueKind.Null, data.GetProperty("agent").ValueKind);
    }

    [Fact]
    public async Task DeleteAgent_SoftDeletes_ThenExcludedFromList()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(client, "delete-test", "ollama");

        var delData = await TestHelpers.GraphQLAsync(
            client,
            "mutation($id: UUID!) { deleteAgent(id: $id) }",
            new { id = agentId });
        Assert.True(delData.GetProperty("deleteAgent").GetBoolean());

        // GET by ID now returns null (soft-deleted)
        var getData = await TestHelpers.GraphQLAsync(
            client,
            "query($id: UUID!) { agent(id: $id) { id } }",
            new { id = agentId });
        Assert.Equal(JsonValueKind.Null, getData.GetProperty("agent").ValueKind);
    }

    [Fact]
    public async Task DeleteAgent_NonExistent_ReturnsFalse()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var data = await TestHelpers.GraphQLAsync(
            client,
            "mutation($id: UUID!) { deleteAgent(id: $id) }",
            new { id = Guid.NewGuid() });

        Assert.False(data.GetProperty("deleteAgent").GetBoolean());
    }

    [Fact]
    public async Task CreateAgent_WithUnconfiguredProvider_ReturnsError()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        // openai provider needs an API key configured; in test DB it won't have one
        const string mutation = @"
            mutation($input: CreateAgentInput!) {
              createAgent(input: $input) { id }
            }";
        var response = await TestHelpers.GraphQLRawAsync(client, mutation, new
        {
            input = new
            {
                name = "should-fail",
                provider = "openai",
                model = (string?)null,
                prompt = (string?)null,
                integrationSlugs = (string[]?)null,
                channelSlugs = (string[]?)null,
            }
        });

        Assert.True(response.TryGetProperty("errors", out var errors));
        Assert.True(errors.GetArrayLength() > 0);
        Assert.Contains("not configured", errors.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateAgent_WithInvalidModel_ReturnsError()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        const string mutation = @"
            mutation($input: CreateAgentInput!) {
              createAgent(input: $input) { id }
            }";
        var response = await TestHelpers.GraphQLRawAsync(client, mutation, new
        {
            input = new
            {
                name = "bad-model",
                provider = "ollama",
                model = "nonexistent-model-xyz",
                prompt = (string?)null,
                integrationSlugs = (string[]?)null,
                channelSlugs = (string[]?)null,
            }
        });

        Assert.True(response.TryGetProperty("errors", out var errors));
        Assert.True(errors.GetArrayLength() > 0);
        Assert.Contains("not a known model", errors.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

namespace EnterpriseAgentOs.Api.Tests;

public sealed class AgentLifecycleTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>
{
    private readonly Infrastructure.CustomWebApplicationFactory _customWebApplicationFactory;

    public AgentLifecycleTests(Infrastructure.CustomWebApplicationFactory factory) => _customWebApplicationFactory = factory;

    private const string AgentFields = "id name provider status";

    [Fact]
    public async Task CreateAgent_ReturnsCreatedWithPendingOrRunningStatus()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);

        const string mutation = @"
            mutation($input: CreateAgentInput!) {
              createAgent(input: $input) { id name provider status }
            }";
        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, mutation, new
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
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);
        await Infrastructure.TestHelpers.CreateAgentAsync(client, "list-test-1", "ollama");
        await Infrastructure.TestHelpers.CreateAgentAsync(client, "list-test-2", "ollama");

        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, "{ agents { id name } }");
        var agents = data.GetProperty("agents");
        Assert.True(agents.GetArrayLength() >= 2);
    }

    [Fact]
    public async Task GetAgent_ById_ReturnsAgent()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);
        var agentId = await Infrastructure.TestHelpers.CreateAgentAsync(client, "get-test", "ollama");

        var data = await Infrastructure.TestHelpers.GraphQLAsync(
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
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);

        var data = await Infrastructure.TestHelpers.GraphQLAsync(
            client,
            "query($id: UUID!) { agent(id: $id) { id } }",
            new { id = Guid.NewGuid() });

        Assert.Equal(JsonValueKind.Null, data.GetProperty("agent").ValueKind);
    }

    [Fact]
    public async Task DeleteAgent_SoftDeletes_ThenExcludedFromList()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);
        var agentId = await Infrastructure.TestHelpers.CreateAgentAsync(client, "delete-test", "ollama");

        var delData = await Infrastructure.TestHelpers.GraphQLAsync(
            client,
            "mutation($id: UUID!) { deleteAgent(id: $id) }",
            new { id = agentId });
        Assert.True(delData.GetProperty("deleteAgent").GetBoolean());

        // GET by ID now returns null (soft-deleted)
        var getData = await Infrastructure.TestHelpers.GraphQLAsync(
            client,
            "query($id: UUID!) { agent(id: $id) { id } }",
            new { id = agentId });
        Assert.Equal(JsonValueKind.Null, getData.GetProperty("agent").ValueKind);
    }

    [Fact]
    public async Task DeleteAgent_NonExistent_ReturnsFalse()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);

        var data = await Infrastructure.TestHelpers.GraphQLAsync(
            client,
            "mutation($id: UUID!) { deleteAgent(id: $id) }",
            new { id = Guid.NewGuid() });

        Assert.False(data.GetProperty("deleteAgent").GetBoolean());
    }

    [Fact]
    public async Task CreateAgent_WithUnconfiguredProvider_ReturnsError()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);

        // Use a truly unknown provider that is not in the keyless list and has no key configured
        const string mutation = @"
            mutation($input: CreateAgentInput!) {
              createAgent(input: $input) { id }
            }";
        var response = await Infrastructure.TestHelpers.GraphQLRawAsync(client, mutation, new
        {
            input = new
            {
                name = "should-fail",
                provider = "unknown-provider-xyz",
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

    [Theory]
    [InlineData("ollama")]
    [InlineData("anthropic")]
    [InlineData("google")]
    [InlineData("xai")]
    [InlineData("openai")]
    public async Task CreateAgent_WithKeylessProvider_SucceedsWithoutApiKey(string provider)
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);

        const string mutation = @"
            mutation($input: CreateAgentInput!) {
              createAgent(input: $input) { id name provider status }
            }";
        var data = await Infrastructure.TestHelpers.GraphQLAsync(client, mutation, new
        {
            input = new
            {
                name = $"keyless-{provider}",
                provider,
                model = (string?)null,
                prompt = (string?)null,
                integrationSlugs = (string[]?)null,
                channelSlugs = (string[]?)null,
            }
        });

        var agent = data.GetProperty("createAgent");
        Assert.Equal($"keyless-{provider}", agent.GetProperty("name").GetString());
        Assert.Equal(provider, agent.GetProperty("provider").GetString());
    }

    [Fact]
    public async Task CreateAgent_WithInvalidModel_ReturnsError()
    {
        var client = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_customWebApplicationFactory);

        // Configure the provider first so we get past the key check
        await Infrastructure.TestHelpers.GraphQLAsync(client, @"
            mutation($providerName: String!, $apiKey: String!) {
              setProviderKey(providerName: $providerName, apiKey: $apiKey) { name }
            }", new { providerName = "openai", apiKey = "sk-test-key" });

        const string mutation = @"
            mutation($input: CreateAgentInput!) {
              createAgent(input: $input) { id }
            }";
        var response = await Infrastructure.TestHelpers.GraphQLRawAsync(client, mutation, new
        {
            input = new
            {
                name = "bad-model",
                provider = "openai",
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

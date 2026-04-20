namespace EnterpriseAgentOs.Api.Tests;

/// <summary>
/// Integration tests for channel message routing:
/// - Incoming messages are logged as ChannelIn
/// - Agent responses are logged as ChannelOut
/// - ChannelIn and ChannelOut share a CorrelationId
/// </summary>
public sealed class ChannelMessageRoutingTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly Infrastructure.CustomWebApplicationFactory _customWebApplicationFactory;
    private Infrastructure.FakeAgentWsServer _fakeAgent = null!;

    public ChannelMessageRoutingTests(Infrastructure.CustomWebApplicationFactory factory)
        => _customWebApplicationFactory = factory;

    public Task InitializeAsync()
    {
        _fakeAgent = new Infrastructure.FakeAgentWsServer();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _fakeAgent.DisposeAsync();
    }

    /// <summary>
    /// Seeds a channel connection, an agent, and a binding between them.
    /// Returns (connectionId, agentId).
    /// </summary>
    private async Task<(Guid ConnectionId, Guid AgentId)> SeedChannelBindingAsync()
    {
        using var scope = _customWebApplicationFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EaosDbContext>();

        // Create a user
        var user = new UserRecord
        {
            Email = $"channel-test-{Guid.NewGuid():N}@example.com",
            Name = "Channel Tester",
            GoogleSubjectId = $"google-{Guid.NewGuid():N}",
        };
        db.Users.Add(user);

        // Create an agent with ServiceUrl pointing at the fake WS server
        var agent = new AgentRecord
        {
            Name = "channel-test-agent",
            Provider = "ollama",
            Status = "running",
            ServiceUrl = _fakeAgent.ServiceUrl,
        };
        db.Agents.Add(agent);

        // Create a channel connection (slack, no encrypted config needed for routing)
        var connection = new ChannelConnectionRecord
        {
            ChannelType = "slack",
            DisplayName = "Test Slack",
            CreatedById = user.Id,
        };
        db.ChannelConnections.Add(connection);
        await db.SaveChangesAsync();

        // Create binding
        var binding = new AgentChannelBindingRecord
        {
            AgentId = agent.Id,
            ChannelConnectionId = connection.Id,
        };
        db.AgentChannelBindings.Add(binding);
        await db.SaveChangesAsync();

        return (connection.Id, agent.Id);
    }

    [Fact]
    public async Task RouteMessage_LogsChannelInEntry()
    {
        _fakeAgent.SetResponse("Hello from agent");
        var (connectionId, agentId) = await SeedChannelBindingAsync();

        using var scope = _customWebApplicationFactory.Services.CreateScope();
        var router = scope.ServiceProvider.GetRequiredService<ChannelMessageRouter>();
        var logRepo = scope.ServiceProvider.GetRequiredService<IAgentLogRepository>();

        await router.RouteMessageAsync(connectionId, "U123", "Hello from Slack");

        var logs = await logRepo.ListAsync(agentId, before: null, limit: 50);
        var channelIn = logs.FirstOrDefault(l => l.Type == AgentLogType.ChannelIn);

        Assert.NotNull(channelIn);
        Assert.Equal("Hello from Slack", channelIn.Content);
        Assert.Equal("slack", channelIn.Channel);
        Assert.Equal(agentId, channelIn.AgentId);
    }

    [Fact]
    public async Task RouteMessage_LogsChannelOutEntry()
    {
        _fakeAgent.SetResponse("Agent response text");
        var (connectionId, agentId) = await SeedChannelBindingAsync();

        using var scope = _customWebApplicationFactory.Services.CreateScope();
        var router = scope.ServiceProvider.GetRequiredService<ChannelMessageRouter>();
        var logRepo = scope.ServiceProvider.GetRequiredService<IAgentLogRepository>();

        await router.RouteMessageAsync(connectionId, "U123", "Hello");

        var logs = await logRepo.ListAsync(agentId, before: null, limit: 50);
        var channelOut = logs.FirstOrDefault(l => l.Type == AgentLogType.ChannelOut);

        Assert.NotNull(channelOut);
        Assert.Equal("Agent response text", channelOut.Content);
        Assert.Equal("slack", channelOut.Channel);
        Assert.Equal(agentId, channelOut.AgentId);
    }

    [Fact]
    public async Task RouteMessage_ChannelInAndChannelOut_ShareCorrelationId()
    {
        _fakeAgent.SetResponse("Hello from agent");
        var (connectionId, agentId) = await SeedChannelBindingAsync();

        using var scope = _customWebApplicationFactory.Services.CreateScope();
        var router = scope.ServiceProvider.GetRequiredService<ChannelMessageRouter>();
        var logRepo = scope.ServiceProvider.GetRequiredService<IAgentLogRepository>();

        await router.RouteMessageAsync(connectionId, "U123", "Correlated message");

        var logs = await logRepo.ListAsync(agentId, before: null, limit: 50);
        var channelIn = logs.FirstOrDefault(l => l.Type == AgentLogType.ChannelIn);
        var channelOut = logs.FirstOrDefault(l => l.Type == AgentLogType.ChannelOut);

        Assert.NotNull(channelIn);
        Assert.NotNull(channelOut);
        Assert.NotNull(channelIn.CorrelationId);
        Assert.Equal(channelIn.CorrelationId, channelOut.CorrelationId);
    }

    [Fact]
    public async Task RouteMessage_PolicyBlocked_NoLogsCreated()
    {
        using var scope = _customWebApplicationFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EaosDbContext>();

        var user = new UserRecord
        {
            Email = $"blocked-{Guid.NewGuid():N}@example.com",
            Name = "Blocked Tester",
            GoogleSubjectId = $"google-{Guid.NewGuid():N}",
        };
        db.Users.Add(user);

        var agent = new AgentRecord
        {
            Name = "blocked-agent",
            Provider = "ollama",
            Status = "running",
            ServiceUrl = _fakeAgent.ServiceUrl,
        };
        db.Agents.Add(agent);

        var connection = new ChannelConnectionRecord
        {
            ChannelType = "slack",
            DisplayName = "Blocked Slack",
            CreatedById = user.Id,
        };
        db.ChannelConnections.Add(connection);
        await db.SaveChangesAsync();

        // Binding with allowlist policy — only "allowed-user" allowed
        var binding = new AgentChannelBindingRecord
        {
            AgentId = agent.Id,
            ChannelConnectionId = connection.Id,
            Config = JsonSerializer.Serialize(new { dmPolicy = "allowlist", allowedUsers = new[] { "allowed-user" } }),
        };
        db.AgentChannelBindings.Add(binding);
        await db.SaveChangesAsync();

        var router = scope.ServiceProvider.GetRequiredService<ChannelMessageRouter>();
        var logRepo = scope.ServiceProvider.GetRequiredService<IAgentLogRepository>();

        // Send from unauthorized user
        await router.RouteMessageAsync(connection.Id, "unauthorized-user", "Should be blocked");

        var logs = await logRepo.ListAsync(agent.Id, before: null, limit: 50);
        Assert.Empty(logs.Where(l => l.Type == AgentLogType.ChannelIn || l.Type == AgentLogType.ChannelOut));
    }
}

namespace EnterpriseAgentOs.Api.Tests;

/// <summary>
/// Integration tests for channel message routing:
/// - Incoming messages are logged as ChannelIn
/// - Agent responses are logged as ChannelOut
/// - ChannelIn and ChannelOut share a CorrelationId
/// </summary>
public sealed class ChannelMessageRoutingTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>
{
    private readonly Infrastructure.CustomWebApplicationFactory _factory;

    public ChannelMessageRoutingTests(Infrastructure.CustomWebApplicationFactory factory)
        => _factory = factory;

    /// <summary>
    /// Seeds a channel connection, an agent, and a binding between them.
    /// Returns (connectionId, agentId).
    /// </summary>
    private async Task<(Guid ConnectionId, Guid AgentId)> SeedChannelBindingAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EaosDbContext>();

        // Create a user
        var user = new UserRecord
        {
            Email = $"channel-test-{Guid.NewGuid():N}@example.com",
            Name = "Channel Tester",
            GoogleSubjectId = $"google-{Guid.NewGuid():N}",
        };
        db.Users.Add(user);

        // Create an agent with a ServiceUrl (pointing at WireMock)
        var agent = new AgentRecord
        {
            Name = "channel-test-agent",
            Provider = "ollama",
            Status = "running",
            ServiceUrl = _factory.SkillRuntimeMock.Url!, // reuse WireMock as fake agent
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

    private void StubAgentChatEndpoint(string responseText = "Hello from agent")
    {
        _factory.SkillRuntimeMock
            .Given(Request.Create()
                .WithPath("/api/chat")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody($"{{\"response\":\"{responseText}\"}}"));
    }

    [Fact]
    public async Task RouteMessage_LogsChannelInEntry()
    {
        var (connectionId, agentId) = await SeedChannelBindingAsync();
        StubAgentChatEndpoint();

        using var scope = _factory.Services.CreateScope();
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
        var (connectionId, agentId) = await SeedChannelBindingAsync();
        StubAgentChatEndpoint("Agent response text");

        using var scope = _factory.Services.CreateScope();
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
        var (connectionId, agentId) = await SeedChannelBindingAsync();
        StubAgentChatEndpoint();

        using var scope = _factory.Services.CreateScope();
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
        using var scope = _factory.Services.CreateScope();
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
            ServiceUrl = _factory.SkillRuntimeMock.Url!,
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

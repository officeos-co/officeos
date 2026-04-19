namespace EnterpriseAgentOs.Api.Tests;

/// <summary>
/// Tests the agent bootstrap endpoint (GET /api/agents/{id}) to verify the
/// contract that the Rust zeroclaw-agent binary depends on — in particular
/// that gateway.host:gateway.port is a valid socket address (numeric IP, not
/// a hostname).
/// </summary>
public sealed class AgentBootstrapTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>
{
    private readonly Infrastructure.CustomWebApplicationFactory _factory;

    public AgentBootstrapTests(Infrastructure.CustomWebApplicationFactory factory)
        => _factory = factory;

    /// <summary>
    /// Calls the bootstrap endpoint for an agent and returns the parsed JSON.
    /// </summary>
    private async Task<JsonElement> GetBootstrapPayloadAsync(HttpClient dashboardClient, Guid agentId)
    {
        // The bootstrap endpoint uses agent-UUID bearer auth, not session cookies.
        var agentClient = _factory.CreateClient();
        agentClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", agentId.ToString());

        var response = await agentClient.GetAsync($"/api/agents/{agentId}");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    [Fact]
    public async Task Bootstrap_GatewayHost_IsValidIpAddress()
    {
        var client = await Infrastructure.TestHelpers
            .CreateAuthenticatedClientAsync(_factory);
        var agentId = await Infrastructure.TestHelpers
            .CreateAgentAsync(client, "bootstrap-gateway-test", "ollama");

        var payload = await GetBootstrapPayloadAsync(client, agentId);
        var gateway = payload.GetProperty("gateway");
        var host = gateway.GetProperty("host").GetString()!;
        var port = gateway.GetProperty("port").GetInt32();

        // The Rust agent does: format!("{host}:{port}").parse::<SocketAddr>()
        // This requires a numeric IP address, not a hostname.
        Assert.True(
            IPAddress.TryParse(host, out _),
            $"Gateway host must be a numeric IP address parseable as SocketAddr, got \"{host}\"");

        Assert.True(
            port > 0 && port <= 65535,
            $"Gateway port must be in range 1-65535, got {port}");

        // End-to-end: the combined string must parse as a .NET IPEndPoint,
        // which mirrors Rust's SocketAddr parsing.
        Assert.True(
            IPEndPoint.TryParse($"{host}:{port}", out _),
            $"Gateway bind address \"{host}:{port}\" is not a valid socket address");
    }

    [Fact]
    public async Task Bootstrap_GatewayPort_Is42617()
    {
        var client = await Infrastructure.TestHelpers
            .CreateAuthenticatedClientAsync(_factory);
        var agentId = await Infrastructure.TestHelpers
            .CreateAgentAsync(client, "bootstrap-port-test", "ollama");

        var payload = await GetBootstrapPayloadAsync(client, agentId);
        var port = payload.GetProperty("gateway").GetProperty("port").GetInt32();

        Assert.Equal(42617, port);
    }
}

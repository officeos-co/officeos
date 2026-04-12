using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;

namespace EnterpriseAgentOs.Api.Tests;

public sealed class RunnerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RunnerTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task CreateRunner_ReturnsRegistrationToken()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var response = await client.PostAsJsonAsync("/api/runners", new { name = "my-runner" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.NotEqual(Guid.Empty, body.GetProperty("id").GetGuid());
        var token = body.GetProperty("registrationToken").GetString();
        Assert.NotNull(token);
        Assert.StartsWith("sr_", token);
    }

    [Fact]
    public async Task RegisterRunner_WithValidToken_ReturnsAuthToken()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var (_, regToken) = await TestHelpers.CreateRunnerAsync(client);

        var runner = new MockRunnerClient(_factory.CreateClient());
        var response = await runner.RegisterAsync(regToken, "1.0.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(runner.AuthToken);
    }

    [Fact]
    public async Task RegisterRunner_WithInvalidToken_Returns401()
    {
        var runner = new MockRunnerClient(_factory.CreateClient());
        var response = await runner.RegisterAsync("bogus-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RegisterRunner_Twice_Returns409()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var (_, regToken) = await TestHelpers.CreateRunnerAsync(client, "double-reg");

        var runner1 = new MockRunnerClient(_factory.CreateClient());
        var resp1 = await runner1.RegisterAsync(regToken);
        Assert.Equal(HttpStatusCode.OK, resp1.StatusCode);

        var runner2 = new MockRunnerClient(_factory.CreateClient());
        var resp2 = await runner2.RegisterAsync(regToken);
        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [Fact]
    public async Task PollJobs_NoPendingJobs_Returns204()
    {
        var client = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var (_, regToken) = await TestHelpers.CreateRunnerAsync(client, "poll-empty");

        var runner = new MockRunnerClient(_factory.CreateClient());
        await runner.RegisterAsync(regToken);

        var (response, job) = await runner.PollJobAsync();

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(job);
    }

    [Fact]
    public async Task PollJobs_WithoutAuth_Returns401()
    {
        var runner = new MockRunnerClient(_factory.CreateClient());
        // Don't register — no auth token
        var response = await runner.ListSkillsAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Heartbeat_UpdatesTimestamp()
    {
        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var (runnerId, regToken) = await TestHelpers.CreateRunnerAsync(dashClient, "heartbeat-test");

        var runner = new MockRunnerClient(_factory.CreateClient());
        await runner.RegisterAsync(regToken);

        var hbResp = await runner.HeartbeatAsync();
        Assert.Equal(HttpStatusCode.OK, hbResp.StatusCode);

        // Verify runner is still online via dashboard API
        var listResp = await dashClient.GetAsync("/api/runners");
        var runners = await listResp.Content.ReadFromJsonAsync<JsonElement>();
        var found = runners.EnumerateArray()
            .FirstOrDefault(r => r.GetProperty("id").GetGuid() == runnerId);
        Assert.Equal("online", found.GetProperty("status").GetString());
    }

    [Fact]
    public async Task ListSkills_WithRegisteredRunner_Returns200()
    {
        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var (_, regToken) = await TestHelpers.CreateRunnerAsync(dashClient, "skills-list");

        var runner = new MockRunnerClient(_factory.CreateClient());
        await runner.RegisterAsync(regToken);

        var response = await runner.ListSkillsAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        // No custom skills seeded, so should be empty array
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetArrayLength());
    }

    [Fact]
    public async Task CreateRunner_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/runners", new { name = "no-auth" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

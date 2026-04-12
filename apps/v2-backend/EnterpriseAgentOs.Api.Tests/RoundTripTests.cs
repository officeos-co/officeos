using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace EnterpriseAgentOs.Api.Tests;

/// <summary>
/// End-to-end tests that exercise the full flow: dashboard creates agent + runner,
/// agent calls skill-exec, runner claims job, posts result, agent gets result.
/// </summary>
public sealed class RoundTripTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RoundTripTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task FullRunnerRoundTrip_AgentCallsSkillExec_RunnerCompletesJob()
    {
        SeedNotionManifest();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        // 1. Create agent
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, "roundtrip-agent");

        // 2. Create and register runner
        var (_, regToken) = await TestHelpers.CreateRunnerAsync(dashClient, "roundtrip-runner");
        var runner = new MockRunnerClient(_factory.CreateClient());
        var regResp = await runner.RegisterAsync(regToken, "1.0.0");
        Assert.Equal(HttpStatusCode.OK, regResp.StatusCode);

        // 3. Install skill and set to runner target
        await dashClient.PostAsJsonAsync("/api/skills/notion/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/notion/credentials", new
        {
            credentials = new Dictionary<string, string> { ["apiKey"] = "test-key" }
        });
        await dashClient.PutAsJsonAsync("/api/skills/notion/run-target", new { runTarget = "runner" });

        // 4. Agent calls skill-exec (starts waiting for runner result)
        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var execTask = agent.SkillExecAsync("notion", "search", new { query = "roundtrip" });

        // 5. Runner polls for job (small delay to let the job be created)
        await Task.Delay(100);
        var (pollResp, job) = await runner.PollJobAsync();
        Assert.Equal(HttpStatusCode.OK, pollResp.StatusCode);
        Assert.NotNull(job);

        var jobId = job.Value.GetProperty("id").GetGuid();
        var payload = job.Value.GetProperty("payload");
        Assert.Equal("notion", payload.GetProperty("skill").GetString());
        Assert.Equal("search", payload.GetProperty("action").GetString());

        // 6. Runner posts result
        var resultResp = await runner.PostResultAsync(jobId, true, new { pages = new[] { "page-1", "page-2" } });
        Assert.Equal(HttpStatusCode.OK, resultResp.StatusCode);

        // 7. Agent's skill-exec call should now complete
        var execResp = await execTask;
        Assert.Equal(HttpStatusCode.OK, execResp.StatusCode);
        var execBody = await execResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(execBody.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task RunnerRoundTrip_JobFails_AgentGetsError()
    {
        SeedNotionManifest();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, "roundtrip-fail-agent");
        var (_, regToken) = await TestHelpers.CreateRunnerAsync(dashClient, "roundtrip-fail-runner");

        var runner = new MockRunnerClient(_factory.CreateClient());
        await runner.RegisterAsync(regToken);

        await dashClient.PostAsJsonAsync("/api/skills/notion/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/notion/credentials", new
        {
            credentials = new Dictionary<string, string> { ["apiKey"] = "k" }
        });
        await dashClient.PutAsJsonAsync("/api/skills/notion/run-target", new { runTarget = "runner" });

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var execTask = agent.SkillExecAsync("notion", "search", new { query = "fail" });

        await Task.Delay(100);
        var (_, job) = await runner.PollJobAsync();
        Assert.NotNull(job);

        var jobId = job.Value.GetProperty("id").GetGuid();
        await runner.PostResultAsync(jobId, false, error: "Notion API rate limited");

        var execResp = await execTask;
        Assert.Equal(HttpStatusCode.UnprocessableEntity, execResp.StatusCode);
        var body = await execResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task ConcurrentJobClaiming_OnlyOneRunnerGetsTheJob()
    {
        SeedNotionManifest();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, "concurrent-agent");

        // Create two runners
        var (_, regToken1) = await TestHelpers.CreateRunnerAsync(dashClient, "concurrent-runner-1");
        var (_, regToken2) = await TestHelpers.CreateRunnerAsync(dashClient, "concurrent-runner-2");

        var runner1 = new MockRunnerClient(_factory.CreateClient());
        await runner1.RegisterAsync(regToken1);
        var runner2 = new MockRunnerClient(_factory.CreateClient());
        await runner2.RegisterAsync(regToken2);

        await dashClient.PostAsJsonAsync("/api/skills/notion/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/notion/credentials", new
        {
            credentials = new Dictionary<string, string> { ["apiKey"] = "k" }
        });
        await dashClient.PutAsJsonAsync("/api/skills/notion/run-target", new { runTarget = "runner" });

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        _ = agent.SkillExecAsync("notion", "search", new { query = "concurrent" });

        await Task.Delay(100);

        // Both runners try to claim
        var poll1 = runner1.PollJobAsync();
        var poll2 = runner2.PollJobAsync();
        var results = await Task.WhenAll(poll1, poll2);

        // Exactly one should get a job, the other should get 204
        var gotJob = results.Count(r => r.Response.StatusCode == HttpStatusCode.OK);
        var noJob = results.Count(r => r.Response.StatusCode == HttpStatusCode.NoContent);

        Assert.Equal(1, gotJob);
        Assert.Equal(1, noJob);
    }

    private void SeedNotionManifest()
    {
        var manifest = """
        [
          {
            "name": "notion",
            "title": "Notion",
            "emoji": "N",
            "description": "Notion workspace",
            "doc": "Access Notion pages and databases",
            "actions": {
              "search": {
                "description": "Search Notion pages",
                "params": { "type": "object", "properties": { "query": { "type": "string" } } }
              }
            },
            "credentialFields": [
              { "key": "apiKey", "label": "API Key", "kind": "secret", "required": true }
            ]
          }
        ]
        """;

        _factory.SkillRuntimeMock.Reset();
        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/manifests").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(manifest));
    }
}

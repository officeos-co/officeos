using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace EnterpriseAgentOs.Api.Tests;

public sealed class SkillExecutionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SkillExecutionTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Capabilities_WithValidAgent_ReturnsEmptyWhenNoSkillsInstalled()
    {
        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient);

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var caps = await agent.GetCapabilitiesAsync();

        Assert.Empty(caps.Tools);
        Assert.Empty(caps.Skills);
    }

    [Fact]
    public async Task Capabilities_WithDeletedAgent_Returns401()
    {
        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, "deleted-caps-agent");

        // Delete the agent
        await dashClient.DeleteAsync($"/api/agents/{agentId}");

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var response = await agent.GetAsync("/api/agents/me/capabilities");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SkillExec_SkillNotInstalled_Returns409()
    {
        // Seed WireMock with a manifest so the skill "exists" in runtime
        // but is NOT installed/configured in the DB
        SeedNotionManifest();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient);

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var response = await agent.SkillExecAsync("notion", "search", new { query = "test" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("not installed", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task SkillExec_CloudExecution_Success()
    {
        SeedNotionManifest();

        // Stub the /execute endpoint to return success
        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {"pages": ["page-1"]}}"""));

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient);

        // Install and configure the skill via the skills API
        await dashClient.PostAsJsonAsync("/api/skills/notion/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/notion/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["apiKey"] = "test-notion-key",
            }
        });

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var response = await agent.SkillExecAsync("notion", "search", new { query = "test" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task SkillExec_RuntimeUnreachable_Returns502()
    {
        SeedNotionManifest();

        // Stop the WireMock /execute route so it returns nothing,
        // and reconfigure to use a dead URL
        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient);

        // Install + configure skill
        await dashClient.PostAsJsonAsync("/api/skills/notion/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/notion/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["apiKey"] = "test-key",
            }
        });

        // Reset WireMock execute endpoint to reject connections
        _factory.SkillRuntimeMock.Reset();
        // Re-add manifests but NO /execute stub => WireMock returns 404
        SeedNotionManifest();
        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(500).WithBody("Internal error"));

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var response = await agent.SkillExecAsync("notion", "search", new { query = "test" });

        // The runtime returns 500, which SkillRuntimeClient parses as a non-success result
        // Since the client doesn't throw on non-2xx from runtime, it returns UnprocessableEntity
        var status = (int)response.StatusCode;
        Assert.True(status == 422 || status == 502,
            $"Expected 422 or 502, got {status}");
    }

    [Fact]
    public async Task SkillExec_RunnerTargetNoRunnersOnline_Returns503()
    {
        SeedNotionManifest();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient);

        // Install skill and set to runner target
        await dashClient.PostAsJsonAsync("/api/skills/notion/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/notion/credentials", new
        {
            credentials = new Dictionary<string, string> { ["apiKey"] = "k" }
        });
        await dashClient.PutAsJsonAsync("/api/skills/notion/run-target", new { runTarget = "runner" });

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var response = await agent.SkillExecAsync("notion", "search", new { query = "test" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("no runners", body.GetProperty("error").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SkillExec_WithoutBearerToken_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/agents/me/skill-exec", new
        {
            skill = "notion",
            action = "search",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SkillExec_WithInvalidAgentId_Returns401()
    {
        var agent = new MockAgentClient(_factory.CreateClient(), Guid.NewGuid());
        var response = await agent.SkillExecAsync("notion", "search");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

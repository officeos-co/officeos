using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace EnterpriseAgentOs.Api.Tests;

/// <summary>
/// End-to-end tests for per-agent skill-exec rate limiting.
///
/// These tests use a low rate limit (3 executions per window) injected via
/// RateLimitingWebApplicationFactory so tests do not have to fire 60 requests.
///
/// Tests will fail to compile until the rate-limiting implementation exists — that
/// is expected. This file defines the contract.
/// </summary>
public sealed class RateLimitingTests : IClassFixture<RateLimitingWebApplicationFactory>
{
    private readonly RateLimitingWebApplicationFactory _factory;

    public RateLimitingTests(RateLimitingWebApplicationFactory factory) => _factory = factory;

    // ---------------------------------------------------------------------------
    // 1. Under-limit execution always succeeds
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SkillExec_UnderLimit_Succeeds()
    {
        SeedEmailManifest(); // sets up /manifests and /execute stubs

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-under-limit-{Guid.NewGuid():N}");

        await InstallNotionSkillAsync(dashClient);

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);

        // Fire exactly 3 requests — the configured per-agent limit.
        for (var i = 0; i < 3; i++)
        {
            var response = await agent.SkillExecAsync("notion", "search", new { query = "test" });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    // ---------------------------------------------------------------------------
    // 2. Exceeding per-agent limit returns 429 with structured body
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task SkillExec_ExceedsPerAgentLimit_Returns429()
    {
        SeedEmailManifest();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-over-limit-{Guid.NewGuid():N}");

        await InstallNotionSkillAsync(dashClient);

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);

        // Consume all 3 allowed executions.
        for (var i = 0; i < 3; i++)
        {
            var ok = await agent.SkillExecAsync("notion", "search", new { query = "test" });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        // 4th call must be rejected.
        var response = await agent.SkillExecAsync("notion", "search", new { query = "test" });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("rate_limit_exceeded", body.GetProperty("error").GetString());

        // retryAfterSeconds must be present and positive.
        Assert.True(body.TryGetProperty("retryAfterSeconds", out var retryProp),
            "Response body must contain 'retryAfterSeconds'");
        Assert.True(retryProp.GetInt32() > 0,
            "retryAfterSeconds must be a positive integer");
    }

    // ---------------------------------------------------------------------------
    // 3. Rate limiting is per-agent — two agents each have their own counter
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task RateLimit_IsPerAgent_NotGlobal()
    {
        SeedEmailManifest();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var agentId1 = await TestHelpers.CreateAgentAsync(dashClient, $"agent1-isolation-{Guid.NewGuid():N}");
        var agentId2 = await TestHelpers.CreateAgentAsync(dashClient, $"agent2-isolation-{Guid.NewGuid():N}");

        await InstallNotionSkillAsync(dashClient);

        var agent1 = new MockAgentClient(_factory.CreateClient(), agentId1);
        var agent2 = new MockAgentClient(_factory.CreateClient(), agentId2);

        // Exhaust agent1's limit.
        for (var i = 0; i < 3; i++)
        {
            var ok = await agent1.SkillExecAsync("notion", "search", new { query = "test" });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var limited = await agent1.SkillExecAsync("notion", "search", new { query = "test" });
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);

        // Agent2 must still have a fresh counter — first execution must succeed.
        var agent2Response = await agent2.SkillExecAsync("notion", "search", new { query = "test" });
        Assert.Equal(HttpStatusCode.OK, agent2Response.StatusCode);
    }

    // ---------------------------------------------------------------------------
    // 4. After the window resets, executions succeed again
    // ---------------------------------------------------------------------------

    [Fact(Skip = "Requires sliding window reset — tested manually or with a clock-injection seam. " +
                 "Contract: after the sliding window duration elapses (e.g. 1 s when " +
                 "RateLimiting:WindowSeconds=1 in test config), a new execution must return 200 " +
                 "even when the window was previously exhausted.")]
    public async Task SkillExec_AfterWindowReset_Succeeds()
    {
        // Requires the implementation to expose a configurable window duration so
        // tests can use a 1-second window. The full flow:
        //   1. Configure limit=3, window=1s in test factory.
        //   2. Exhaust the limit.
        //   3. Verify 4th call returns 429.
        //   4. Wait 1 second (Task.Delay(1100)) so the sliding window expires.
        //   5. Verify a new call returns 200.
        //
        // This test is skipped because the base RateLimitingWebApplicationFactory
        // uses a 1-hour window and Task.Delay(3600_000) would make CI unusable.
        // Re-enable once a short test window is wired up:
        //   ["Production:RateLimiting:WindowSeconds"] = "1"
        throw new InvalidOperationException("This test must not run unless un-skipped intentionally.");
    }

    // ---------------------------------------------------------------------------
    // 5. Email skill hard limit returns 429 with email_rate_limit_exceeded
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task EmailSkill_HardLimit_Returns429()
    {
        SeedEmailManifest();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-email-limit-{Guid.NewGuid():N}");

        // Install and configure the email skill.
        await dashClient.PostAsJsonAsync("/api/skills/sendgrid/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/sendgrid/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["apiKey"] = "test-sendgrid-key",
            }
        });

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);

        // Consume exactly 2 email sends — the configured email hard limit.
        for (var i = 0; i < 2; i++)
        {
            var ok = await agent.SkillExecAsync("sendgrid", "send_email", new
            {
                to = "user@example.com",
                subject = $"Test {i}",
                body = "hello",
            });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        // 3rd call must be rejected with the email-specific error code.
        var response = await agent.SkillExecAsync("sendgrid", "send_email", new
        {
            to = "user@example.com",
            subject = "Over limit",
            body = "hello",
        });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("email_rate_limit_exceeded", body.GetProperty("error").GetString());
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Seeds WireMock with manifests for "notion" (general) and "sendgrid" (email category),
    /// plus a catch-all /execute stub that returns success.
    /// </summary>
    private void SeedEmailManifest()
    {
        const string manifests = """
        [
          {
            "name": "notion",
            "title": "Notion",
            "emoji": "N",
            "description": "Notion workspace",
            "doc": "Access Notion pages and databases",
            "category": "productivity",
            "actions": {
              "search": {
                "description": "Search Notion pages",
                "params": {
                  "type": "object",
                  "properties": { "query": { "type": "string" } }
                }
              }
            },
            "credentialFields": [
              { "key": "apiKey", "label": "API Key", "kind": "secret", "required": true }
            ]
          },
          {
            "name": "sendgrid",
            "title": "SendGrid",
            "emoji": "✉",
            "description": "Send transactional email via SendGrid",
            "doc": "Email delivery via SendGrid API",
            "category": "email",
            "actions": {
              "send_email": {
                "description": "Send an email",
                "params": {
                  "type": "object",
                  "properties": {
                    "to":      { "type": "string" },
                    "subject": { "type": "string" },
                    "body":    { "type": "string" }
                  },
                  "required": ["to", "subject", "body"]
                }
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
                .WithBody(manifests));

        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {}}"""));
    }

    private static async Task InstallNotionSkillAsync(HttpClient dashClient)
    {
        await dashClient.PostAsJsonAsync("/api/skills/notion/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/notion/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["apiKey"] = "test-notion-key",
            }
        });
    }
}

/// <summary>
/// WebApplicationFactory subclass that injects a low rate limit for fast testing.
/// Uses 3 skill executions per hour and 2 email sends per hour so tests
/// don't need to fire 60 (or 10) real requests.
///
/// All other config is inherited from <see cref="CustomWebApplicationFactory"/>.
/// </summary>
public sealed class RateLimitingWebApplicationFactory : CustomWebApplicationFactory
{
    protected override void SetAdditionalTestConfig(IDictionary<string, string?> config)
    {
        // Low limits so test loops only need 3–4 iterations.
        config["Production:RateLimiting:SkillExecPerAgentPerHour"] = "3";
        config["Production:RateLimiting:EmailPerAgentPerHour"] = "2";
    }
}

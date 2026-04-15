using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Tests.Infrastructure;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace EnterpriseAgentOs.Api.Tests;

public sealed class ApprovalQueueTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ApprovalQueueTests(CustomWebApplicationFactory factory) => _factory = factory;

    // -------------------------------------------------------------------------
    // Test 1 — skill with requiresApproval: false executes immediately
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SkillExec_ApprovalNotRequired_ExecutesImmediately()
    {
        SeedGmailManifest(requiresApproval: false);

        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {"messageId": "msg-123"}}"""));

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-no-approval-{Guid.NewGuid():N}");

        await dashClient.PostAsJsonAsync("/api/skills/gmail/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/gmail/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["accessToken"] = "test-access-token",
            }
        });

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var response = await agent.SkillExecAsync("gmail", "send", new { to = "test@example.com", subject = "Hello", body = "World" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("success").GetBoolean());
    }

    // -------------------------------------------------------------------------
    // Test 2 — skill with requiresApproval: true returns 202 with pending status
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SkillExec_ApprovalRequired_Returns202WithPendingStatus()
    {
        SeedGmailManifest(requiresApproval: true);

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-approval-{Guid.NewGuid():N}");

        await dashClient.PostAsJsonAsync("/api/skills/gmail/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/gmail/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["accessToken"] = "test-access-token",
            }
        });

        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var response = await agent.SkillExecAsync("gmail", "send", new { to = "test@example.com", subject = "Approve me", body = "Please approve" });

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("approval_pending", body.GetProperty("status").GetString());
        Assert.True(body.GetProperty("approvalRequestId").GetGuid() != Guid.Empty);
    }

    // -------------------------------------------------------------------------
    // Test 3 — list pending approval requests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApprovalQueue_ListPendingRequests()
    {
        SeedGmailManifest(requiresApproval: true);

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-list-{Guid.NewGuid():N}");

        await dashClient.PostAsJsonAsync("/api/skills/gmail/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/gmail/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["accessToken"] = "test-access-token",
            }
        });

        // Trigger the approval-required skill exec to create a pending request
        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var execResponse = await agent.SkillExecAsync("gmail", "send", new { to = "list@example.com", subject = "List test", body = "Body" });
        Assert.Equal(HttpStatusCode.Accepted, execResponse.StatusCode);

        // List pending approvals via dashboard
        var listResponse = await dashClient.GetAsync("/api/approval-requests");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var body = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        // Response should be an array (or have an "items" property) containing our request
        var items = body.ValueKind == JsonValueKind.Array
            ? body
            : body.GetProperty("items");

        Assert.True(items.GetArrayLength() >= 1, "Expected at least one pending approval request");

        var found = false;
        foreach (var item in items.EnumerateArray())
        {
            if (item.GetProperty("skillName").GetString() == "gmail"
                && item.GetProperty("status").GetString() == "pending")
            {
                found = true;
                break;
            }
        }
        Assert.True(found, "Expected a pending gmail approval request in the list");
    }

    // -------------------------------------------------------------------------
    // Test 4 — approve executes skill and returns result
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApprovalRequest_Approve_ExecutesSkillAndReturnsResult()
    {
        SeedGmailManifest(requiresApproval: true);

        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {"messageId": "approved-msg-456"}}"""));

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-approve-{Guid.NewGuid():N}");

        await dashClient.PostAsJsonAsync("/api/skills/gmail/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/gmail/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["accessToken"] = "test-access-token",
            }
        });

        // Create pending approval request
        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var execResponse = await agent.SkillExecAsync("gmail", "send", new { to = "approve@example.com", subject = "Approve this", body = "Please" });
        Assert.Equal(HttpStatusCode.Accepted, execResponse.StatusCode);

        var execBody = await execResponse.Content.ReadFromJsonAsync<JsonElement>();
        var approvalRequestId = execBody.GetProperty("approvalRequestId").GetGuid();

        // Approve via dashboard
        var approveResponse = await dashClient.PostAsJsonAsync($"/api/approval-requests/{approvalRequestId}/approve", new { });
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        var approveBody = await approveResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("approved", approveBody.GetProperty("status").GetString());

        // resultJson should be set (either directly on the response or check via GET)
        var getResponse = await dashClient.GetAsync($"/api/approval-requests/{approvalRequestId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("approved", getBody.GetProperty("status").GetString());
        Assert.True(getBody.TryGetProperty("resultJson", out var resultJson) && resultJson.ValueKind != JsonValueKind.Null,
            "Expected resultJson to be set after approval");
        Assert.True(getBody.TryGetProperty("decidedAt", out var decidedAt) && decidedAt.ValueKind != JsonValueKind.Null,
            "Expected decidedAt to be set after approval");
    }

    // -------------------------------------------------------------------------
    // Test 5 — reject does NOT call skill-runtime /execute
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApprovalRequest_Reject_DoesNotExecuteSkill()
    {
        SeedGmailManifest(requiresApproval: true);

        // Stub /execute so we can count calls
        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {}}"""));

        var executeCalls = CountExecuteCalls();

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-reject-{Guid.NewGuid():N}");

        await dashClient.PostAsJsonAsync("/api/skills/gmail/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/gmail/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["accessToken"] = "test-access-token",
            }
        });

        // Create pending approval request
        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var execResponse = await agent.SkillExecAsync("gmail", "send", new { to = "reject@example.com", subject = "Reject me", body = "Nope" });
        Assert.Equal(HttpStatusCode.Accepted, execResponse.StatusCode);

        var execBody = await execResponse.Content.ReadFromJsonAsync<JsonElement>();
        var approvalRequestId = execBody.GetProperty("approvalRequestId").GetGuid();

        // Record execute call count BEFORE reject (skill-exec to create the request should not call /execute)
        var callsBeforeReject = CountExecuteCalls();

        // Reject via dashboard
        var rejectResponse = await dashClient.PostAsJsonAsync($"/api/approval-requests/{approvalRequestId}/reject", new { });
        Assert.Equal(HttpStatusCode.OK, rejectResponse.StatusCode);

        // Verify /execute was NOT called during reject
        var callsAfterReject = CountExecuteCalls();
        Assert.Equal(callsBeforeReject, callsAfterReject);

        // Verify status is rejected
        var getResponse = await dashClient.GetAsync($"/api/approval-requests/{approvalRequestId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var getBody = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("rejected", getBody.GetProperty("status").GetString());
    }

    // -------------------------------------------------------------------------
    // Test 6 — agent polls for approved request and gets the result
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AgentPolling_ApprovedRequest_ReturnsResult()
    {
        SeedGmailManifest(requiresApproval: true);

        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {"messageId": "polled-msg-789"}}"""));

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-poll-{Guid.NewGuid():N}");

        await dashClient.PostAsJsonAsync("/api/skills/gmail/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/gmail/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["accessToken"] = "test-access-token",
            }
        });

        // Agent submits skill exec — gets approval pending
        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var execResponse = await agent.SkillExecAsync("gmail", "send", new { to = "poll@example.com", subject = "Poll me", body = "Waiting" });
        Assert.Equal(HttpStatusCode.Accepted, execResponse.StatusCode);

        var execBody = await execResponse.Content.ReadFromJsonAsync<JsonElement>();
        var approvalRequestId = execBody.GetProperty("approvalRequestId").GetGuid();

        // Dashboard operator approves it
        var approveResponse = await dashClient.PostAsJsonAsync($"/api/approval-requests/{approvalRequestId}/approve", new { });
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        // Agent polls for result using bearer auth
        var pollResponse = await agent.GetAsync($"/api/agents/me/approval-requests/{approvalRequestId}");
        Assert.Equal(HttpStatusCode.OK, pollResponse.StatusCode);

        var pollBody = await pollResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("approved", pollBody.GetProperty("status").GetString());

        // Result should be accessible to the agent
        Assert.True(pollBody.TryGetProperty("result", out var result) && result.ValueKind != JsonValueKind.Null,
            "Expected result to be present when polling an approved request");
    }

    // -------------------------------------------------------------------------
    // Test 7 — approval queue requires dashboard auth
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApprovalQueue_RequiresDashboardAuth()
    {
        var unauthenticatedClient = _factory.CreateClient();

        var response = await unauthenticatedClient.GetAsync("/api/approval-requests");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Test 8 — per-skill override makes approval required even if manifest says false
    // -------------------------------------------------------------------------

    [Fact]
    public async Task ApprovalRequest_SetOverride_SkillDefaultFalseBecomesRequired()
    {
        SeedGmailManifest(requiresApproval: false);

        var dashClient = await TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await TestHelpers.CreateAgentAsync(dashClient, $"agent-override-{Guid.NewGuid():N}");

        await dashClient.PostAsJsonAsync("/api/skills/gmail/install", new { });
        await dashClient.PutAsJsonAsync("/api/skills/gmail/credentials", new
        {
            credentials = new Dictionary<string, string>
            {
                ["accessToken"] = "test-access-token",
            }
        });

        // Override: force approval required even though manifest says false
        var overrideResponse = await dashClient.PutAsJsonAsync("/api/skills/gmail/approval", new
        {
            requiresApproval = true
        });
        Assert.Equal(HttpStatusCode.OK, overrideResponse.StatusCode);

        // Now executing the skill should be parked, not executed
        var agent = new MockAgentClient(_factory.CreateClient(), agentId);
        var execResponse = await agent.SkillExecAsync("gmail", "send", new { to = "override@example.com", subject = "Override test", body = "Should need approval" });

        Assert.Equal(HttpStatusCode.Accepted, execResponse.StatusCode);
        var body = await execResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("approval_pending", body.GetProperty("status").GetString());
        Assert.True(body.GetProperty("approvalRequestId").GetGuid() != Guid.Empty);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SeedGmailManifest(bool requiresApproval)
    {
        var requiresApprovalValue = requiresApproval ? "true" : "false";

        var manifest = $$"""
        [
          {
            "name": "gmail",
            "title": "Gmail",
            "emoji": "📧",
            "description": "Google Gmail integration",
            "doc": "Send and read Gmail messages",
            "requiresApproval": {{requiresApprovalValue}},
            "actions": {
              "send": {
                "description": "Send an email via Gmail",
                "params": {
                  "type": "object",
                  "properties": {
                    "to": { "type": "string" },
                    "subject": { "type": "string" },
                    "body": { "type": "string" }
                  },
                  "required": ["to", "subject", "body"]
                }
              }
            },
            "credentialFields": [
              { "key": "accessToken", "label": "Access Token", "kind": "secret", "required": true }
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

    /// <summary>
    /// Returns the number of times WireMock has received a POST /execute request.
    /// </summary>
    private int CountExecuteCalls()
    {
        return _factory.SkillRuntimeMock.LogEntries
            .Count(entry => entry.RequestMessage.Path == "/execute"
                         && entry.RequestMessage.Method == "POST");
    }
}

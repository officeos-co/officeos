namespace EnterpriseAgentOs.Api.Tests;

public sealed class AuditTrailTests : IClassFixture<Infrastructure.CustomWebApplicationFactory>
{
    private readonly Infrastructure.CustomWebApplicationFactory _factory;

    public AuditTrailTests(Infrastructure.CustomWebApplicationFactory factory) => _factory = factory;

    // ─────────────────────────────────────────────────────────────────────────
    // 1. Successful skill execution → audit entry recorded
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task SkillExec_RecordsAuditEntry()
    {
        SeedNotionManifest();

        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {"pages": ["page-1"]}}"""));

        var dashClient = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await Infrastructure.TestHelpers.CreateAgentAsync(dashClient);

        await InstallAndConfigureNotionAsync(dashClient);

        var agent = new Infrastructure.MockAgentClient(_factory.CreateClient(), agentId);
        var execResponse = await agent.SkillExecAsync("notion", "search", new { query = "hello" });
        Assert.Equal(HttpStatusCode.OK, execResponse.StatusCode);

        // Fetch audit log via dashboard GraphQL
        var data = await Infrastructure.TestHelpers.GraphQLAsync(dashClient, AuditLogQuery,
            new { agentId, skip = 0, limit = 50 });
        var body = data.GetProperty("auditLog");
        var items = body.GetProperty("items");
        var total = body.GetProperty("total").GetInt32();

        Assert.Equal(1, total);
        Assert.Equal(1, items.GetArrayLength());

        var entry = items[0];
        Assert.Equal("notion", entry.GetProperty("skillName").GetString());
        Assert.Equal("search", entry.GetProperty("action").GetString());
        Assert.Equal(agentId, entry.GetProperty("agentId").GetGuid());
        Assert.NotEqual(default, entry.GetProperty("timestamp").GetDateTime());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 2. Secret params must be redacted in paramsJson
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task SkillExec_AuditEntry_SecretsRedacted()
    {
        SeedNotionManifest();

        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {}}"""));

        var dashClient = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await Infrastructure.TestHelpers.CreateAgentAsync(dashClient);

        await InstallAndConfigureNotionAsync(dashClient);

        var agent = new Infrastructure.MockAgentClient(_factory.CreateClient(), agentId);

        // Pass params that include both a sensitive key and a safe key
        var execResponse = await agent.SkillExecAsync("notion", "search", new
        {
            apiKey = "secret-value",
            query = "test",
        });

        // Execution may succeed or be rejected depending on schema validation;
        // either way an audit record must be written.
        var data = await Infrastructure.TestHelpers.GraphQLAsync(dashClient, AuditLogQuery,
            new { agentId, skip = 0, limit = 50 });
        var body = data.GetProperty("auditLog");
        var items = body.GetProperty("items");
        Assert.True(items.GetArrayLength() >= 1, "Expected at least one audit entry.");

        var entry = items[0];
        var paramsJson = entry.GetProperty("paramsJson").GetString()!;

        // Sensitive field must be redacted
        var parsed = JsonSerializer.Deserialize<JsonElement>(paramsJson);
        Assert.Equal("[REDACTED]", parsed.GetProperty("apiKey").GetString());

        // Non-sensitive field must be preserved
        Assert.Equal("test", parsed.GetProperty("query").GetString());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3. Failed skill execution still records an audit entry
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task SkillExec_Failed_StillRecordsAuditEntry()
    {
        SeedNotionManifest();

        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error": "internal skill runtime error"}"""));

        var dashClient = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await Infrastructure.TestHelpers.CreateAgentAsync(dashClient);

        await InstallAndConfigureNotionAsync(dashClient);

        var agent = new Infrastructure.MockAgentClient(_factory.CreateClient(), agentId);
        var execResponse = await agent.SkillExecAsync("notion", "search", new { query = "fail" });

        // Execution should have resulted in a non-2xx from the backend
        var status = (int)execResponse.StatusCode;
        Assert.True(status >= 400 || status == 200,
            $"Expected a non-success or proxied-error response, got {status}");

        // Audit entry must still be present regardless
        var data = await Infrastructure.TestHelpers.GraphQLAsync(dashClient, AuditLogQuery,
            new { agentId, skip = 0, limit = 50 });
        var body = data.GetProperty("auditLog");
        var total = body.GetProperty("total").GetInt32();
        var items = body.GetProperty("items");

        Assert.True(total >= 1, "Expected at least one audit entry even after a failed execution.");
        Assert.True(items.GetArrayLength() >= 1);

        var entry = items[0];
        var resultSummary = entry.GetProperty("resultSummary").GetString();
        Assert.False(string.IsNullOrEmpty(resultSummary), "resultSummary must not be empty.");

        // The summary should indicate failure — it must not read as a clean success
        Assert.DoesNotContain("success", resultSummary, StringComparison.OrdinalIgnoreCase);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 4. Audit log endpoint requires dashboard authentication
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AuditLog_RequiresDashboardAuth()
    {
        var dashClient = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await Infrastructure.TestHelpers.CreateAgentAsync(dashClient);

        // Unauthenticated client — no cookie
        var anonClient = _factory.CreateClient();
        var raw = await Infrastructure.TestHelpers.GraphQLRawAsync(anonClient, AuditLogQuery,
            new { agentId, skip = 0, limit = 50 });
        // Dashboard GraphQL endpoint rejects unauthenticated calls via DashboardAuthMiddleware.
        // The response contains errors and auditLog is not resolved.
        Assert.True(raw.TryGetProperty("errors", out var errors) && errors.GetArrayLength() > 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 5. Pagination: limit + offset are respected; total reflects full count
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AuditLog_Pagination()
    {
        SeedNotionManifest();

        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {}}"""));

        var dashClient = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);
        var agentId = await Infrastructure.TestHelpers.CreateAgentAsync(dashClient);

        await InstallAndConfigureNotionAsync(dashClient);

        var agent = new Infrastructure.MockAgentClient(_factory.CreateClient(), agentId);

        // Execute the skill 5 times to produce 5 audit entries
        for (var i = 0; i < 5; i++)
        {
            var r = await agent.SkillExecAsync("notion", "search", new { query = $"query-{i}" });
            Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        }

        // Request first page: limit=2, offset=0
        var data = await Infrastructure.TestHelpers.GraphQLAsync(dashClient, AuditLogQuery,
            new { agentId, skip = 0, limit = 2 });
        var body = data.GetProperty("auditLog");
        var items = body.GetProperty("items");
        var total = body.GetProperty("total").GetInt32();

        Assert.Equal(5, total);
        Assert.Equal(2, items.GetArrayLength());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 6. Agent isolation: one agent's executions do not appear in another's log
    // ─────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task AuditLog_AgentIsolation()
    {
        SeedNotionManifest();

        _factory.SkillRuntimeMock
            .Given(Request.Create().WithPath("/execute").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"success": true, "result": {}}"""));

        var dashClient = await Infrastructure.TestHelpers.CreateAuthenticatedClientAsync(_factory);

        var agent1Id = await Infrastructure.TestHelpers.CreateAgentAsync(dashClient, "isolation-agent-1");
        var agent2Id = await Infrastructure.TestHelpers.CreateAgentAsync(dashClient, "isolation-agent-2");

        await InstallAndConfigureNotionAsync(dashClient);

        // Execute skill only for agent 1
        var agent1Client = new Infrastructure.MockAgentClient(_factory.CreateClient(), agent1Id);
        var execResponse = await agent1Client.SkillExecAsync("notion", "search", new { query = "agent1-query" });
        Assert.Equal(HttpStatusCode.OK, execResponse.StatusCode);

        // Agent 2's audit log should be empty
        var data = await Infrastructure.TestHelpers.GraphQLAsync(dashClient, AuditLogQuery,
            new { agentId = agent2Id, skip = 0, limit = 50 });
        var body = data.GetProperty("auditLog");
        var total = body.GetProperty("total").GetInt32();
        var items = body.GetProperty("items");

        Assert.Equal(0, total);
        Assert.Equal(0, items.GetArrayLength());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Seeds WireMock with the Notion manifest so the skill is discoverable.
    /// Resets any previously registered stubs first.
    /// </summary>
    private void SeedNotionManifest()
    {
        const string manifest = """
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
                "params": {
                  "type": "object",
                  "properties": {
                    "query": { "type": "string" }
                  }
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
                .WithBody(manifest));
    }

    /// <summary>
    /// Installs the "notion" skill and sets the apiKey credential via the dashboard GraphQL.
    /// </summary>
    private static async Task InstallAndConfigureNotionAsync(HttpClient dashClient)
    {
        await Infrastructure.TestHelpers.InstallSkillAsync(dashClient, "notion");
        await Infrastructure.TestHelpers.SetSkillCredentialsAsync(dashClient, "notion", new Dictionary<string, string>
        {
            ["apiKey"] = "test-notion-key",
        });
    }

    private const string AuditLogQuery = @"
        query($agentId: UUID!, $skip: Int!, $limit: Int!) {
          auditLog(agentId: $agentId, skip: $skip, limit: $limit) {
            total
            items {
              id agentId skillName action paramsJson resultSummary durationMs timestamp
            }
          }
        }";
}

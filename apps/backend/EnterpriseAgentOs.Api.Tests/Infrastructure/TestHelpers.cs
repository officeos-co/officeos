namespace EnterpriseAgentOs.Api.Tests.Infrastructure;

public static class TestHelpers
{
    private const string DashboardGraphQLPath = "/api/dashboard/graphql";

    /// <summary>
    /// Posts a GraphQL operation to the dashboard schema, throws on a non-success
    /// HTTP status, throws if the response contains an "errors" array, and returns
    /// the "data" property as a <see cref="JsonElement"/>.
    /// </summary>
    public static async Task<JsonElement> GraphQLAsync(
        HttpClient client,
        string query,
        object? variables = null)
    {
        var payload = variables is null
            ? (object)new { query }
            : new { query, variables };

        var response = await client.PostAsJsonAsync(DashboardGraphQLPath, payload);
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array
            && errors.GetArrayLength() > 0)
        {
            throw new InvalidOperationException("GraphQL errors: " + errors.ToString());
        }

        return root.GetProperty("data").Clone();
    }

    /// <summary>
    /// Like <see cref="GraphQLAsync"/> but does not throw on "errors" — returns the
    /// whole response (with "data" and/or "errors") so tests can assert on error
    /// shape.
    /// </summary>
    public static async Task<JsonElement> GraphQLRawAsync(
        HttpClient client,
        string query,
        object? variables = null)
    {
        var payload = variables is null
            ? (object)new { query }
            : new { query, variables };

        var response = await client.PostAsJsonAsync(DashboardGraphQLPath, payload);
        // Don't throw on non-success — caller inspects body.
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    /// <summary>
    /// Seeds a user + session directly in the DB and returns an HttpClient
    /// with the eaos-session cookie set, simulating a logged-in dashboard user.
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        CustomWebApplicationFactory factory,
        string? email = null,
        string name = "Test User")
    {
        var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = EnterpriseAgentOs.Api.Middleware.SessionAuthMiddleware.HashToken(sessionToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EaosDbContext>();

        email ??= $"test-{Guid.NewGuid():N}@example.com";

        var user = new UserRecord
        {
            Email = email,
            Name = name,
            GoogleSubjectId = $"google-{Guid.NewGuid():N}",
        };
        db.Users.Add(user);

        var session = new SessionRecord
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"eaos-session={Uri.EscapeDataString(sessionToken)}");
        return client;
    }

    /// <summary>
    /// Creates an agent via the dashboard GraphQL and returns its ID.
    /// Requires an authenticated client.
    /// </summary>
    public static async Task<Guid> CreateAgentAsync(HttpClient client, string name = "test-agent", string provider = "ollama")
    {
        const string mutation = @"
            mutation($input: CreateAgentInput!) {
              createAgent(input: $input) { id }
            }";
        var data = await GraphQLAsync(client, mutation, new
        {
            input = new
            {
                name,
                provider,
                model = (string?)null,
                prompt = (string?)null,
                integrationSlugs = (string[]?)null,
                channelSlugs = (string[]?)null,
            }
        });
        return data.GetProperty("createAgent").GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Seeds a user + expired session and returns an HttpClient with that cookie.
    /// </summary>
    public static async Task<HttpClient> CreateExpiredSessionClientAsync(CustomWebApplicationFactory factory)
    {
        var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = EnterpriseAgentOs.Api.Middleware.SessionAuthMiddleware.HashToken(sessionToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EaosDbContext>();

        var user = new UserRecord
        {
            Email = $"expired-{Guid.NewGuid():N}@example.com",
            Name = "Expired User",
            GoogleSubjectId = $"google-{Guid.NewGuid():N}",
        };
        db.Users.Add(user);

        var session = new SessionRecord
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddDays(-1), // expired
        };
        db.Sessions.Add(session);
        await db.SaveChangesAsync();

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"eaos-session={Uri.EscapeDataString(sessionToken)}");
        return client;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Shared dashboard helpers for the most common GraphQL calls.
    // ─────────────────────────────────────────────────────────────────────────

    public static async Task InstallSkillAsync(HttpClient client, string name)
    {
        const string mutation = @"mutation($name: String!) { installSkill(name: $name) }";
        await GraphQLAsync(client, mutation, new { name });
    }

    public static async Task SetSkillCredentialsAsync(
        HttpClient client,
        string name,
        Dictionary<string, string> credentials)
    {
        const string mutation =
            @"mutation($name: String!, $credentials: [SkillCredentialEntryInput!]!) {
                setSkillCredentials(name: $name, credentials: $credentials)
              }";
        var entries = credentials.Select(kv => new { key = kv.Key, value = kv.Value }).ToArray();
        await GraphQLAsync(client, mutation, new { name, credentials = entries });
    }

}

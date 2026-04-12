using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Database.Models;
using EnterpriseAgentOs.Api.Entities.Auth;

namespace EnterpriseAgentOs.Api.Tests.Infrastructure;

public static class TestHelpers
{
    /// <summary>
    /// Seeds a user + session directly in the DB and returns an HttpClient
    /// with the eaos-session cookie set, simulating a logged-in dashboard user.
    /// </summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        CustomWebApplicationFactory factory,
        string email = "test@example.com",
        string name = "Test User")
    {
        var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = SessionAuthMiddleware.HashToken(sessionToken);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EaosDbContext>();

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
    /// Creates an agent via the API and returns its ID.
    /// Requires an authenticated client.
    /// </summary>
    public static async Task<Guid> CreateAgentAsync(HttpClient client, string name = "test-agent", string provider = "ollama")
    {
        var response = await client.PostAsJsonAsync("/api/agents", new
        {
            name,
            provider,
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetGuid();
    }

    /// <summary>
    /// Creates a runner via the dashboard API and returns the registration token.
    /// </summary>
    public static async Task<(Guid Id, string RegistrationToken)> CreateRunnerAsync(
        HttpClient client,
        string name = "test-runner")
    {
        var response = await client.PostAsJsonAsync("/api/runners", new { name });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (
            body.GetProperty("id").GetGuid(),
            body.GetProperty("registrationToken").GetString()!
        );
    }

    /// <summary>
    /// Seeds a user + expired session and returns an HttpClient with that cookie.
    /// </summary>
    public static async Task<HttpClient> CreateExpiredSessionClientAsync(CustomWebApplicationFactory factory)
    {
        var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = SessionAuthMiddleware.HashToken(sessionToken);

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
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace EnterpriseAgentOs.Api.Tests.Infrastructure;

/// <summary>
/// Simulates what a self-hosted runner does: registers, polls for jobs,
/// posts results, sends heartbeats.
/// </summary>
public sealed class MockRunnerClient
{
    private readonly HttpClient _http;
    private string? _authToken;

    public MockRunnerClient(HttpClient http)
    {
        _http = http;
    }

    public string? AuthToken => _authToken;

    /// <summary>Register with a one-time registration token.</summary>
    public async Task<HttpResponseMessage> RegisterAsync(string registrationToken, string? version = null)
    {
        var response = await _http.PostAsJsonAsync("/api/runner/register", new
        {
            registrationToken,
            version,
        });

        if (response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            _authToken = body.GetProperty("authToken").GetString();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _authToken);
        }

        return response;
    }

    /// <summary>Poll for the next pending job.</summary>
    public async Task<(HttpResponseMessage Response, JsonElement? Job)> PollJobAsync()
    {
        var response = await _http.GetAsync("/api/runner/jobs");
        if (response.StatusCode == HttpStatusCode.NoContent)
            return (response, null);

        if (response.IsSuccessStatusCode)
        {
            var job = await response.Content.ReadFromJsonAsync<JsonElement>();
            return (response, job);
        }

        return (response, null);
    }

    /// <summary>Post the result of a completed job.</summary>
    public async Task<HttpResponseMessage> PostResultAsync(Guid jobId, bool success, object? result = null, string? error = null)
    {
        return await _http.PostAsJsonAsync($"/api/runner/jobs/{jobId}/result", new
        {
            success,
            result,
            error,
        });
    }

    /// <summary>Send a heartbeat.</summary>
    public async Task<HttpResponseMessage> HeartbeatAsync()
    {
        return await _http.PostAsync("/api/runner/heartbeat", null);
    }

    /// <summary>List available custom skills.</summary>
    public async Task<HttpResponseMessage> ListSkillsAsync()
    {
        return await _http.GetAsync("/api/runner/skills");
    }
}

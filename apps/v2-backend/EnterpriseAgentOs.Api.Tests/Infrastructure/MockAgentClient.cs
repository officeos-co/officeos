using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EnterpriseAgentOs.Api.Entities.Skills;

namespace EnterpriseAgentOs.Api.Tests.Infrastructure;

/// <summary>
/// Simulates what the zeroclaw Rust agent does: calls capabilities, skill-exec,
/// and chat completions via the backend API using Bearer agent-uuid auth.
/// </summary>
public sealed class MockAgentClient
{
    private readonly HttpClient _http;
    private readonly Guid _agentId;

    public MockAgentClient(HttpClient http, Guid agentId)
    {
        _http = http;
        _agentId = agentId;
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", agentId.ToString());
    }

    public Guid AgentId => _agentId;

    /// <summary>Fetch capabilities (what zeroclaw does on boot + every 30s).</summary>
    public async Task<CapabilitiesResponse> GetCapabilitiesAsync()
    {
        var response = await _http.GetAsync("/api/agents/me/capabilities");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CapabilitiesResponse>())!;
    }

    /// <summary>Execute a skill (what zeroclaw does when LLM returns a tool call).</summary>
    public async Task<HttpResponseMessage> SkillExecAsync(string skill, string action, object? @params = null)
    {
        return await _http.PostAsJsonAsync("/api/agents/me/skill-exec", new
        {
            skill,
            action,
            @params = @params ?? new Dictionary<string, object>(),
        });
    }

    /// <summary>Simulate an LLM call (what zeroclaw sends every turn).</summary>
    public async Task<HttpResponseMessage> ChatCompletionAsync(object body)
    {
        return await _http.PostAsJsonAsync("/v1/chat/completions", body);
    }

    /// <summary>Raw GET with agent auth.</summary>
    public Task<HttpResponseMessage> GetAsync(string path)
        => _http.GetAsync(path);
}

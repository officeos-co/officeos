using System.Net.Http.Headers;
using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills.Implementations;

public sealed record GithubListReposRequest(string Visibility = "all", int PerPage = 30);
public sealed record GithubRepoRequest(string Owner, string Repo, string State = "open");

public sealed class GithubSkill
{
    private const string GithubApi = "https://api.github.com";

    private readonly HttpClient _http;

    public GithubSkill(HttpClient http)
    {
        _http = http;
    }

    public async Task<object> ListReposAsync(
        GithubListReposRequest req,
        IReadOnlyDictionary<string, string> creds,
        CancellationToken ct = default)
    {
        var token = GetRequired(creds, "token");
        var data = await CallAsync(
            HttpMethod.Get,
            $"/user/repos?visibility={Uri.EscapeDataString(req.Visibility)}&per_page={req.PerPage}",
            token,
            ct);

        var repos = new List<object>();
        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in data.EnumerateArray())
            {
                repos.Add(new
                {
                    full_name = Prop(r, "full_name"),
                    @private = r.TryGetProperty("private", out var pv) ? pv.GetBoolean() : (bool?)null,
                    description = Prop(r, "description"),
                    html_url = Prop(r, "html_url"),
                    default_branch = Prop(r, "default_branch"),
                });
            }
        }
        return new { repos };
    }

    public async Task<object> ListIssuesAsync(
        GithubRepoRequest req,
        IReadOnlyDictionary<string, string> creds,
        CancellationToken ct = default)
    {
        var token = GetRequired(creds, "token");
        var data = await CallAsync(
            HttpMethod.Get,
            $"/repos/{Uri.EscapeDataString(req.Owner)}/{Uri.EscapeDataString(req.Repo)}/issues?state={Uri.EscapeDataString(req.State)}",
            token,
            ct);

        var issues = new List<object>();
        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var i in data.EnumerateArray())
            {
                // Filter out PRs masquerading as issues
                if (i.TryGetProperty("pull_request", out _)) continue;
                issues.Add(new
                {
                    number = i.TryGetProperty("number", out var n) ? n.GetInt64() : (long?)null,
                    title = Prop(i, "title"),
                    state = Prop(i, "state"),
                    author = i.TryGetProperty("user", out var u) && u.ValueKind == JsonValueKind.Object
                        ? Prop(u, "login")
                        : null,
                    html_url = Prop(i, "html_url"),
                });
            }
        }
        return new { issues };
    }

    public async Task<object> ListPrsAsync(
        GithubRepoRequest req,
        IReadOnlyDictionary<string, string> creds,
        CancellationToken ct = default)
    {
        var token = GetRequired(creds, "token");
        var data = await CallAsync(
            HttpMethod.Get,
            $"/repos/{Uri.EscapeDataString(req.Owner)}/{Uri.EscapeDataString(req.Repo)}/pulls?state={Uri.EscapeDataString(req.State)}",
            token,
            ct);

        var prs = new List<object>();
        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var pr in data.EnumerateArray())
            {
                prs.Add(new
                {
                    number = pr.TryGetProperty("number", out var n) ? n.GetInt64() : (long?)null,
                    title = Prop(pr, "title"),
                    state = Prop(pr, "state"),
                    author = pr.TryGetProperty("user", out var u) && u.ValueKind == JsonValueKind.Object
                        ? Prop(u, "login")
                        : null,
                    html_url = Prop(pr, "html_url"),
                    draft = pr.TryGetProperty("draft", out var d) ? d.GetBoolean() : (bool?)null,
                });
            }
        }
        return new { prs };
    }

    private async Task<JsonElement> CallAsync(
        HttpMethod method,
        string path,
        string token,
        CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, $"{GithubApi}{path}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Accept.ParseAdd("application/vnd.github+json");
        req.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        req.Headers.UserAgent.ParseAdd("eaos-skill-gateway/1.0");
        using var resp = await _http.SendAsync(req, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"GitHub API {(int)resp.StatusCode}: {Trim(text, 500)}");
        }
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    private static string? Prop(JsonElement e, string key) =>
        e.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string GetRequired(IReadOnlyDictionary<string, string> creds, string key)
    {
        if (!creds.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"GitHub skill: missing credential '{key}'");
        }
        return value;
    }

    private static string Trim(string s, int max) => s.Length <= max ? s : s[..max];
}

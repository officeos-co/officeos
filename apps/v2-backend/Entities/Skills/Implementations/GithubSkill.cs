using System.Net.Http.Headers;
using System.Text.Json;
using EnterpriseAgentOs.Api.Entities.Skills.GraphQL.Types;

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

    public async Task<List<GithubRepo>> ListReposAsync(
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

        var repos = new List<GithubRepo>();
        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var r in data.EnumerateArray())
            {
                repos.Add(new GithubRepo
                {
                    FullName = Prop(r, "full_name"),
                    Private = r.TryGetProperty("private", out var pv) ? pv.GetBoolean() : null,
                    Description = Prop(r, "description"),
                    HtmlUrl = Prop(r, "html_url"),
                    DefaultBranch = Prop(r, "default_branch"),
                });
            }
        }
        return repos;
    }

    public async Task<List<GithubIssue>> ListIssuesAsync(
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

        var issues = new List<GithubIssue>();
        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var i in data.EnumerateArray())
            {
                if (i.TryGetProperty("pull_request", out _)) continue;
                issues.Add(new GithubIssue
                {
                    Number = i.TryGetProperty("number", out var n) ? n.GetInt64() : null,
                    Title = Prop(i, "title"),
                    State = Prop(i, "state"),
                    Author = i.TryGetProperty("user", out var u) && u.ValueKind == JsonValueKind.Object
                        ? Prop(u, "login")
                        : null,
                    HtmlUrl = Prop(i, "html_url"),
                });
            }
        }
        return issues;
    }

    public async Task<List<GithubPr>> ListPrsAsync(
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

        var prs = new List<GithubPr>();
        if (data.ValueKind == JsonValueKind.Array)
        {
            foreach (var pr in data.EnumerateArray())
            {
                prs.Add(new GithubPr
                {
                    Number = pr.TryGetProperty("number", out var n) ? n.GetInt64() : null,
                    Title = Prop(pr, "title"),
                    State = Prop(pr, "state"),
                    Author = pr.TryGetProperty("user", out var u) && u.ValueKind == JsonValueKind.Object
                        ? Prop(u, "login")
                        : null,
                    HtmlUrl = Prop(pr, "html_url"),
                    Draft = pr.TryGetProperty("draft", out var d) ? d.GetBoolean() : null,
                });
            }
        }
        return prs;
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

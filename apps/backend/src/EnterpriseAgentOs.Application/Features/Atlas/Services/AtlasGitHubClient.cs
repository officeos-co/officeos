using System.Net.Http.Headers;
using System.Text.Json.Nodes;

namespace EnterpriseAgentOs.Application.Features.Agents.Integrations;

internal sealed class GitHubIntegrationClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOAuthTokenRepository _oauthTokenRepository;
    private readonly CredentialProtector _credentialProtector;

    public GitHubIntegrationClient(
        IHttpClientFactory httpClientFactory,
        IOAuthTokenRepository oauthTokenRepository,
        CredentialProtector credentialProtector)
    {
        _httpClientFactory = httpClientFactory;
        _oauthTokenRepository = oauthTokenRepository;
        _credentialProtector = credentialProtector;
    }

    public async Task<bool> HasTokenAsync(CancellationToken ct)
        => !string.IsNullOrWhiteSpace(await GetAccessTokenAsync(ct));

    public async Task ValidateRepositoriesAsync(IReadOnlyList<string> repositories, CancellationToken ct)
    {
        foreach (var repository in repositories)
        {
            var (owner, repo) = SplitRepository(repository);
            await SendAsync(HttpMethod.Get, $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}", ct);
        }
    }

    public async Task<IReadOnlyList<JsonObject>> FetchEntityAsync(string entity, string repository, int perPage, CancellationToken ct)
    {
        var (owner, repo) = SplitRepository(repository);
        return entity switch
        {
            "repositories" => [await GetRepositoryAsync(owner, repo, ct)],
            "issues" => await GetArrayAsync($"repos/{owner}/{repo}/issues?state=all&per_page={perPage}", ct, excludePullRequests: true),
            "pull_requests" => await GetArrayAsync($"repos/{owner}/{repo}/pulls?state=all&per_page={perPage}", ct),
            "commits" => await GetArrayAsync($"repos/{owner}/{repo}/commits?per_page={perPage}", ct),
            _ => [],
        };
    }

    public async Task<JsonElement> ExecuteDirectAsync(string entity, string action, JsonElement parameters, CancellationToken ct)
    {
        if (action != "list" && action != "get")
            throw new InvalidOperationException($"GitHub Integration supports only list/get direct actions for V1, got '{action}'.");

        var owner = ReadString(parameters, "owner");
        var repo = ReadString(parameters, "repo");
        var perPage = ReadInt(parameters, "per_page") ?? 10;

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            throw new InvalidOperationException("GitHub direct calls require params.owner and params.repo.");

        if (action == "get" || entity == "repositories")
            return JsonSerializer.SerializeToElement(await GetRepositoryAsync(owner, repo, ct));

        var path = entity switch
        {
            "issues" => $"repos/{owner}/{repo}/issues?state=all&per_page={perPage}",
            "pull_requests" => $"repos/{owner}/{repo}/pulls?state=all&per_page={perPage}",
            "commits" => $"repos/{owner}/{repo}/commits?per_page={perPage}",
            "branches" => $"repos/{owner}/{repo}/branches?per_page={perPage}",
            "comments" => $"repos/{owner}/{repo}/issues/comments?per_page={perPage}",
            _ => throw new InvalidOperationException($"Unsupported GitHub entity '{entity}'."),
        };

        var rows = await GetArrayAsync(path, ct, excludePullRequests: entity == "issues");
        return JsonSerializer.SerializeToElement(rows);
    }

    private async Task<JsonObject> GetRepositoryAsync(string owner, string repo, CancellationToken ct)
    {
        var json = await SendAsync(HttpMethod.Get, $"repos/{owner}/{repo}", ct);
        var obj = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
        obj["owner"] = obj["owner"]?.ToJsonString();
        return obj;
    }

    private async Task<IReadOnlyList<JsonObject>> GetArrayAsync(string path, CancellationToken ct, bool excludePullRequests = false)
    {
        var json = await SendAsync(HttpMethod.Get, path, ct);
        var array = JsonNode.Parse(json) as JsonArray ?? [];
        var rows = new List<JsonObject>();
        foreach (var item in array)
        {
            if (item is not JsonObject obj) continue;
            if (excludePullRequests && obj.ContainsKey("pull_request")) continue;
            rows.Add(obj);
        }
        return rows;
    }

    private async Task<string> SendAsync(HttpMethod method, string path, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(ct)
            ?? throw new InvalidOperationException("GitHub OAuth is not connected.");
        var client = _httpClientFactory.CreateClient("github-atlas");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GitHub API returned {(int)response.StatusCode}: {body}");
        return body;
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken ct)
    {
        var token = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { Provider = "github" }, ct);
        if (string.IsNullOrWhiteSpace(token?.EncryptedAccessToken)) return null;
        return _credentialProtector.Unprotect(token.EncryptedAccessToken).GetValueOrDefault("token");
    }

    private static (string Owner, string Repo) SplitRepository(string repository)
    {
        var parts = repository.Split('/', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || parts[0].Contains('*') || parts[1].Contains('*'))
            throw new InvalidOperationException($"Repository must use explicit owner/repo format: {repository}");
        return (parts[0], parts[1]);
    }

    private static string? ReadString(JsonElement obj, string name)
        => obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out var value)
            ? value.GetString()
            : null;

    private static int? ReadInt(JsonElement obj, string name)
        => obj.ValueKind == JsonValueKind.Object
           && obj.TryGetProperty(name, out var value)
           && value.TryGetInt32(out var parsed)
            ? parsed
            : null;
}

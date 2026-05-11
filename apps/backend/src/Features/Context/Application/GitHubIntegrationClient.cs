namespace OffceOs.Application.Features.Context;

public sealed class GitHubIntegrationClient
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

    public async Task<bool> HasTokenAsync(Guid? userId, CancellationToken ct)
        => !string.IsNullOrWhiteSpace(await GetAccessTokenAsync(userId, ct));

    public async Task<IReadOnlyList<GitHubRepositoryItem>> ListRepositoriesAsync(Guid? userId, CancellationToken ct)
    {
        var rows = new List<GitHubRepositoryItem>();
        for (var page = 1; page <= 5; page++)
        {
            var json = await SendAsync(
                userId,
                HttpMethod.Get,
                $"user/repos?affiliation=owner,collaborator,organization_member&sort=updated&per_page=100&page={page}",
                ct);
            var array = JsonNode.Parse(json) as JsonArray ?? [];
            foreach (var item in array)
            {
                if (item is not JsonObject obj) continue;
                var fullName = obj["full_name"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(fullName)) continue;
                rows.Add(new GitHubRepositoryItem(
                    fullName,
                    obj["name"]?.GetValue<string>() ?? fullName.Split('/').Last(),
                    obj["owner"]?["login"]?.GetValue<string>() ?? fullName.Split('/').First(),
                    obj["private"]?.GetValue<bool>() ?? false,
                    obj["html_url"]?.GetValue<string>(),
                    obj["description"]?.GetValue<string>()));
            }
            if (array.Count < 100) break;
        }

        return rows
            .DistinctBy(repo => repo.FullName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(repo => repo.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task ValidateRepositoriesAsync(Guid? userId, IReadOnlyList<string> repositories, CancellationToken ct)
    {
        foreach (var repository in repositories)
        {
            var (owner, repo) = SplitRepository(repository);
            await SendAsync(userId, HttpMethod.Get, $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repo)}", ct);
        }
    }

    public async Task<IReadOnlyList<JsonObject>> FetchEntityAsync(Guid? userId, string entity, string repository, int perPage, CancellationToken ct)
    {
        var (owner, repo) = SplitRepository(repository);
        return entity switch
        {
            "repositories" => [await GetRepositoryAsync(userId, owner, repo, ct)],
            "issues" => await GetArrayAsync(userId, $"repos/{owner}/{repo}/issues?state=all&per_page={perPage}", ct, excludePullRequests: true),
            "pull_requests" => await GetArrayAsync(userId, $"repos/{owner}/{repo}/pulls?state=all&per_page={perPage}", ct),
            "commits" => await GetArrayAsync(userId, $"repos/{owner}/{repo}/commits?per_page={perPage}", ct),
            _ => [],
        };
    }

    public async Task<JsonElement> ExecuteDirectAsync(Guid? userId, string entity, string action, JsonElement parameters, CancellationToken ct)
    {
        if (action != "list" && action != "get")
            throw new InvalidOperationException($"GitHub Integration supports only list/get direct actions for V1, got '{action}'.");

        var owner = ReadString(parameters, "owner");
        var repo = ReadString(parameters, "repo");
        var perPage = ReadInt(parameters, "per_page") ?? 10;

        if (string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
            throw new InvalidOperationException("GitHub direct calls require params.owner and params.repo.");

        if (action == "get" || entity == "repositories")
            return JsonSerializer.SerializeToElement(await GetRepositoryAsync(userId, owner, repo, ct));

        var path = entity switch
        {
            "issues" => $"repos/{owner}/{repo}/issues?state=all&per_page={perPage}",
            "pull_requests" => $"repos/{owner}/{repo}/pulls?state=all&per_page={perPage}",
            "commits" => $"repos/{owner}/{repo}/commits?per_page={perPage}",
            "branches" => $"repos/{owner}/{repo}/branches?per_page={perPage}",
            "comments" => $"repos/{owner}/{repo}/issues/comments?per_page={perPage}",
            _ => throw new InvalidOperationException($"Unsupported GitHub entity '{entity}'."),
        };

        var rows = await GetArrayAsync(userId, path, ct, excludePullRequests: entity == "issues");
        return JsonSerializer.SerializeToElement(rows);
    }

    private async Task<JsonObject> GetRepositoryAsync(Guid? userId, string owner, string repo, CancellationToken ct)
    {
        var json = await SendAsync(userId, HttpMethod.Get, $"repos/{owner}/{repo}", ct);
        var obj = JsonNode.Parse(json) as JsonObject ?? new JsonObject();
        obj["owner"] = obj["owner"]?.ToJsonString();
        return obj;
    }

    private async Task<IReadOnlyList<JsonObject>> GetArrayAsync(Guid? userId, string path, CancellationToken ct, bool excludePullRequests = false)
    {
        var json = await SendAsync(userId, HttpMethod.Get, path, ct);
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

    private async Task<string> SendAsync(Guid? userId, HttpMethod method, string path, CancellationToken ct)
    {
        var token = await GetAccessTokenAsync(userId, ct)
            ?? throw new InvalidOperationException("GitHub OAuth is not connected.");
        var client = _httpClientFactory.CreateClient("github-integration-indexing");
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"GitHub API returned {(int)response.StatusCode}: {body}");
        return body;
    }

    private async Task<string?> GetAccessTokenAsync(Guid? userId, CancellationToken ct)
    {
        if (!userId.HasValue) return null;

        var token = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { UserId = userId.Value, Provider = "github" }, ct);
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

public sealed record GitHubRepositoryItem(
    string FullName,
    string Name,
    string Owner,
    bool Private,
    string? Url,
    string? Description);

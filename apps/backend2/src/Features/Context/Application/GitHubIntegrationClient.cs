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
        var client = _httpClientFactory.CreateClient("github-api");
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

}

public sealed record GitHubRepositoryItem(
    string FullName,
    string Name,
    string Owner,
    bool Private,
    string? Url,
    string? Description);

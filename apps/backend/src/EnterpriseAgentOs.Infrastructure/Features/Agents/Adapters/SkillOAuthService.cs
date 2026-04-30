using System.Text.Json.Serialization;

namespace EnterpriseAgentOs.Infrastructure.Features.Agents;

internal sealed class SkillOAuthService : ISkillOAuthService
{
    private readonly GoogleOAuthConfig _googleConfig;
    private readonly GitHubOAuthConfig _githubConfig;
    private readonly EaosDbContext _db;
    private readonly SkillCredentialProtector _protector;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SkillOAuthService> _logger;

    public SkillOAuthService(
        GoogleOAuthConfig googleConfig,
        GitHubOAuthConfig githubConfig,
        EaosDbContext db,
        SkillCredentialProtector protector,
        IHttpClientFactory httpFactory,
        ILogger<SkillOAuthService> logger)
    {
        _googleConfig = googleConfig;
        _githubConfig = githubConfig;
        _db = db;
        _protector = protector;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<SkillOAuthStartResult> PrepareStartAsync(string provider, string scopes, CancellationToken ct = default)
    {
        var requestedScopes = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var existing = await _db.OAuthTokens.Include(t => t.GrantedScopes).FirstOrDefaultAsync(t => t.Provider == provider, ct);
        HashSet<string> mergedScopeSet;
        if (existing is not null)
        {
            mergedScopeSet = existing.GrantedScopes.Select(s => s.Scope).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var s in requestedScopes) mergedScopeSet.Add(s);
        }
        else
        {
            mergedScopeSet = requestedScopes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        var mergedScopes = string.Join(' ', mergedScopeSet);

        var state = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        var url = provider switch
        {
            "google" => "https://accounts.google.com/o/oauth2/v2/auth"
                + $"?client_id={Uri.EscapeDataString(_googleConfig.ClientId)}"
                + $"&redirect_uri={Uri.EscapeDataString(_googleConfig.RedirectUri)}"
                + "&response_type=code"
                + "&access_type=offline"
                + "&prompt=consent"
                + $"&scope={Uri.EscapeDataString(mergedScopes)}"
                + $"&state={Uri.EscapeDataString(state)}",

            "github" => "https://github.com/login/oauth/authorize"
                + $"?client_id={Uri.EscapeDataString(_githubConfig.ClientId)}"
                + $"&redirect_uri={Uri.EscapeDataString(_githubConfig.RedirectUri)}"
                + $"&scope={Uri.EscapeDataString(mergedScopes)}"
                + $"&state={Uri.EscapeDataString(state)}",

            _ => throw new InvalidOperationException($"Unsupported OAuth provider: {provider}"),
        };

        return new SkillOAuthStartResult(url, state, mergedScopes);
    }

    public async Task<string?> ExchangeCallbackAsync(string provider, string code, CancellationToken ct = default)
    {
        var http = _httpFactory.CreateClient();

        var (accessToken, refreshToken, expiresIn, grantedScopes) = provider switch
        {
            "google" => await ExchangeGoogleCodeAsync(http, code, ct),
            "github" => await ExchangeGitHubCodeAsync(http, code, ct),
            _ => throw new InvalidOperationException($"Unsupported OAuth provider: {provider}"),
        };

        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("No access token in response.");

        string? email = null;
        try
        {
            email = provider switch
            {
                "google" => await FetchGoogleEmailAsync(http, accessToken, ct),
                "github" => await FetchGitHubEmailAsync(http, accessToken, ct),
                _ => null,
            };
        }
        catch { /* non-critical */ }

        var existing = await _db.OAuthTokens.Include(t => t.GrantedScopes).FirstOrDefaultAsync(t => t.Provider == provider, ct);
        if (existing is null)
        {
            existing = new OAuthTokenEntity { Id = Guid.NewGuid(), Provider = provider, CreatedAt = DateTime.UtcNow };
            _db.OAuthTokens.Add(existing);
        }

        existing.EncryptedAccessToken = _protector.Protect(accessToken);
        existing.EncryptedRefreshToken = !string.IsNullOrEmpty(refreshToken)
            ? _protector.Protect(refreshToken)
            : existing.EncryptedRefreshToken;
        existing.ExpiresAtUtc = expiresIn.HasValue
            ? DateTime.UtcNow.AddSeconds(expiresIn.Value > 0 ? expiresIn.Value : 3600)
            : null; // GitHub tokens don't expire
        existing.Email = email;
        existing.UpdatedAt = DateTime.UtcNow;

        if (existing.GrantedScopes.Count > 0)
        {
            _db.OAuthGrantedScopes.RemoveRange(existing.GrantedScopes);
            await _db.SaveChangesAsync(ct);
        }

        existing.GrantedScopes.Clear();
        foreach (var scope in grantedScopes)
        {
            existing.GrantedScopes.Add(new OAuthGrantedScopeEntity
            {
                Id = Guid.NewGuid(),
                OAuthTokenId = existing.Id,
                Scope = scope,
            });
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("OAuth tokens stored for provider {Provider}, email {Email}", provider, email);

        return email;
    }

    private async Task<(string? AccessToken, string? RefreshToken, int? ExpiresIn, string[] Scopes)> ExchangeGoogleCodeAsync(
        HttpClient http, string code, CancellationToken ct)
    {
        var res = await http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _googleConfig.ClientId,
                ["client_secret"] = _googleConfig.ClientSecret,
                ["redirect_uri"] = _googleConfig.RedirectUri,
                ["grant_type"] = "authorization_code",
            }), ct);

        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError("Google token exchange failed: {Error}", err);
            throw new InvalidOperationException("Token exchange failed.");
        }

        var data = await res.Content.ReadFromJsonAsync<OAuthTokenResponse>(ct);
        var scopes = (data?.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        return (data?.AccessToken, data?.RefreshToken, data?.ExpiresIn, scopes);
    }

    private async Task<(string? AccessToken, string? RefreshToken, int? ExpiresIn, string[] Scopes)> ExchangeGitHubCodeAsync(
        HttpClient http, string code, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _githubConfig.ClientId,
                ["client_secret"] = _githubConfig.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = _githubConfig.RedirectUri,
            }),
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var res = await http.SendAsync(request, ct);
        if (!res.IsSuccessStatusCode)
        {
            var err = await res.Content.ReadAsStringAsync(ct);
            _logger.LogError("GitHub token exchange failed: {Error}", err);
            throw new InvalidOperationException("Token exchange failed.");
        }

        var data = await res.Content.ReadFromJsonAsync<OAuthTokenResponse>(ct);
        if (!string.IsNullOrEmpty(data?.Error))
        {
            _logger.LogError("GitHub token exchange error: {Error} - {Description}", data.Error, data.ErrorDescription);
            throw new InvalidOperationException($"GitHub token exchange failed: {data.ErrorDescription}");
        }

        // GitHub returns scopes as comma-separated
        var scopes = (data?.Scope ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        // GitHub tokens don't expire (no ExpiresIn)
        return (data?.AccessToken, data?.RefreshToken, null, scopes);
    }

    private async Task<string?> FetchGoogleEmailAsync(HttpClient http, string accessToken, CancellationToken ct)
    {
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userInfo = await http.GetFromJsonAsync<UserInfoResponse>("https://www.googleapis.com/oauth2/v2/userinfo", ct);
        return userInfo?.Email;
    }

    private async Task<string?> FetchGitHubEmailAsync(HttpClient http, string accessToken, CancellationToken ct)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.UserAgent.ParseAdd("eaos-backend/1.0");
        var res = await http.SendAsync(request, ct);
        if (!res.IsSuccessStatusCode) return null;
        var user = await res.Content.ReadFromJsonAsync<UserInfoResponse>(ct);
        return user?.Email;
    }

    public async Task<SkillOAuthStatusResult> GetStatusAsync(string provider, CancellationToken ct = default)
    {
        var token = await _db.OAuthTokens.Include(t => t.GrantedScopes).FirstOrDefaultAsync(t => t.Provider == provider, ct);
        if (token is null)
            return new SkillOAuthStatusResult(false, null, null, null);

        return new SkillOAuthStatusResult(
            true,
            token.Email,
            string.Join(' ', token.GrantedScopes.Select(s => s.Scope)),
            token.ExpiresAtUtc);
    }

    public async Task DisconnectAsync(string provider, CancellationToken ct = default)
    {
        var token = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.Provider == provider, ct);
        if (token is not null)
        {
            _db.OAuthTokens.Remove(token);
            await _db.SaveChangesAsync(ct);
        }
    }

    private sealed record OAuthTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("token_type")] string? TokenType,
        [property: JsonPropertyName("error")] string? Error,
        [property: JsonPropertyName("error_description")] string? ErrorDescription);

    private sealed record UserInfoResponse(
        [property: JsonPropertyName("email")] string? Email);
}

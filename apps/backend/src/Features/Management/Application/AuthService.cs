namespace OffceOs.Application.Features.Management;

internal sealed class AuthService : IAuthService
{
    private readonly GoogleOAuthConfig _googleOAuthConfig;
    private readonly GitHubOAuthConfig _gitHubOAuthConfig;
    private readonly IUserRepository _userRepository;
    private readonly IWorkspaceService _workspaceService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _distributedCache;

    public AuthService(
        GoogleOAuthConfig googleOAuth,
        GitHubOAuthConfig gitHubOAuth,
        IUserRepository users,
        IWorkspaceService workspaceService,
        ISessionRepository sessions,
        IHttpClientFactory httpFactory,
        IDistributedCache cache)
    {
        _googleOAuthConfig = googleOAuth;
        _gitHubOAuthConfig = gitHubOAuth;
        _userRepository = users;
        _workspaceService = workspaceService;
        _sessionRepository = sessions;
        _httpClientFactory = httpFactory;
        _distributedCache = cache;
    }

    public GoogleLoginResult BuildGoogleLoginUrl(string? redirectUri = null)
    {
        if (string.IsNullOrEmpty(_googleOAuthConfig.ClientId) || string.IsNullOrEmpty(_googleOAuthConfig.ClientSecret))
            throw new InvalidOperationException("Google OAuth is not configured on the server.");

        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var callbackUri = ResolveRedirectUri(redirectUri, _googleOAuthConfig.RedirectUri, "Google");

        var url = "https://accounts.google.com/o/oauth2/v2/auth"
            + $"?client_id={Uri.EscapeDataString(_googleOAuthConfig.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(callbackUri)}"
            + "&response_type=code"
            + $"&scope={Uri.EscapeDataString(string.Join(' ', GoogleScopes))}"
            + "&access_type=offline"
            + "&prompt=consent"
            + "&include_granted_scopes=true"
            + $"&state={Uri.EscapeDataString(state)}";

        return new GoogleLoginResult(url, state);
    }

    public async Task<GoogleCallbackResult> HandleGoogleCallbackAsync(string code, string? redirectUri = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_googleOAuthConfig.ClientId) || string.IsNullOrEmpty(_googleOAuthConfig.ClientSecret))
            throw new InvalidOperationException("Google OAuth is not configured on the server.");

        var callbackUri = ResolveRedirectUri(redirectUri, _googleOAuthConfig.RedirectUri, "Google");
        var client = _httpClientFactory.CreateClient();
        var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _googleOAuthConfig.ClientId,
                ["client_secret"] = _googleOAuthConfig.ClientSecret,
                ["redirect_uri"] = callbackUri,
                ["grant_type"] = "authorization_code",
            }), ct);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var body = await tokenResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to exchange authorization code: {body}");
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (!tokenJson.TryGetProperty("access_token", out var atProp))
            throw new InvalidOperationException("Google did not return an access token.");

        var accessToken = atProp.GetString()!;
        var refreshToken = tokenJson.TryGetProperty("refresh_token", out var rtProp)
            ? rtProp.GetString()
            : null;
        var expiresAt = tokenJson.TryGetProperty("expires_in", out var expProp)
            ? DateTime.UtcNow.AddSeconds(expProp.GetInt32())
            : (DateTime?)null;
        var scopes = tokenJson.TryGetProperty("scope", out var scopeProp)
            ? SplitScopes(scopeProp.GetString())
            : GoogleScopes;

        var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userInfoResponse = await client.SendAsync(userInfoRequest, ct);
        if (!userInfoResponse.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to fetch your Google profile.");

        var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var sub = userInfo.GetProperty("sub").GetString()!;
        var email = userInfo.GetProperty("email").GetString()!;
        var name = userInfo.TryGetProperty("name", out var n) ? n.GetString() : null;
        var avatar = userInfo.TryGetProperty("picture", out var p) ? p.GetString() : null;

        var user = await _userRepository.UpsertByGoogleSubjectAsync(sub, email, name, avatar, ct);
        await _workspaceService.GetCurrentAsync(user.Id, ct);

        var sessionToken = await CreateSessionTokenAsync(user.Id, TimeSpan.FromDays(7), ct);
        var integrationCredentials = new Dictionary<string, string>
        {
            ["GOOGLE_CLIENT_ID"] = _googleOAuthConfig.ClientId,
            ["GOOGLE_CLIENT_SECRET"] = _googleOAuthConfig.ClientSecret,
            ["GOOGLE_TOKEN_SCOPE"] = string.Join(' ', scopes),
            ["GOOGLE_ACCESS_TOKEN"] = accessToken,
        };
        if (!string.IsNullOrWhiteSpace(refreshToken))
            integrationCredentials["GOOGLE_REFRESH_TOKEN"] = refreshToken;

        return new GoogleCallbackResult(sessionToken, user.Id, email, integrationCredentials, scopes, expiresAt);
    }

    public GitHubLoginResult BuildGitHubLoginUrl(string? redirectUri = null)
    {
        if (string.IsNullOrEmpty(_gitHubOAuthConfig.ClientId) || string.IsNullOrEmpty(_gitHubOAuthConfig.ClientSecret))
            throw new InvalidOperationException("GitHub OAuth is not configured on the server.");

        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var callbackUri = ResolveRedirectUri(redirectUri, _gitHubOAuthConfig.RedirectUri, "GitHub");
        var url = "https://github.com/login/oauth/authorize"
            + $"?client_id={Uri.EscapeDataString(_gitHubOAuthConfig.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(callbackUri)}"
            + $"&scope={Uri.EscapeDataString(string.Join(' ', GitHubScopes))}"
            + $"&state={Uri.EscapeDataString(state)}";

        return new GitHubLoginResult(url, state);
    }

    public async Task<GitHubCallbackResult> HandleGitHubCallbackAsync(string code, string? redirectUri = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_gitHubOAuthConfig.ClientId) || string.IsNullOrEmpty(_gitHubOAuthConfig.ClientSecret))
            throw new InvalidOperationException("GitHub OAuth is not configured on the server.");

        var callbackUri = ResolveRedirectUri(redirectUri, _gitHubOAuthConfig.RedirectUri, "GitHub");
        var client = _httpClientFactory.CreateClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _gitHubOAuthConfig.ClientId,
                ["client_secret"] = _gitHubOAuthConfig.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = callbackUri,
            }),
        };
        tokenRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var tokenResponse = await client.SendAsync(tokenRequest, ct);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var body = await tokenResponse.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to exchange GitHub authorization code: {body}");
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (!tokenJson.TryGetProperty("access_token", out var atProp))
            throw new InvalidOperationException("GitHub did not return an access token.");

        var accessToken = atProp.GetString()!;
        var scopes = tokenJson.TryGetProperty("scope", out var scopeProp)
            ? SplitScopes(scopeProp.GetString())
            : GitHubScopes;

        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        userRequest.Headers.UserAgent.ParseAdd("OffceOs");
        var userResponse = await client.SendAsync(userRequest, ct);
        if (!userResponse.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to fetch your GitHub profile.");

        var userInfo = await userResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var sub = userInfo.GetProperty("id").GetInt64().ToString();
        var name = userInfo.TryGetProperty("name", out var n) && n.ValueKind != JsonValueKind.Null ? n.GetString() : null;
        var avatar = userInfo.TryGetProperty("avatar_url", out var a) ? a.GetString() : null;
        var email = userInfo.TryGetProperty("email", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetString() : null;

        if (string.IsNullOrEmpty(email))
            email = await ResolveGitHubPrimaryEmailAsync(client, accessToken, ct);
        if (string.IsNullOrEmpty(email))
            throw new InvalidOperationException("Could not retrieve a verified email from your GitHub account.");

        var user = await _userRepository.UpsertByGitHubSubjectAsync(sub, email, name, avatar, ct);
        await _workspaceService.GetCurrentAsync(user.Id, ct);

        var sessionToken = await CreateSessionTokenAsync(user.Id, TimeSpan.FromDays(7), ct);
        var integrationCredentials = new Dictionary<string, string>
        {
            ["GITHUB_PERSONAL_ACCESS_TOKEN"] = accessToken,
        };

        return new GitHubCallbackResult(sessionToken, user.Id, email, integrationCredentials, scopes, null);
    }

    public async Task<UserRecord> UpdateProfileAsync(
        Guid userId,
        string? name,
        string? displayName,
        string? timezone,
        string? notificationPrefsJson,
        string? preferences,
        CancellationToken ct = default)
    {
        var updated = await _userRepository.UpdateProfileAsync(
            userId,
            name,
            displayName,
            timezone,
            notificationPrefsJson,
            preferences,
            ct);
        await _distributedCache.RemoveAsync($"auth:me:{userId}", ct);
        return updated;
    }

    public async Task<string> CreateSessionTokenAsync(Guid userId, TimeSpan lifetime, CancellationToken ct = default)
    {
        var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = SessionTokenHasher.Hash(sessionToken);
        await _sessionRepository.CreateAsync(userId, tokenHash, DateTime.UtcNow.Add(lifetime), ct);
        return sessionToken;
    }

    public async Task<bool> LogoutAsync(string? sessionToken, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(sessionToken))
            return false;

        var tokenHash = SessionTokenHasher.Hash(sessionToken);
        await _sessionRepository.DeleteAsync(tokenHash, ct);
        await _distributedCache.RemoveAsync($"session:{tokenHash[..16]}", ct);
        return true;
    }

    private static async Task<string?> ResolveGitHubPrimaryEmailAsync(HttpClient client, string accessToken, CancellationToken ct)
    {
        var emailsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
        emailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        emailsRequest.Headers.UserAgent.ParseAdd("OffceOs");
        var emailsResponse = await client.SendAsync(emailsRequest, ct);
        if (!emailsResponse.IsSuccessStatusCode)
            return null;

        var emails = await emailsResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        foreach (var entry in emails.EnumerateArray())
        {
            if (entry.TryGetProperty("primary", out var primary) && primary.GetBoolean()
                && entry.TryGetProperty("verified", out var verified) && verified.GetBoolean())
            {
                return entry.GetProperty("email").GetString();
            }
        }

        return null;
    }

    private static readonly string[] GoogleScopes =
    [
        "openid",
        "email",
        "profile",
        "https://www.googleapis.com/auth/gmail.modify",
        "https://www.googleapis.com/auth/drive",
        "https://www.googleapis.com/auth/documents",
        "https://www.googleapis.com/auth/calendar",
    ];

    private static readonly string[] GitHubScopes =
    [
        "user:email",
        "repo",
        "read:org",
    ];

    private static string ResolveRedirectUri(string? requestedRedirectUri, string configuredRedirectUri, string provider)
    {
        if (!string.IsNullOrWhiteSpace(requestedRedirectUri))
            return requestedRedirectUri;
        if (!string.IsNullOrWhiteSpace(configuredRedirectUri))
            return configuredRedirectUri;

        throw new InvalidOperationException($"{provider} OAuth redirect URI is not configured on the server.");
    }

    private static IReadOnlyList<string> SplitScopes(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

namespace OffceOs.Application.Features.Management;

internal sealed class AuthService : IAuthService
{
    private readonly GoogleOAuthConfig _googleOAuthConfig;
    private readonly GitHubOAuthConfig _gitHubOAuthConfig;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationService _organizationService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IOAuthTokenRepository _oauthTokenRepository;
    private readonly CredentialProtector _credentialProtector;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        GoogleOAuthConfig googleOAuth,
        GitHubOAuthConfig gitHubOAuth,
        IUserRepository users,
        IOrganizationService organizationService,
        ISessionRepository sessions,
        IOAuthTokenRepository oauthTokens,
        CredentialProtector credentialProtector,
        IHttpClientFactory httpFactory,
        IDistributedCache cache,
        ILogger<AuthService> logger)
    {
        _googleOAuthConfig = googleOAuth;
        _gitHubOAuthConfig = gitHubOAuth;
        _userRepository = users;
        _organizationService = organizationService;
        _sessionRepository = sessions;
        _oauthTokenRepository = oauthTokens;
        _credentialProtector = credentialProtector;
        _httpClientFactory = httpFactory;
        _distributedCache = cache;
        _logger = logger;
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

        // Exchange code for tokens
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

        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(ct);
        var tokenJson = JsonSerializer.Deserialize<JsonElement>(tokenBody);
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

        // Fetch user info
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

        // Upsert user
        var user = await _userRepository.UpsertByGoogleSubjectAsync(sub, email, name, avatar, ct);
        await _organizationService.EnsureOrganizationAsync(user.Id, user.Email, user.Name, ct);
        _logger.LogInformation("OAuth: user upserted {Email} ({UserId})", email, user.Id);

        var existingGoogleToken = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { UserId = user.Id, Provider = "google" }, ct);
        var googleToken = new OAuthTokenRecord
        {
            Id = existingGoogleToken?.Id ?? Guid.NewGuid(),
            UserId = user.Id,
            Provider = "google",
            EncryptedAccessToken = ProtectToken(accessToken),
            EncryptedRefreshToken = refreshToken is not null
                ? ProtectToken(refreshToken)
                : existingGoogleToken?.EncryptedRefreshToken,
            ExpiresAtUtc = expiresAt,
            Email = email,
            CreatedAt = existingGoogleToken?.CreatedAt ?? DateTime.UtcNow,
        };
        googleToken.ReplaceScopes(scopes);
        await _oauthTokenRepository.UpsertAsync(googleToken, ct);

        var sessionToken = await CreateSessionTokenAsync(user.Id, TimeSpan.FromDays(7), ct);
        _logger.LogInformation("OAuth: session created for {Email}", email);

        var integrationCredentials = new Dictionary<string, string>
        {
            ["GOOGLE_CLIENT_ID"] = _googleOAuthConfig.ClientId,
            ["GOOGLE_CLIENT_SECRET"] = _googleOAuthConfig.ClientSecret,
            ["GOOGLE_TOKEN_SCOPE"] = string.Join(' ', scopes),
        };
        if (!string.IsNullOrWhiteSpace(refreshToken))
            integrationCredentials["GOOGLE_REFRESH_TOKEN"] = refreshToken;
        integrationCredentials["GOOGLE_ACCESS_TOKEN"] = accessToken;

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

        // Exchange code for access token
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

        // Fetch user profile
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

        // GitHub may not include email in /user — fetch from /user/emails
        var email = userInfo.TryGetProperty("email", out var e) && e.ValueKind != JsonValueKind.Null
            ? e.GetString()
            : null;

        if (string.IsNullOrEmpty(email))
        {
            var emailsRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user/emails");
            emailsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            emailsRequest.Headers.UserAgent.ParseAdd("OffceOs");
            var emailsResponse = await client.SendAsync(emailsRequest, ct);
            if (emailsResponse.IsSuccessStatusCode)
            {
                var emails = await emailsResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
                foreach (var entry in emails.EnumerateArray())
                {
                    if (entry.TryGetProperty("primary", out var primary) && primary.GetBoolean()
                        && entry.TryGetProperty("verified", out var verified) && verified.GetBoolean())
                    {
                        email = entry.GetProperty("email").GetString();
                        break;
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(email))
            throw new InvalidOperationException("Could not retrieve a verified email from your GitHub account.");

        // Upsert user
        var user = await _userRepository.UpsertByGitHubSubjectAsync(sub, email, name, avatar, ct);
        await _organizationService.EnsureOrganizationAsync(user.Id, user.Email, user.Name, ct);
        _logger.LogInformation("OAuth: GitHub user upserted {Email} ({UserId})", email, user.Id);

        var existingGitHubToken = await _oauthTokenRepository.GetByAsync(new OAuthTokenFilter { UserId = user.Id, Provider = "github" }, ct);
        var gitHubToken = new OAuthTokenRecord
        {
            Id = existingGitHubToken?.Id ?? Guid.NewGuid(),
            UserId = user.Id,
            Provider = "github",
            EncryptedAccessToken = ProtectToken(accessToken),
            EncryptedRefreshToken = existingGitHubToken?.EncryptedRefreshToken,
            Email = email,
            CreatedAt = existingGitHubToken?.CreatedAt ?? DateTime.UtcNow,
        };
        gitHubToken.ReplaceScopes(scopes);
        await _oauthTokenRepository.UpsertAsync(gitHubToken, ct);

        var sessionToken = await CreateSessionTokenAsync(user.Id, TimeSpan.FromDays(7), ct);
        _logger.LogInformation("OAuth: session created for {Email}", email);

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

    private string ProtectToken(string token) =>
        _credentialProtector.Protect(new Dictionary<string, string> { ["token"] = token });

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

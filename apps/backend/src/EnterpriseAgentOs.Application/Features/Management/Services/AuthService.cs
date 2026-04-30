using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace EnterpriseAgentOs.Application.Features.Management;

internal sealed class AuthService : IAuthService
{
    private readonly GoogleOAuthConfig _googleOAuthConfig;
    private readonly GitHubOAuthConfig _gitHubOAuthConfig;
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        GoogleOAuthConfig googleOAuth,
        GitHubOAuthConfig gitHubOAuth,
        IUserRepository users,
        ISessionRepository sessions,
        IHttpClientFactory httpFactory,
        ILogger<AuthService> logger)
    {
        _googleOAuthConfig = googleOAuth;
        _gitHubOAuthConfig = gitHubOAuth;
        _userRepository = users;
        _sessionRepository = sessions;
        _httpClientFactory = httpFactory;
        _logger = logger;
    }

    public GoogleLoginResult BuildGoogleLoginUrl()
    {
        if (string.IsNullOrEmpty(_googleOAuthConfig.ClientId) || string.IsNullOrEmpty(_googleOAuthConfig.ClientSecret))
            throw new InvalidOperationException("Google OAuth is not configured on the server.");

        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var url = "https://accounts.google.com/o/oauth2/v2/auth"
            + $"?client_id={Uri.EscapeDataString(_googleOAuthConfig.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_googleOAuthConfig.RedirectUri)}"
            + "&response_type=code"
            + "&scope=openid%20email%20profile"
            + $"&state={Uri.EscapeDataString(state)}";

        return new GoogleLoginResult(url, state);
    }

    public async Task<GoogleCallbackResult> HandleGoogleCallbackAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_googleOAuthConfig.ClientId) || string.IsNullOrEmpty(_googleOAuthConfig.ClientSecret))
            throw new InvalidOperationException("Google OAuth is not configured on the server.");

        // Exchange code for tokens
        var client = _httpClientFactory.CreateClient();
        var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _googleOAuthConfig.ClientId,
                ["client_secret"] = _googleOAuthConfig.ClientSecret,
                ["redirect_uri"] = _googleOAuthConfig.RedirectUri,
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
        _logger.LogInformation("OAuth: user upserted {Email} ({UserId})", email, user.Id);

        // Create session
        var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = SessionTokenHasher.Hash(sessionToken);
        await _sessionRepository.CreateAsync(user.Id, tokenHash, DateTime.UtcNow.AddDays(7), ct);
        _logger.LogInformation("OAuth: session created for {Email}", email);

        return new GoogleCallbackResult(sessionToken, email);
    }

    public GitHubLoginResult BuildGitHubLoginUrl()
    {
        if (string.IsNullOrEmpty(_gitHubOAuthConfig.ClientId) || string.IsNullOrEmpty(_gitHubOAuthConfig.ClientSecret))
            throw new InvalidOperationException("GitHub OAuth is not configured on the server.");

        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var url = "https://github.com/login/oauth/authorize"
            + $"?client_id={Uri.EscapeDataString(_gitHubOAuthConfig.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_gitHubOAuthConfig.RedirectUri)}"
            + "&scope=user%3Aemail"
            + $"&state={Uri.EscapeDataString(state)}";

        return new GitHubLoginResult(url, state);
    }

    public async Task<GitHubCallbackResult> HandleGitHubCallbackAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_gitHubOAuthConfig.ClientId) || string.IsNullOrEmpty(_gitHubOAuthConfig.ClientSecret))
            throw new InvalidOperationException("GitHub OAuth is not configured on the server.");

        // Exchange code for access token
        var client = _httpClientFactory.CreateClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = _gitHubOAuthConfig.ClientId,
                ["client_secret"] = _gitHubOAuthConfig.ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = _gitHubOAuthConfig.RedirectUri,
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

        // Fetch user profile
        var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/user");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        userRequest.Headers.UserAgent.ParseAdd("EnterpriseAgentOs");
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
            emailsRequest.Headers.UserAgent.ParseAdd("EnterpriseAgentOs");
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
        _logger.LogInformation("OAuth: GitHub user upserted {Email} ({UserId})", email, user.Id);

        // Create session
        var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var tokenHash = SessionTokenHasher.Hash(sessionToken);
        await _sessionRepository.CreateAsync(user.Id, tokenHash, DateTime.UtcNow.AddDays(7), ct);
        _logger.LogInformation("OAuth: session created for {Email}", email);

        return new GitHubCallbackResult(sessionToken, email);
    }
}

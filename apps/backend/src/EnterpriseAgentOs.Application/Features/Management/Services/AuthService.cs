using System.Net.Http.Headers;
using System.Security.Cryptography;

namespace EnterpriseAgentOs.Application.Features.Management;

internal sealed class AuthService : IAuthService
{
    private readonly GoogleOAuthConfig _googleOAuthConfig;
    private readonly IUserRepository _userRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        GoogleOAuthConfig oauth,
        IUserRepository users,
        ISessionRepository sessions,
        IHttpClientFactory httpFactory,
        ILogger<AuthService> logger)
    {
        _googleOAuthConfig = oauth;
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
}

using System.Text.Json.Serialization;

namespace EnterpriseAgentOs.Infrastructure.Features.Skills;

internal sealed class SkillOAuthService : ISkillOAuthService
{
    private readonly GoogleOAuthConfig _googleConfig;
    private readonly EaosDbContext _db;
    private readonly SkillCredentialProtector _protector;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SkillOAuthService> _logger;

    public SkillOAuthService(
        GoogleOAuthConfig googleConfig,
        EaosDbContext db,
        SkillCredentialProtector protector,
        IHttpClientFactory httpFactory,
        ILogger<SkillOAuthService> logger)
    {
        _googleConfig = googleConfig;
        _db = db;
        _protector = protector;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public async Task<SkillOAuthStartResult> PrepareStartAsync(string provider, string scopes, CancellationToken ct = default)
    {
        if (provider != "google")
            throw new InvalidOperationException($"Unsupported OAuth provider: {provider}");

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

        var url = "https://accounts.google.com/o/oauth2/v2/auth"
            + $"?client_id={Uri.EscapeDataString(_googleConfig.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_googleConfig.SkillOAuthRedirectUri)}"
            + "&response_type=code"
            + "&access_type=offline"
            + "&prompt=consent"
            + $"&scope={Uri.EscapeDataString(mergedScopes)}"
            + $"&state={Uri.EscapeDataString(state)}";

        return new SkillOAuthStartResult(url, state, mergedScopes);
    }

    public async Task<string?> ExchangeCallbackAsync(string provider, string code, CancellationToken ct = default)
    {
        if (provider != "google")
            throw new InvalidOperationException($"Unsupported OAuth provider: {provider}");

        var http = _httpFactory.CreateClient();
        var tokenRes = await http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _googleConfig.ClientId,
                ["client_secret"] = _googleConfig.ClientSecret,
                ["redirect_uri"] = _googleConfig.SkillOAuthRedirectUri,
                ["grant_type"] = "authorization_code",
            }), ct);

        if (!tokenRes.IsSuccessStatusCode)
        {
            var err = await tokenRes.Content.ReadAsStringAsync(ct);
            _logger.LogError("Google token exchange failed: {Error}", err);
            throw new InvalidOperationException("Token exchange failed.");
        }

        var tokenData = await tokenRes.Content.ReadFromJsonAsync<GoogleTokenResponse>(ct);
        if (tokenData is null || string.IsNullOrEmpty(tokenData.AccessToken))
            throw new InvalidOperationException("No access token in response.");

        string? email = null;
        try
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
            var userInfoRes = await http.GetFromJsonAsync<GoogleUserInfo>("https://www.googleapis.com/oauth2/v2/userinfo", ct);
            email = userInfoRes?.Email;
        }
        catch { /* non-critical */ }

        var existing = await _db.OAuthTokens.Include(t => t.GrantedScopes).FirstOrDefaultAsync(t => t.Provider == provider, ct);
        if (existing is null)
        {
            existing = new OAuthTokenEntity { Id = Guid.NewGuid(), Provider = provider, CreatedAt = DateTime.UtcNow };
            _db.OAuthTokens.Add(existing);
        }

        existing.EncryptedAccessToken = _protector.Protect(tokenData.AccessToken);
        existing.EncryptedRefreshToken = !string.IsNullOrEmpty(tokenData.RefreshToken)
            ? _protector.Protect(tokenData.RefreshToken)
            : existing.EncryptedRefreshToken;
        existing.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn > 0 ? tokenData.ExpiresIn : 3600);
        existing.Email = email;
        existing.UpdatedAt = DateTime.UtcNow;

        if (existing.GrantedScopes.Count > 0)
        {
            _db.OAuthGrantedScopes.RemoveRange(existing.GrantedScopes);
            await _db.SaveChangesAsync(ct);
        }

        existing.GrantedScopes.Clear();
        foreach (var scope in (tokenData.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase))
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

    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn,
        [property: JsonPropertyName("scope")] string? Scope,
        [property: JsonPropertyName("token_type")] string? TokenType);

    private sealed record GoogleUserInfo(
        [property: JsonPropertyName("email")] string? Email);
}

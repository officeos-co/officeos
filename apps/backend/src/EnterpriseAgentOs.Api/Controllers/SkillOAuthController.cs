namespace EnterpriseAgentOs.Api.Controllers;

/// <summary>
/// Handles OAuth2 flows for skill credentials.
/// Users initiate from the dashboard → redirect to provider → callback stores tokens.
/// Multiple skills sharing the same provider reuse one token (scopes are merged).
/// </summary>
[ApiController]
[Route("api/skills/oauth")]
public sealed class SkillOAuthController : ControllerBase
{
    private readonly GoogleOAuthConfig _googleConfig;
    private readonly EaosDbContext _db;
    private readonly SkillCredentialProtector _protector;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SkillOAuthController> _logger;

    public SkillOAuthController(
        GoogleOAuthConfig googleConfig,
        EaosDbContext db,
        SkillCredentialProtector protector,
        IHttpClientFactory httpFactory,
        ILogger<SkillOAuthController> logger)
    {
        _googleConfig = googleConfig;
        _db = db;
        _protector = protector;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    /// <summary>
    /// Initiates OAuth2 flow for a skill credential.
    /// Dashboard calls this to get the redirect URL.
    /// </summary>
    [HttpGet("{provider}/start")]
    public async Task<IActionResult> Start(string provider, [FromQuery] string scopes, [FromQuery] string? returnUrl)
    {
        if (provider != "google")
            return BadRequest($"Unsupported OAuth provider: {provider}");

        // Merge requested scopes with already-granted scopes so the new token covers everything
        var requestedScopes = scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var existing = await _db.OAuthTokens.Include(t => t.GrantedScopes).FirstOrDefaultAsync(t => t.Provider == provider);
        var mergedScopes = existing is not null
            ? string.Join(' ', existing.MergedScopesWith(requestedScopes))
            : scopes;

        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append("skill-oauth-state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = !IsLocalhost,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
        });

        if (!string.IsNullOrEmpty(returnUrl))
        {
            Response.Cookies.Append("skill-oauth-return", returnUrl, new CookieOptions
            {
                HttpOnly = true,
                Secure = !IsLocalhost,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10),
            });
        }

        var url = "https://accounts.google.com/o/oauth2/v2/auth"
            + $"?client_id={Uri.EscapeDataString(_googleConfig.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_googleConfig.SkillOAuthRedirectUri)}"
            + "&response_type=code"
            + "&access_type=offline"
            + "&prompt=consent"
            + $"&scope={Uri.EscapeDataString(mergedScopes)}"
            + $"&state={Uri.EscapeDataString(state)}";

        return Redirect(url);
    }

    /// <summary>
    /// OAuth2 callback — exchanges code for tokens and stores them.
    /// </summary>
    [HttpGet("{provider}/callback")]
    public async Task<IActionResult> Callback(string provider, [FromQuery] string code, [FromQuery] string state)
    {
        if (provider != "google")
            return BadRequest($"Unsupported OAuth provider: {provider}");

        // Validate state
        var storedState = Request.Cookies["skill-oauth-state"];
        if (string.IsNullOrEmpty(storedState) || storedState != state)
            return BadRequest("Invalid OAuth state.");

        Response.Cookies.Delete("skill-oauth-state");

        // Exchange code for tokens
        var http = _httpFactory.CreateClient();
        var tokenRes = await http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _googleConfig.ClientId,
                ["client_secret"] = _googleConfig.ClientSecret,
                ["redirect_uri"] = _googleConfig.SkillOAuthRedirectUri,
                ["grant_type"] = "authorization_code",
            }));

        if (!tokenRes.IsSuccessStatusCode)
        {
            var err = await tokenRes.Content.ReadAsStringAsync();
            _logger.LogError("Google token exchange failed: {Error}", err);
            return BadRequest("Token exchange failed.");
        }

        var tokenData = await tokenRes.Content.ReadFromJsonAsync<GoogleTokenResponse>();
        if (tokenData is null || string.IsNullOrEmpty(tokenData.AccessToken))
            return BadRequest("No access token in response.");

        // Get user email for display
        string? email = null;
        try
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenData.AccessToken);
            var userInfoRes = await http.GetFromJsonAsync<GoogleUserInfo>("https://www.googleapis.com/oauth2/v2/userinfo");
            email = userInfoRes?.Email;
        }
        catch { /* non-critical */ }

        // Store tokens — upsert by provider
        var existing = await _db.OAuthTokens.Include(t => t.GrantedScopes).FirstOrDefaultAsync(t => t.Provider == provider);
        if (existing is null)
        {
            existing = new OAuthTokenRecord { Provider = provider };
            _db.OAuthTokens.Add(existing);
        }

        existing.EncryptedAccessToken = _protector.Protect(tokenData.AccessToken);
        existing.EncryptedRefreshToken = !string.IsNullOrEmpty(tokenData.RefreshToken)
            ? _protector.Protect(tokenData.RefreshToken)
            : existing.EncryptedRefreshToken; // keep old refresh token if not returned
        existing.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(tokenData.ExpiresIn > 0 ? tokenData.ExpiresIn : 3600);
        existing.Email = email;
        existing.UpdatedAt = DateTime.UtcNow;

        // Remove old scopes directly via DbContext to avoid concurrency issues with tracked entities
        if (existing.GrantedScopes.Count > 0)
        {
            _db.OAuthGrantedScopes.RemoveRange(existing.GrantedScopes);
            await _db.SaveChangesAsync();
        }

        // Add new scopes
        existing.GrantedScopes.Clear();
        foreach (var scope in (tokenData.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            existing.GrantedScopes.Add(new OAuthGrantedScopeRecord
            {
                OAuthTokenId = existing.Id,
                Scope = scope,
            });
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("OAuth tokens stored for provider {Provider}, email {Email}", provider, email);

        // Redirect back to dashboard
        var returnUrl = Request.Cookies["skill-oauth-return"];
        Response.Cookies.Delete("skill-oauth-return");
        var dashboardUrl = returnUrl ?? "/integrations";

        return Redirect(dashboardUrl);
    }

    /// <summary>
    /// Returns the current OAuth connection status for a provider.
    /// </summary>
    [HttpGet("{provider}/status")]
    public async Task<IActionResult> Status(string provider)
    {
        var token = await _db.OAuthTokens.Include(t => t.GrantedScopes).FirstOrDefaultAsync(t => t.Provider == provider);
        if (token is null)
            return Ok(new { connected = false });

        return Ok(new
        {
            connected = true,
            email = token.Email,
            scopes = string.Join(' ', token.GetScopeSet()),
            expiresAt = token.ExpiresAtUtc,
        });
    }

    /// <summary>
    /// Disconnects OAuth for a provider (removes stored tokens).
    /// </summary>
    [HttpDelete("{provider}")]
    public async Task<IActionResult> Disconnect(string provider)
    {
        var token = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.Provider == provider);
        if (token is not null)
        {
            _db.OAuthTokens.Remove(token);
            await _db.SaveChangesAsync();
        }
        return Ok(new { disconnected = true });
    }

    private bool IsLocalhost =>
        Request.Host.Host is "localhost" or "127.0.0.1";

    private sealed record GoogleTokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")] string? AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")] int ExpiresIn,
        [property: System.Text.Json.Serialization.JsonPropertyName("scope")] string? Scope,
        [property: System.Text.Json.Serialization.JsonPropertyName("token_type")] string? TokenType);

    private sealed record GoogleUserInfo(
        [property: System.Text.Json.Serialization.JsonPropertyName("email")] string? Email);
}

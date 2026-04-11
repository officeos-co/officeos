using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly GoogleOAuthConfig _oauth;
    private readonly IUserRepository _users;
    private readonly ISessionRepository _sessions;
    private readonly IHttpClientFactory _httpFactory;

    public AuthController(
        GoogleOAuthConfig oauth,
        IUserRepository users,
        ISessionRepository sessions,
        IHttpClientFactory httpFactory)
    {
        _oauth = oauth;
        _users = users;
        _sessions = sessions;
        _httpFactory = httpFactory;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        var state = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        Response.Cookies.Append("oauth-state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = !Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
        });

        var url = "https://accounts.google.com/o/oauth2/v2/auth"
            + $"?client_id={Uri.EscapeDataString(_oauth.ClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(_oauth.RedirectUri)}"
            + "&response_type=code"
            + "&scope=openid%20email%20profile"
            + $"&state={Uri.EscapeDataString(state)}";

        return Redirect(url);
    }

    [HttpGet("callback/google")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct)
    {
        try
        {
            var savedState = Request.Cookies["oauth-state"];
            Response.Cookies.Delete("oauth-state");

            if (string.IsNullOrEmpty(savedState) || savedState != state)
                return BadRequest(new { error = "Invalid OAuth state", detail = $"cookie present: {savedState is not null}, match: {savedState == state}" });

            if (string.IsNullOrEmpty(_oauth.ClientId) || string.IsNullOrEmpty(_oauth.ClientSecret))
                return StatusCode(500, new { error = "Google OAuth not configured — ClientId or ClientSecret is empty" });

            // Exchange code for tokens
            var client = _httpFactory.CreateClient();
            var tokenResponse = await client.PostAsync("https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = _oauth.ClientId,
                    ["client_secret"] = _oauth.ClientSecret,
                    ["redirect_uri"] = _oauth.RedirectUri,
                    ["grant_type"] = "authorization_code",
                }), ct);

            var tokenBody = await tokenResponse.Content.ReadAsStringAsync(ct);
            if (!tokenResponse.IsSuccessStatusCode)
                return BadRequest(new { error = "Failed to exchange authorization code", detail = tokenBody });

            var tokenJson = JsonSerializer.Deserialize<JsonElement>(tokenBody);
            if (!tokenJson.TryGetProperty("access_token", out var atProp))
                return BadRequest(new { error = "Token response missing access_token", detail = tokenBody });
            var accessToken = atProp.GetString()!;

            // Fetch user info
            var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
            userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var userInfoResponse = await client.SendAsync(userInfoRequest, ct);

            if (!userInfoResponse.IsSuccessStatusCode)
            {
                var uiBody = await userInfoResponse.Content.ReadAsStringAsync(ct);
                return BadRequest(new { error = "Failed to fetch user info", detail = uiBody });
            }

            var userInfo = await userInfoResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
            var sub = userInfo.GetProperty("sub").GetString()!;
            var email = userInfo.GetProperty("email").GetString()!;
            var name = userInfo.TryGetProperty("name", out var n) ? n.GetString() : null;
            var avatar = userInfo.TryGetProperty("picture", out var p) ? p.GetString() : null;

            // Upsert user
            var user = await _users.UpsertByGoogleSubjectAsync(sub, email, name, avatar, ct);

            // Create session
            var sessionToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            var tokenHash = SessionAuthMiddleware.HashToken(sessionToken);
            await _sessions.CreateAsync(user.Id, tokenHash, DateTime.UtcNow.AddDays(7), ct);

            Response.Cookies.Append("eaos-session", sessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase),
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(7),
                Path = "/",
            });

            return Redirect("/");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "OAuth callback failed", detail = ex.Message, type = ex.GetType().Name });
        }
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var cookie = Request.Cookies["eaos-session"];
        if (!string.IsNullOrEmpty(cookie))
        {
            var tokenHash = SessionAuthMiddleware.HashToken(cookie);
            await _sessions.DeleteAsync(tokenHash, ct);
        }
        Response.Cookies.Delete("eaos-session");
        return Ok(new { ok = true });
    }

    [HttpGet("me")]
    public IActionResult Me()
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return Unauthorized(new { error = "Not authenticated" });

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.Name,
            avatarUrl = user.AvatarUrl,
        });
    }
}

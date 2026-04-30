namespace EnterpriseAgentOs.Api.Features.Agents;

/// <summary>
/// Handles OAuth2 flows for skill credentials.
/// Users initiate from the dashboard → redirect to provider → callback stores tokens.
/// Multiple skills sharing the same provider reuse one token (scopes are merged).
/// </summary>
[ApiController]
[Route("api/skills/oauth")]
public sealed class SkillOAuthController : ControllerBase
{
    private readonly ISkillOAuthService _oauthService;

    public SkillOAuthController(ISkillOAuthService oauthService)
    {
        _oauthService = oauthService;
    }

    [HttpGet("{provider}/start")]
    public async Task<IActionResult> Start(string provider, [FromQuery] string scopes, [FromQuery] string? returnUrl, CancellationToken ct)
    {
        try
        {
            var result = await _oauthService.PrepareStartAsync(provider, scopes, ct);

            Response.Cookies.Append("skill-oauth-state", result.State, new CookieOptions
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

            return Redirect(result.RedirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{provider}/status")]
    public async Task<IActionResult> Status(string provider, CancellationToken ct)
    {
        var result = await _oauthService.GetStatusAsync(provider, ct);
        if (!result.Connected)
            return Ok(new { connected = false });

        return Ok(new
        {
            connected = true,
            email = result.Email,
            scopes = result.Scopes,
            expiresAt = result.ExpiresAt,
        });
    }

    [HttpDelete("{provider}")]
    public async Task<IActionResult> Disconnect(string provider, CancellationToken ct)
    {
        await _oauthService.DisconnectAsync(provider, ct);
        return Ok(new { disconnected = true });
    }

    private bool IsLocalhost =>
        Request.Host.Host is "localhost" or "127.0.0.1";
}

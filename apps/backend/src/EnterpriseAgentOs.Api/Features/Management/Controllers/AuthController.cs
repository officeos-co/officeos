namespace EnterpriseAgentOs.Api.Features.Management;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin()
    {
        try
        {
            var result = _authService.BuildGoogleLoginUrl();

            Response.Cookies.Append("oauth-state", result.State, new CookieOptions
            {
                HttpOnly = true,
                Secure = !IsLocalhost,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10),
            });

            return Redirect(result.RedirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            return RedirectWithError(ex.Message);
        }
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
                return RedirectWithError("Invalid OAuth state — please try signing in again.");

            var result = await _authService.HandleGoogleCallbackAsync(code, ct);

            Response.Cookies.Append("eaos-session", result.SessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !IsLocalhost,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(7),
                Path = "/",
            });

            _logger.LogInformation("OAuth: login complete for {Email}, redirecting to /", result.Email);
            return Redirect("/");
        }
        catch (Exception ex)
        {
            return RedirectWithError($"Sign-in failed: {ex.Message}");
        }
    }

    [HttpGet("github")]
    public IActionResult GitHubLogin()
    {
        try
        {
            var result = _authService.BuildGitHubLoginUrl();

            Response.Cookies.Append("oauth-state", result.State, new CookieOptions
            {
                HttpOnly = true,
                Secure = !IsLocalhost,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromMinutes(10),
            });

            return Redirect(result.RedirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            return RedirectWithError(ex.Message);
        }
    }

    [HttpGet("callback/github")]
    public async Task<IActionResult> GitHubCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct)
    {
        try
        {
            var savedState = Request.Cookies["oauth-state"];
            Response.Cookies.Delete("oauth-state");

            if (string.IsNullOrEmpty(savedState) || savedState != state)
                return RedirectWithError("Invalid OAuth state — please try signing in again.");

            var result = await _authService.HandleGitHubCallbackAsync(code, ct);

            Response.Cookies.Append("eaos-session", result.SessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !IsLocalhost,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(7),
                Path = "/",
            });

            _logger.LogInformation("OAuth: GitHub login complete for {Email}, redirecting to /", result.Email);
            return Redirect("/");
        }
        catch (Exception ex)
        {
            return RedirectWithError($"Sign-in failed: {ex.Message}");
        }
    }

    private bool IsLocalhost => Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);

    private IActionResult RedirectWithError(string message)
        => Redirect($"/login?error={Uri.EscapeDataString(message)}");
}

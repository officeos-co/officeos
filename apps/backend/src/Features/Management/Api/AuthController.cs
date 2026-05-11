namespace OffceOs.Api.Features.Management;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWorkspaceService _workspaceService;
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly FrontendConfig _frontendConfig;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IWorkspaceService workspaceService,
        IIntegrationDefinitionService integrationDefinitionService,
        FrontendConfig frontendConfig,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _workspaceService = workspaceService;
        _integrationDefinitionService = integrationDefinitionService;
        _frontendConfig = frontendConfig;
        _logger = logger;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? returnTo = null)
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
            SetReturnToCookie(returnTo);

            return Redirect(result.RedirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            return RedirectWithError(ex.Message, returnTo);
        }
    }

    [HttpGet("callback/google")]
    public async Task<IActionResult> GoogleCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct)
    {
        string? returnTo = null;
        try
        {
            var savedState = Request.Cookies["oauth-state"];
            Response.Cookies.Delete("oauth-state");
            returnTo = GetAndClearReturnToCookie();

            if (string.IsNullOrEmpty(savedState) || savedState != state)
                return RedirectWithError("Invalid OAuth state - please try signing in again.", returnTo);

            var result = await _authService.HandleGoogleCallbackAsync(code, ct: ct);
            await SaveIntegrationOAuthCredentialAsync("google", returnTo, result.UserId, result.Email, result.IntegrationCredentials, result.Scopes, result.ExpiresAtUtc, ct);

            Response.Cookies.Append("eaos-session", result.SessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !IsLocalhost,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(7),
                Path = "/",
            });

            _logger.LogInformation("OAuth: login complete for {Email}, redirecting to {ReturnTo}", result.Email, returnTo);
            return Redirect(BuildFrontendRedirect(returnTo));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Google OAuth callback failed");
            return RedirectWithError(ex.Message, returnTo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google OAuth callback failed");
            return RedirectWithError("OAuth connection failed. Please try again.", returnTo);
        }
    }

    [HttpGet("github")]
    public IActionResult GitHubLogin([FromQuery] string? returnTo = null)
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
            SetReturnToCookie(returnTo);

            return Redirect(result.RedirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            return RedirectWithError(ex.Message, returnTo);
        }
    }

    [HttpGet("callback/github")]
    public async Task<IActionResult> GitHubCallback(
        [FromQuery] string code,
        [FromQuery] string state,
        CancellationToken ct)
    {
        string? returnTo = null;
        try
        {
            var savedState = Request.Cookies["oauth-state"];
            Response.Cookies.Delete("oauth-state");
            returnTo = GetAndClearReturnToCookie();

            if (string.IsNullOrEmpty(savedState) || savedState != state)
                return RedirectWithError("Invalid OAuth state - please try signing in again.", returnTo);

            var result = await _authService.HandleGitHubCallbackAsync(code, ct: ct);
            await SaveIntegrationOAuthCredentialAsync("github", returnTo, result.UserId, result.Email, result.IntegrationCredentials, result.Scopes, result.ExpiresAtUtc, ct);

            Response.Cookies.Append("eaos-session", result.SessionToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !IsLocalhost,
                SameSite = SameSiteMode.Lax,
                MaxAge = TimeSpan.FromDays(7),
                Path = "/",
            });

            _logger.LogInformation("OAuth: GitHub login complete for {Email}, redirecting to {ReturnTo}", result.Email, returnTo);
            return Redirect(BuildFrontendRedirect(returnTo));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "GitHub OAuth callback failed");
            return RedirectWithError(ex.Message, returnTo);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub OAuth callback failed");
            return RedirectWithError("OAuth connection failed. Please try again.", returnTo);
        }
    }

    private bool IsLocalhost => Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);

    private void SetReturnToCookie(string? returnTo)
    {
        if (!IsSafeLocalPath(returnTo)) return;

        Response.Cookies.Append("oauth-return-to", returnTo!, new CookieOptions
        {
            HttpOnly = true,
            Secure = !IsLocalhost,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/",
        });
    }

    private string GetAndClearReturnToCookie()
    {
        var returnTo = Request.Cookies["oauth-return-to"];
        Response.Cookies.Delete("oauth-return-to");
        return IsSafeLocalPath(returnTo) ? returnTo! : "/";
    }

    private static bool IsSafeLocalPath(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.StartsWith("/", StringComparison.Ordinal)
        && !value.StartsWith("//", StringComparison.Ordinal)
        && !value.Contains("://", StringComparison.Ordinal);

    private string BuildFrontendRedirect(string path)
    {
        if (!Uri.TryCreate(_frontendConfig.Origin, UriKind.Absolute, out var frontendOrigin))
            return path;

        return new Uri(frontendOrigin, path).ToString();
    }

    private IActionResult RedirectWithError(string message, string? returnTo = null)
    {
        var target = IsIntegrationReturn(returnTo)
            ? AppendQueryParameter(returnTo!, "oauthError", message)
            : $"/login?error={Uri.EscapeDataString(message)}";
        return Redirect(BuildFrontendRedirect(target));
    }

    private async Task SaveIntegrationOAuthCredentialAsync(
        string provider,
        string returnTo,
        Guid userId,
        string email,
        Dictionary<string, string> integrationCredentials,
        IReadOnlyList<string> scopes,
        DateTime? expiresAtUtc,
        CancellationToken ct)
    {
        if (!IsIntegrationReturn(returnTo))
            return;

        var workspace = await _workspaceService.GetCurrentAsync(userId, ct);
        await _integrationDefinitionService.SaveOAuthCredentialAsync(
            userId,
            workspace.Id,
            provider,
            integrationCredentials,
            scopes,
            email,
            expiresAtUtc,
            ct);
    }

    private static bool IsIntegrationReturn(string? returnTo)
    {
        if (!IsSafeLocalPath(returnTo))
            return false;

        var path = returnTo!.Split('?', '#')[0];
        return path.StartsWith("/integrations/", StringComparison.Ordinal)
            || string.Equals(path, "/integrations", StringComparison.Ordinal);
    }

    private static string AppendQueryParameter(string path, string name, string value)
    {
        var hashIndex = path.IndexOf('#', StringComparison.Ordinal);
        var fragment = hashIndex >= 0 ? path[hashIndex..] : string.Empty;
        var withoutFragment = hashIndex >= 0 ? path[..hashIndex] : path;
        var separator = withoutFragment.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{withoutFragment}{separator}{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}{fragment}";
    }
}

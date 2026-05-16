namespace OffceOs.Api.Features.Management;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IWorkspaceService _workspaceService;
    private readonly IIntegrationDefinitionService _integrationDefinitionService;
    private readonly IAgentRoutineCredentialRepository _agentRoutineCredentialRepository;
    private readonly CredentialProtector _credentialProtector;
    private readonly FrontendConfig _frontendConfig;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IWorkspaceService workspaceService,
        IIntegrationDefinitionService integrationDefinitionService,
        IAgentRoutineCredentialRepository agentRoutineCredentialRepository,
        CredentialProtector credentialProtector,
        FrontendConfig frontendConfig,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _workspaceService = workspaceService;
        _integrationDefinitionService = integrationDefinitionService;
        _agentRoutineCredentialRepository = agentRoutineCredentialRepository;
        _credentialProtector = credentialProtector;
        _frontendConfig = frontendConfig;
        _logger = logger;
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? returnTo = null)
    {
        try
        {
            var result = _authService.BuildGoogleLoginUrl();
            SetOAuthCookies(result.State, returnTo);
            return Redirect(result.RedirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            return RedirectWithError(ex.Message, returnTo);
        }
    }

    [HttpGet("callback/google")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state, CancellationToken ct)
    {
        string? returnTo = null;
        try
        {
            returnTo = ValidateCallbackState(state);
            var result = await _authService.HandleGoogleCallbackAsync(code, ct: ct);
            await SaveIntegrationOAuthCredentialAsync("google", returnTo, result.UserId, result.Email, result.IntegrationCredentials, result.Scopes, result.ExpiresAtUtc, ct);
            SetSessionCookie(result.SessionToken);

            _logger.LogInformation("OAuth: Google login complete for {Email}, redirecting to {ReturnTo}", result.Email, returnTo);
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
            SetOAuthCookies(result.State, returnTo);
            return Redirect(result.RedirectUrl);
        }
        catch (InvalidOperationException ex)
        {
            return RedirectWithError(ex.Message, returnTo);
        }
    }

    [HttpGet("callback/github")]
    public async Task<IActionResult> GitHubCallback([FromQuery] string code, [FromQuery] string state, CancellationToken ct)
    {
        string? returnTo = null;
        try
        {
            returnTo = ValidateCallbackState(state);
            var result = await _authService.HandleGitHubCallbackAsync(code, ct: ct);
            await SaveIntegrationOAuthCredentialAsync("github", returnTo, result.UserId, result.Email, result.IntegrationCredentials, result.Scopes, result.ExpiresAtUtc, ct);
            await SaveRoutineCredentialOAuthAsync("github", returnTo, result.UserId, result.Email, result.IntegrationCredentials, result.Scopes, result.ExpiresAtUtc, ct);
            SetSessionCookie(result.SessionToken);

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

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _authService.LogoutAsync(Request.Cookies["eaos-session"], ct);
        Response.Cookies.Delete("eaos-session");
        return Ok(new { ok = true });
    }

    private bool IsLocalhost => Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase);

    private void SetOAuthCookies(string state, string? returnTo)
    {
        Response.Cookies.Append("oauth-state", state, new CookieOptions
        {
            HttpOnly = true,
            Secure = !IsLocalhost,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
            Path = "/",
        });
        SetReturnToCookie(returnTo);
    }

    private string ValidateCallbackState(string state)
    {
        var savedState = Request.Cookies["oauth-state"];
        Response.Cookies.Delete("oauth-state");
        var returnTo = GetAndClearReturnToCookie();

        if (string.IsNullOrEmpty(savedState) || savedState != state)
            throw new InvalidOperationException("Invalid OAuth state - please try signing in again.");

        return returnTo;
    }

    private void SetSessionCookie(string sessionToken)
    {
        Response.Cookies.Append("eaos-session", sessionToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = !IsLocalhost,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromDays(7),
            Path = "/",
        });
    }

    private void SetReturnToCookie(string? returnTo)
    {
        if (!IsSafeLocalPath(returnTo))
            return;

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
        var target = IsIntegrationReturn(returnTo) || IsCredentialReturn(returnTo)
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

    private async Task SaveRoutineCredentialOAuthAsync(
        string provider,
        string returnTo,
        Guid userId,
        string email,
        Dictionary<string, string> credentials,
        IReadOnlyList<string> scopes,
        DateTime? expiresAtUtc,
        CancellationToken ct)
    {
        if (!IsCredentialReturn(returnTo))
            return;

        var workspace = await _workspaceService.GetCurrentAsync(userId, ct);
        var credentialName = CredentialNameFromReturn(returnTo) ?? provider;
        var now = DateTime.UtcNow;
        var existing = await _agentRoutineCredentialRepository.GetByNameAsync(workspace.Id, credentialName, ct);
        await _agentRoutineCredentialRepository.UpsertAsync(new AgentRoutineCredentialRecord
        {
            Id = existing?.Id ?? Guid.NewGuid(),
            OwnerId = userId,
            WorkspaceId = workspace.Id,
            Name = credentialName,
            Provider = provider,
            AuthKind = AgentRoutineCredentialAuthKinds.OAuth,
            EncryptedSecret = _credentialProtector.Protect(credentials),
            PublicMetadataJson = JsonSerializer.Serialize(new { provider, email }),
            ScopesJson = JsonSerializer.Serialize(scopes),
            ExpiresAtUtc = expiresAtUtc,
            CreatedAt = existing?.CreatedAt ?? now,
            UpdatedAt = now,
        }, ct);
    }

    private static bool IsCredentialReturn(string? returnTo)
    {
        if (!IsSafeLocalPath(returnTo))
            return false;

        var path = returnTo!.Split('?', '#')[0];
        return path.StartsWith("/credentials/", StringComparison.Ordinal)
            || string.Equals(path, "/credentials", StringComparison.Ordinal);
    }

    private static string? CredentialNameFromReturn(string returnTo)
    {
        var path = returnTo.Split('?', '#')[0].Trim('/');
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length >= 2 && parts[0] == "credentials" ? parts[1] : null;
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

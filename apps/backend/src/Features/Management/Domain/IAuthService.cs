namespace OffceOs.Features.Management.Domain;

public sealed record GoogleLoginResult(string RedirectUrl, string State);

public sealed record GoogleCallbackResult(
    string SessionToken,
    Guid UserId,
    string Email,
    Dictionary<string, string> IntegrationCredentials,
    IReadOnlyList<string> Scopes,
    DateTime? ExpiresAtUtc);

public sealed record GitHubLoginResult(string RedirectUrl, string State);

public sealed record GitHubCallbackResult(
    string SessionToken,
    Guid UserId,
    string Email,
    Dictionary<string, string> IntegrationCredentials,
    IReadOnlyList<string> Scopes,
    DateTime? ExpiresAtUtc);

public interface IAuthService
{
    GoogleLoginResult BuildGoogleLoginUrl(string? redirectUri = null);
    Task<GoogleCallbackResult> HandleGoogleCallbackAsync(string code, string? redirectUri = null, CancellationToken ct = default);
    GitHubLoginResult BuildGitHubLoginUrl(string? redirectUri = null);
    Task<GitHubCallbackResult> HandleGitHubCallbackAsync(string code, string? redirectUri = null, CancellationToken ct = default);
    Task<string> CreateSessionTokenAsync(Guid userId, TimeSpan lifetime, CancellationToken ct = default);
    Task<UserRecord> UpdateProfileAsync(Guid userId, string? name, string? displayName, string? timezone, string? notificationPrefsJson, string? preferences, CancellationToken ct = default);
    Task<bool> LogoutAsync(string? sessionToken, CancellationToken ct = default);
}

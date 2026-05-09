namespace OffceOs.Domain.Features.Management;

public sealed record GoogleLoginResult(string RedirectUrl, string State);

public sealed record GoogleCallbackResult(string SessionToken, string Email);

public sealed record GitHubLoginResult(string RedirectUrl, string State);

public sealed record GitHubCallbackResult(string SessionToken, string Email);

public interface IAuthService
{
    GoogleLoginResult BuildGoogleLoginUrl(string? redirectUri = null);
    Task<GoogleCallbackResult> HandleGoogleCallbackAsync(string code, string? redirectUri = null, CancellationToken ct = default);
    GitHubLoginResult BuildGitHubLoginUrl(string? redirectUri = null);
    Task<GitHubCallbackResult> HandleGitHubCallbackAsync(string code, string? redirectUri = null, CancellationToken ct = default);
    Task<UserRecord> UpdateProfileAsync(Guid userId, string? name, string? displayName, string? timezone, string? notificationPrefsJson, string? preferences, CancellationToken ct = default);
    Task<bool> LogoutAsync(string? sessionToken, CancellationToken ct = default);
}

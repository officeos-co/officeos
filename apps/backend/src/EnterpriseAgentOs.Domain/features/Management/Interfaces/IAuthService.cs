namespace EnterpriseAgentOs.Domain.Features.Management;

public sealed record GoogleLoginResult(string RedirectUrl, string State);

public sealed record GoogleCallbackResult(string SessionToken, string Email);

public sealed record GitHubLoginResult(string RedirectUrl, string State);

public sealed record GitHubCallbackResult(string SessionToken, string Email);

public interface IAuthService
{
    GoogleLoginResult BuildGoogleLoginUrl();
    Task<GoogleCallbackResult> HandleGoogleCallbackAsync(string code, CancellationToken ct = default);
    GitHubLoginResult BuildGitHubLoginUrl();
    Task<GitHubCallbackResult> HandleGitHubCallbackAsync(string code, CancellationToken ct = default);
}

namespace EnterpriseAgentOs.Domain.Features.Agents;

public record SkillOAuthStartResult(string RedirectUrl, string State, string MergedScopes);
public record SkillOAuthStatusResult(bool Connected, string? Email, string? Scopes, DateTime? ExpiresAt);

public interface ISkillOAuthService
{
    Task<SkillOAuthStartResult> PrepareStartAsync(string provider, string scopes, CancellationToken ct = default);
    Task<string?> ExchangeCallbackAsync(string provider, string code, CancellationToken ct = default);
    Task<SkillOAuthStatusResult> GetStatusAsync(string provider, CancellationToken ct = default);
    Task DisconnectAsync(string provider, CancellationToken ct = default);
}

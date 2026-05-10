namespace OffceOs.Application.Features.Providers;

public interface ICodexProviderSetupService
{
    Task<CodexOAuthLoginResult> StartOAuthLoginAsync(Guid actorUserId, CancellationToken ct = default);
    Task<CodexOAuthStatusResult> PollOAuthLoginAsync(Guid actorUserId, string loginId, CancellationToken ct = default);
    Task<bool> DisconnectAsync(Guid actorUserId, CancellationToken ct = default);
}

public interface ICodexAppServerService
{
    Task<CodexOAuthLoginResult> StartLoginAsync(Guid userId, CancellationToken ct = default);
    Task<CodexOAuthStatusResult> PollLoginAsync(string loginId, CancellationToken ct = default);
    Task<AgentResult<LlmDispatchResponse>> DispatchAsync(ProviderAuthResult auth, string model, JsonElement requestBody, CancellationToken ct = default);
}

public sealed record CodexOAuthLoginResult(
    string LoginId,
    string AuthUrl,
    DateTime ExpiresAt);

public sealed record CodexOAuthStatusResult(
    string LoginId,
    bool Completed,
    bool Success,
    string? Error,
    string? AccountEmail,
    string? PlanType,
    IReadOnlyDictionary<string, string> Credentials);

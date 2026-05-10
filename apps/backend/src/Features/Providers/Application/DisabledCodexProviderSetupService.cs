namespace OffceOs.Application.Features.Providers;

internal sealed class DisabledCodexProviderSetupService : ICodexProviderSetupService
{
    public Task<CodexOAuthLoginResult> StartOAuthLoginAsync(Guid actorUserId, CancellationToken ct = default) =>
        throw new InvalidOperationException("OpenAI Codex OAuth is available only in development.");

    public Task<CodexOAuthStatusResult> PollOAuthLoginAsync(Guid actorUserId, string loginId, CancellationToken ct = default) =>
        throw new InvalidOperationException("OpenAI Codex OAuth is available only in development.");

    public Task<bool> DisconnectAsync(Guid actorUserId, CancellationToken ct = default) =>
        throw new InvalidOperationException("OpenAI Codex OAuth is available only in development.");
}

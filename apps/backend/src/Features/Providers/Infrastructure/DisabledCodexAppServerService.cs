namespace OffceOs.Infrastructure.Features.Providers;

internal sealed class DisabledCodexAppServerService : ICodexAppServerService
{
    public Task<CodexOAuthLoginResult> StartLoginAsync(Guid userId, CancellationToken ct = default) =>
        throw new InvalidOperationException("OpenAI Codex OAuth is available only in development.");

    public Task<CodexOAuthStatusResult> PollLoginAsync(string loginId, CancellationToken ct = default) =>
        throw new InvalidOperationException("OpenAI Codex OAuth is available only in development.");

    public Task<AgentResult<LlmDispatchResponse>> DispatchAsync(ProviderAuthResult auth, string model, JsonElement requestBody, CancellationToken ct = default) =>
        Task.FromResult<AgentResult<LlmDispatchResponse>>(new AgentError(
            AgentErrorCategory.Configuration,
            "OpenAI Codex OAuth is available only in development."));
}

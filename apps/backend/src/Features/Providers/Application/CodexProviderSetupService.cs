namespace OffceOs.Application.Features.Providers;

internal sealed class CodexProviderSetupService : ICodexProviderSetupService
{
    private readonly IOAuthTokenRepository _oauthTokenRepository;
    private readonly ICodexAppServerService _codexAppServerService;
    private readonly CredentialProtector _credentialProtector;

    public CodexProviderSetupService(
        IOAuthTokenRepository oauthTokenRepository,
        ICodexAppServerService codexAppServerService,
        CredentialProtector credentialProtector)
    {
        _oauthTokenRepository = oauthTokenRepository;
        _codexAppServerService = codexAppServerService;
        _credentialProtector = credentialProtector;
    }

    public async Task<CodexOAuthLoginResult> StartOAuthLoginAsync(Guid actorUserId, CancellationToken ct = default)
    {
        return await _codexAppServerService.StartLoginAsync(actorUserId, ct);
    }

    public async Task<CodexOAuthStatusResult> PollOAuthLoginAsync(Guid actorUserId, string loginId, CancellationToken ct = default)
    {
        var status = await _codexAppServerService.PollLoginAsync(loginId, ct);
        if (!status.Completed || !status.Success)
            return status;

        var token = new OAuthTokenRecord
        {
            UserId = actorUserId,
            Provider = OAuthProvider.OpenAiCodex.ToStorageString(),
            EncryptedAccessToken = _credentialProtector.Protect(new Dictionary<string, string>(status.Credentials, StringComparer.OrdinalIgnoreCase)),
            Email = status.AccountEmail,
            ExpiresAtUtc = null,
        };
        token.ReplaceScopes([ProviderAuthKind.CodexChatGptOAuth.ToStorageString()]);
        await _oauthTokenRepository.UpsertAsync(token, ct);
        return status;
    }

    public async Task<bool> DisconnectAsync(Guid actorUserId, CancellationToken ct = default)
    {
        return await _oauthTokenRepository.DeleteAsync(
            new OAuthTokenFilter
            {
                UserId = actorUserId,
                Provider = OAuthProvider.OpenAiCodex.ToStorageString(),
            },
            ct);
    }
}

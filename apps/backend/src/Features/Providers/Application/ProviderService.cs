using OffceOs.Features.Providers.Domain;
using OffceOs.Common.Infrastructure.Security;
namespace OffceOs.Features.Providers.Application;

internal sealed class ProviderService : IProviderService
{
    private const string DefaultCodexClientId = "app_EMoamEEZ73f0CkXaXp7hrann";
    private const string DefaultCodexTokenUrl = "https://auth.openai.com/oauth/token";
    private static readonly TimeSpan CodexRefreshSkew = TimeSpan.FromMinutes(2);

    private readonly IProviderResourceRepository _providerResourceRepository;
    private readonly CredentialProtector _credentialProtector;
    private readonly IHttpClientFactory? _httpClientFactory;

    public ProviderService(
        IProviderResourceRepository providerResourceRepository,
        CredentialProtector credentialProtector,
        IHttpClientFactory? httpClientFactory = null)
    {
        _providerResourceRepository = providerResourceRepository;
        _credentialProtector = credentialProtector;
        _httpClientFactory = httpClientFactory;
    }

    public Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<ProviderResult>>([]);

    public async Task<IReadOnlyList<ProviderResult>> ListForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default)
    {
        if (!workspaceId.HasValue)
            return [];

        var providers = await _providerResourceRepository.ListAsync(workspaceId.Value, ct);
        return providers.Select(ToProviderResult).ToList();
    }

    public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default) =>
        Task.FromResult<string?>(null);

    public async Task<string?> GetApiKeyForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default)
    {
        var auth = await GetAuthForDispatchAsync(name, workspaceId, ct);
        return auth?.Kind is ProviderAuthKind.ApiKey or ProviderAuthKind.AwsBedrockApiKey or ProviderAuthKind.AzureApiKey
            ? auth.Get("apiKey")
            : null;
    }

    public async Task<ProviderAuthResult?> GetAuthForDispatchAsync(string name, Guid? workspaceId, CancellationToken ct = default)
    {
        if (!workspaceId.HasValue)
            return null;

        var provider = await _providerResourceRepository.GetByNameAsync(workspaceId.Value, name, ct);
        if (provider is null || !provider.Enabled || string.IsNullOrWhiteSpace(provider.EncryptedCredentialsJson))
            return null;

        var credentials = _credentialProtector.Unprotect(provider.EncryptedCredentialsJson);
        var kind = provider.AuthKind.ToProviderAuthKind();
        if (kind == ProviderAuthKind.CodexChatGptOAuth)
            return await RefreshCodexAuthIfNeededAsync(provider, credentials, ct);

        return new ProviderAuthResult(kind, credentials);
    }

    public async Task<bool> IsModelAllowedAsync(string provider, string? model, Guid? workspaceId, CancellationToken ct = default)
    {
        if (!workspaceId.HasValue)
            return false;

        var resource = await _providerResourceRepository.GetByNameAsync(workspaceId.Value, provider, ct);
        if (resource is null || !resource.Enabled)
            return false;

        var effectiveModel = string.IsNullOrWhiteSpace(model) ? ProviderRegistry.DefaultModel : model.Trim();
        if (effectiveModel.Equals(ProviderRegistry.DefaultModel, StringComparison.OrdinalIgnoreCase))
            return resource.Models.Count > 0;

        return resource.Models.Any(allowed => allowed.Equals(effectiveModel, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<ProviderResourceAuthResult> AuthenticateCodexAsync(Guid workspaceId, CodexProviderAuthRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.AccessToken))
            throw new InvalidOperationException("Codex access token is required.");
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw new InvalidOperationException("Codex refresh token is required.");

        var now = DateTime.UtcNow;
        var definition = ProviderRegistry.Get(ProviderRegistry.CodexProviderSlug)
            ?? throw new InvalidOperationException("Codex provider is not registered.");
        var account = FirstNonEmpty(request.AccountEmail, request.AccountId);
        var credentials = new Dictionary<string, string>
        {
            ["accessToken"] = request.AccessToken.Trim(),
            ["refreshToken"] = request.RefreshToken.Trim(),
            ["authSource"] = "codex_subscription",
        };
        if (request.ExpiresAt.HasValue)
            credentials["expiresAt"] = request.ExpiresAt.Value.ToUniversalTime().ToString("O");
        if (!string.IsNullOrWhiteSpace(request.AccountId))
            credentials["accountId"] = request.AccountId.Trim();
        if (!string.IsNullOrWhiteSpace(request.AccountEmail))
            credentials["accountEmail"] = request.AccountEmail.Trim();
        if (!string.IsNullOrWhiteSpace(request.ClientId))
            credentials["clientId"] = request.ClientId.Trim();
        if (!string.IsNullOrWhiteSpace(request.TokenUrl))
            credentials["tokenUrl"] = request.TokenUrl.Trim();
        if (request.Scopes is { Count: > 0 })
            credentials["scopes"] = string.Join(" ", request.Scopes.Where(scope => !string.IsNullOrWhiteSpace(scope)).Select(scope => scope.Trim()));

        var resource = await _providerResourceRepository.UpsertAsync(new ProviderResourceRecord
        {
            WorkspaceId = workspaceId,
            Name = ProviderRegistry.CodexProviderSlug,
            Type = ProviderRegistry.CodexProviderSlug,
            DisplayName = definition.DisplayName,
            Enabled = true,
            DefaultModel = definition.Models.FirstOrDefault(model => model.SmartTier == SmartRoutingTier.Standard)?.Id
                ?? definition.Models.FirstOrDefault()?.Id,
            Models = definition.Models.Select(model => model.Id).ToList(),
            AuthKind = ProviderAuthKind.CodexChatGptOAuth.ToStorageString(),
            EncryptedCredentialsJson = _credentialProtector.Protect(credentials),
            Phase = ProviderResourcePhaseKinds.Ready,
            StatusMessage = "Codex subscription credentials configured.",
            Account = account,
            ExpiresAt = request.ExpiresAt?.ToUniversalTime(),
            LastValidatedAt = now,
        }, ct);

        return ToProviderResourceAuthResult(resource);
    }

    private static ProviderResult ToProviderResult(ProviderResourceRecord resource)
    {
        var definition = ProviderRegistry.Get(resource.Type);
        var displayName = string.IsNullOrWhiteSpace(resource.DisplayName)
            ? definition?.DisplayName ?? resource.Name
            : resource.DisplayName;
        var models = resource.Models.Count == 0 && definition is not null
            ? definition.Models.Select(model => model.Id).ToList()
            : resource.Models;

        return new ProviderResult(
            resource.Id,
            resource.Name,
            displayName,
            resource.Enabled && !string.IsNullOrWhiteSpace(resource.EncryptedCredentialsJson),
            resource.UpdatedAt,
            models.Select(model =>
            {
                var definitionModel = definition?.Models.FirstOrDefault(item => item.Id.Equals(model, StringComparison.OrdinalIgnoreCase));
                return new ProviderModelResult(model, definitionModel?.DisplayName ?? model, definitionModel?.CostWeight ?? 1);
            }).ToList(),
            resource.Phase,
            resource.StatusMessage,
            resource.Account,
            resource.ExpiresAt,
            resource.LastValidatedAt);
    }

    private static ProviderResourceAuthResult ToProviderResourceAuthResult(ProviderResourceRecord resource) => new(
        "Provider",
        resource.Name,
        resource.Type,
        resource.Phase,
        resource.StatusMessage,
        resource.Account,
        resource.ExpiresAt,
        resource.LastValidatedAt);

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private async Task<ProviderAuthResult?> RefreshCodexAuthIfNeededAsync(
        ProviderResourceRecord provider,
        Dictionary<string, string> credentials,
        CancellationToken ct)
    {
        if (!DateTime.TryParse(credentials.GetValueOrDefault("expiresAt"), out var expiresAt) ||
            expiresAt.ToUniversalTime() > DateTime.UtcNow.Add(CodexRefreshSkew))
        {
            return new ProviderAuthResult(ProviderAuthKind.CodexChatGptOAuth, credentials);
        }

        if (_httpClientFactory is null)
            return new ProviderAuthResult(ProviderAuthKind.CodexChatGptOAuth, credentials);

        try
        {
            var refreshed = await RefreshCodexTokenAsync(credentials, ct);
            var newExpiresAt = DateTime.UtcNow.AddSeconds(refreshed.ExpiresIn);
            credentials["accessToken"] = refreshed.AccessToken;
            credentials["refreshToken"] = string.IsNullOrWhiteSpace(refreshed.RefreshToken)
                ? credentials["refreshToken"]
                : refreshed.RefreshToken;
            credentials["expiresAt"] = newExpiresAt.ToString("O");

            await _providerResourceRepository.UpsertAsync(provider with
            {
                EncryptedCredentialsJson = _credentialProtector.Protect(credentials),
                Phase = ProviderResourcePhaseKinds.Ready,
                StatusMessage = "Codex subscription credentials refreshed.",
                ExpiresAt = newExpiresAt,
                LastValidatedAt = DateTime.UtcNow,
            }, ct);

            return new ProviderAuthResult(ProviderAuthKind.CodexChatGptOAuth, credentials);
        }
        catch
        {
            await _providerResourceRepository.UpsertAsync(provider with
            {
                Phase = ProviderResourcePhaseKinds.Error,
                StatusMessage = "Codex credentials expired and refresh failed. Run `officeos provider auth codex` again.",
                LastValidatedAt = DateTime.UtcNow,
            }, ct);
            return null;
        }
    }

    private async Task<CodexRefreshResult> RefreshCodexTokenAsync(Dictionary<string, string> credentials, CancellationToken ct)
    {
        var refreshToken = credentials.GetValueOrDefault("refreshToken");
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException("Codex refresh token is missing.");

        var tokenUrl = credentials.GetValueOrDefault("tokenUrl") ?? DefaultCodexTokenUrl;
        var clientId = credentials.GetValueOrDefault("clientId") ?? DefaultCodexClientId;
        var client = _httpClientFactory!.CreateClient("llm-proxy");
        using var response = await client.PostAsync(tokenUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = clientId,
        }), ct);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CodexRefreshTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Codex token refresh returned an empty response.");
        if (string.IsNullOrWhiteSpace(payload.AccessToken))
            throw new InvalidOperationException("Codex token refresh did not return an access token.");

        return new CodexRefreshResult(
            payload.AccessToken,
            payload.RefreshToken,
            payload.ExpiresIn > 0 ? payload.ExpiresIn : 3600);
    }

    private sealed record CodexRefreshResult(string AccessToken, string? RefreshToken, int ExpiresIn);

    private sealed record CodexRefreshTokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

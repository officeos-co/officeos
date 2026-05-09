using System.Security.Cryptography;
using System.Text;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class ProviderService : IProviderService
{
    private readonly PlatformKeysConfig _platformKeysConfig;
    private readonly CustomLlmProviderConfig _customLlmProviderConfig;

    public ProviderService(
        PlatformKeysConfig platformKeys,
        CustomLlmProviderConfig customLlmProviderConfig)
    {
        _platformKeysConfig = platformKeys;
        _customLlmProviderConfig = customLlmProviderConfig;
    }

    public Task<IReadOnlyList<ProviderResult>> ListAsync(CancellationToken ct = default)
    {
        var list = ProviderRegistry.DashboardProviders
            .Select(def => new ProviderResult(
                DeterministicGuid(def.Slug),
                def.Slug,
                def.DisplayName,
                HasPlatformKey(def.Slug),
                null,
                def.Models.Select(m => new ProviderModelResult(m.Id, m.DisplayName, m.CostWeight)).ToList()))
            .ToList();

        list.Add(new ProviderResult(
            DeterministicGuid(ProviderRegistry.CustomProviderSlug),
            ProviderRegistry.CustomProviderSlug,
            _customLlmProviderConfig.EffectiveDisplayName,
            _customLlmProviderConfig.IsConfigured,
            null,
            GetCustomModels()));

        return Task.FromResult<IReadOnlyList<ProviderResult>>(list);
    }

    public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default)
    {
        if (ProviderRegistry.IsCustomProvider(name))
            return Task.FromResult(_customLlmProviderConfig.IsConfigured
                ? _customLlmProviderConfig.ApiKeyOrNull ?? string.Empty
                : null);

        var key = _platformKeysConfig.GetKey(ProviderRegistry.Get(name)?.PlatformKeyConfigName);
        return Task.FromResult(key);
    }

    private bool HasPlatformKey(string name) =>
        _platformKeysConfig.GetKey(ProviderRegistry.Get(name)?.PlatformKeyConfigName) is not null;

    private IReadOnlyList<ProviderModelResult> GetCustomModels() =>
        _customLlmProviderConfig.IsConfigured
            ? new[]
            {
                new ProviderModelResult(
                    _customLlmProviderConfig.ModelId.Trim(),
                    _customLlmProviderConfig.EffectiveModelDisplayName,
                    _customLlmProviderConfig.EffectiveCostWeight),
            }
            : [];

    private static Guid DeterministicGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"provider:{input}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}

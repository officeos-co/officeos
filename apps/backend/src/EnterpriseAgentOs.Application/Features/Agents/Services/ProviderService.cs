using System.Security.Cryptography;
using System.Text;

namespace EnterpriseAgentOs.Application.Features.Agents;

internal sealed class ProviderService : IProviderService
{
    private readonly PlatformKeysConfig _platformKeysConfig;

    public ProviderService(PlatformKeysConfig platformKeys)
    {
        _platformKeysConfig = platformKeys;
    }

    public Task<IReadOnlyList<ProviderDto>> ListAsync(CancellationToken ct = default)
    {
        var list = ProviderRegistry.SeedableProviders
            .Select(def => new ProviderDto(
                DeterministicGuid(def.Slug),
                def.Slug,
                def.DisplayName,
                HasPlatformKey(def.Slug),
                null))
            .ToList();
        return Task.FromResult<IReadOnlyList<ProviderDto>>(list);
    }

    public Task<string?> GetApiKeyForDispatchAsync(string name, CancellationToken ct = default)
    {
        var key = _platformKeysConfig.GetKey(ProviderRegistry.Get(name)?.PlatformKeyConfigName);
        return Task.FromResult(key);
    }

    private bool HasPlatformKey(string name) =>
        _platformKeysConfig.GetKey(ProviderRegistry.Get(name)?.PlatformKeyConfigName) is not null;

    private static Guid DeterministicGuid(string input)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"provider:{input}"));
        return new Guid(hash.AsSpan(0, 16));
    }
}

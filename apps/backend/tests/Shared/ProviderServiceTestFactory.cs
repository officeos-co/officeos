using OffceOs.Application.Features.Agents;
using OffceOs.Configuration;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Management;
using Microsoft.AspNetCore.DataProtection;

namespace OffceOs.Tests.Shared;

internal static class ProviderServiceTestFactory
{
    public static ProviderService CreateService(PlatformKeysConfig platformKeysConfig, CustomLlmProviderConfig customLlmProviderConfig)
    {
        var db = TestDbFactory.Create("provider-service");
        var protector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-provider-keys-{Guid.NewGuid():N}"))));
        return new ProviderService(
            platformKeysConfig,
            customLlmProviderConfig,
            new OrganizationProviderProfileRepository(db),
            new WorkspaceRepository(db),
            protector);
    }
}

using Microsoft.AspNetCore.DataProtection;
using OffceOs.Application.Features.Providers;
using OffceOs.Domain.Features.Providers;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Providers;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Providers;

public sealed class ProviderServiceTests
{
    [Fact]
    public async Task Authenticate_codex_upserts_provider_without_exposing_tokens()
    {
        await using var db = TestDbFactory.Create("codex-provider-auth");
        var repository = new ProviderResourceRepository(db);
        var service = new ProviderService(repository, CreateCredentialProtector());
        var workspaceId = Guid.NewGuid();

        var result = await service.AuthenticateCodexAsync(workspaceId, new CodexProviderAuthRequest(
            "access-token",
            "refresh-token",
            new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc),
            "user@example.com",
            "account-1",
            "client-id",
            "https://auth.example.test/oauth/token",
            ["openid", "offline_access"]));

        Assert.Equal("Provider", result.Kind);
        Assert.Equal("codex", result.Name);
        Assert.Equal("codex", result.Type);
        Assert.Equal(ProviderResourcePhaseKinds.Ready, result.Phase);
        Assert.Equal("user@example.com", result.Account);

        var stored = await repository.GetByNameAsync(workspaceId, "codex");
        Assert.NotNull(stored);
        Assert.Equal(ProviderAuthKind.CodexChatGptOAuth.ToStorageString(), stored.AuthKind);
        Assert.Equal("Codex", stored.DisplayName);
        Assert.Contains("gpt-5.3-codex", stored.Models);
        Assert.DoesNotContain("access-token", stored.EncryptedCredentialsJson);
        Assert.DoesNotContain("refresh-token", stored.EncryptedCredentialsJson);
    }

    private static CredentialProtector CreateCredentialProtector()
    {
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-provider-tests-{Guid.NewGuid():N}");
        return new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(keyRingPath)));
    }
}

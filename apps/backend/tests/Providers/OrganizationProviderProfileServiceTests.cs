using OffceOs.Application.Features.Providers;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Providers;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Billing;
using OffceOs.Infrastructure.Features.Management;
using OffceOs.Infrastructure.Features.Providers;
using OffceOs.Tests.Shared;
using Microsoft.AspNetCore.DataProtection;
using Xunit;

namespace OffceOs.Tests.Providers;

public sealed class OrganizationProviderProfileServiceTests
{
    [Fact]
    public async Task Free_org_admin_cannot_manage_enterprise_provider_profiles()
    {
        await using var db = TestDbFactory.Create("provider-profile-service");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Free);
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ListAsync(ownerId, organizationId));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(
                ownerId,
                organizationId,
                ProviderRegistry.AwsBedrockProviderSlug,
                "Bedrock",
                ["us.anthropic.claude-sonnet-4-6"],
                "aws-key",
                true));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteAsync(ownerId, organizationId, ProviderRegistry.AwsBedrockProviderSlug));
    }

    [Fact]
    public async Task Non_admin_enterprise_member_cannot_manage_provider_profiles()
    {
        await using var db = TestDbFactory.Create("provider-profile-service");
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        db.Users.Add(new UserEntity { Id = memberId, Email = "member@example.com", Name = "Member", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.OrgMembers.Add(new OrgMemberEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = memberId,
            Email = "member@example.com",
            Role = OrgRole.Member.ToStorageString(),
            Status = MemberStatus.Active.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(
                memberId,
                organizationId,
                ProviderRegistry.AwsBedrockProviderSlug,
                "Bedrock",
                ["us.anthropic.claude-sonnet-4-6"],
                "aws-key",
                true));
    }

    [Fact]
    public async Task Enterprise_cloud_provider_profiles_require_valid_pinned_models()
    {
        await using var db = TestDbFactory.Create("provider-profile-service");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(ownerId, organizationId, ProviderRegistry.AwsBedrockProviderSlug, "Bedrock", [], "aws-key", true));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(ownerId, organizationId, ProviderRegistry.AwsBedrockProviderSlug, "Bedrock", ["claude-sonnet-4-6"], "aws-key", true));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(ownerId, organizationId, "not-a-provider", "Unknown", ["model"], "key", true));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveAsync(ownerId, organizationId, ProviderRegistry.AwsBedrockProviderSlug, "Bedrock", ["us.anthropic.claude-sonnet-4-6"], " ", true));
    }

    [Fact]
    public async Task Enterprise_provider_profile_save_normalizes_duplicate_models_and_redacts_key()
    {
        await using var db = TestDbFactory.Create("provider-profile-service");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        var service = CreateService(db);

        var saved = await service.SaveAsync(
            ownerId,
            organizationId,
            ProviderRegistry.AwsBedrockProviderSlug,
            " Bedrock ",
            [
                " us.anthropic.claude-sonnet-4-6 ",
                "us.anthropic.claude-sonnet-4-6",
                ""
            ],
            "aws-key",
            true);

        Assert.Equal("Bedrock", saved.DisplayName);
        Assert.Equal("""["us.anthropic.claude-sonnet-4-6"]""", saved.AllowedModelsJson);
        Assert.DoesNotContain("aws-key", saved.EncryptedApiKey);
    }

    [Fact]
    public async Task Native_cloud_auth_profiles_validate_required_fields_and_store_encrypted_payload()
    {
        await using var db = TestDbFactory.Create("provider-profile-service");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveNativeAuthAsync(
                ownerId,
                organizationId,
                ProviderRegistry.GoogleVertexProviderSlug,
                "Vertex",
                ["claude-sonnet-4-6"],
                ProviderAuthKind.GoogleServiceAccountFile,
                new Dictionary<string, string>
                {
                    ["credentialsPath"] = "/var/run/secrets/gcp/claude.json",
                    ["projectId"] = "acme",
                },
                true));

        var saved = await service.SaveNativeAuthAsync(
            ownerId,
            organizationId,
            ProviderRegistry.GoogleVertexProviderSlug,
            "Vertex",
            ["claude-sonnet-4-6"],
            ProviderAuthKind.GoogleServiceAccountFile,
            new Dictionary<string, string>
            {
                ["credentialsPath"] = "/var/run/secrets/gcp/claude.json",
                ["projectId"] = "acme",
                ["location"] = "us-east5",
            },
            true);

        Assert.DoesNotContain("/var/run/secrets/gcp/claude.json", saved.EncryptedApiKey);
        Assert.DoesNotContain("acme", saved.EncryptedApiKey);
    }

    [Fact]
    public async Task Codex_oauth_profile_is_separate_from_openai_api_key_profile()
    {
        await using var db = TestDbFactory.Create("provider-profile-service");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveNativeAuthAsync(
                ownerId,
                organizationId,
                ProviderRegistry.OpenAiCodexProviderSlug,
                "OpenAI Codex",
                ProviderRegistry.GetModelIds(ProviderRegistry.OpenAiCodexProviderSlug),
                ProviderAuthKind.ApiKey,
                new Dictionary<string, string> { ["apiKey"] = "sk-test" },
                true));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveNativeAuthAsync(
                ownerId,
                organizationId,
                "openai",
                "OpenAI",
                ["gpt-4o-mini"],
                ProviderAuthKind.CodexChatGptOAuth,
                new Dictionary<string, string> { ["authJson"] = "{}" },
                true));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveNativeAuthAsync(
                ownerId,
                organizationId,
                ProviderRegistry.OpenAiCodexProviderSlug,
                "OpenAI Codex",
                ["gpt-5.5"],
                ProviderAuthKind.CodexChatGptOAuth,
                new Dictionary<string, string>
                {
                    ["authJson"] = """{"tokens":"redacted"}""",
                    ["accountEmail"] = "codex@example.com",
                    ["planType"] = "pro",
                },
                true));

        Assert.Contains("personal user", ex.Message);
    }

    private static OrganizationProviderProfileService CreateService(EaosDbContext db)
    {
        var protector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-provider-profile-keys-{Guid.NewGuid():N}"))));
        return new OrganizationProviderProfileService(
            new OrganizationProviderProfileRepository(db),
            new OrganizationRepository(db),
            protector,
            new ProviderEnterprisePolicy(new OrgSubscriptionRepository(db)),
            new NoopPublisher());
    }

    private static Guid SeedOrganization(EaosDbContext db, Guid ownerId, SubscriptionPlan plan)
    {
        var organizationId = Guid.NewGuid();
        db.Users.Add(new UserEntity { Id = ownerId, Email = "owner@example.com", Name = "Owner", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Organizations.Add(new OrganizationEntity { Id = organizationId, Name = "Acme", OwnerUserId = ownerId, CreatedAt = DateTime.UtcNow });
        db.OrgMembers.Add(new OrgMemberEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = ownerId,
            Email = "owner@example.com",
            Role = OrgRole.Owner.ToStorageString(),
            Status = MemberStatus.Active.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
        });
        db.OrgSubscriptions.Add(new OrgSubscriptionEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId.ToString(),
            Plan = plan.ToStorageString(),
            ConcurrentAgentLimit = plan == SubscriptionPlan.Enterprise ? 100 : 1,
            CreditBudgetPerMonth = plan == SubscriptionPlan.Enterprise ? 1_000_000_000 : 500_000,
            CreditsUsedThisMonth = 0,
            PeriodStart = DateTime.UtcNow.AddDays(-1),
            PeriodEnd = DateTime.UtcNow.AddMonths(1),
            IsActive = true,
            OverageEnabled = plan == SubscriptionPlan.Enterprise,
        });
        db.SaveChanges();
        return organizationId;
    }
}

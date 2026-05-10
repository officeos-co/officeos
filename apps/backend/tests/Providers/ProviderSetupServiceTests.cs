using System.Net;
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
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Providers;

public sealed class ProviderSetupServiceTests
{
    [Fact]
    public async Task Bedrock_setup_matches_claude_auth_choices_and_returns_redacted_env_status()
    {
        await using var db = TestDbFactory.Create("provider-setup");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        var service = CreateService(db);

        var saved = await service.SaveBedrockSetupAsync(
            ownerId,
            new BedrockProviderSetupRequest(
                organizationId,
                "AWS Prod",
                "us-east-1",
                ProviderAuthKind.AwsProfile,
                AwsProfile: "prod-sso",
                AwsAccessKeyId: null,
                AwsSecretAccessKey: null,
                AwsSessionToken: null,
                BedrockApiKey: null,
                BaseUrl: null,
                SkipProviderAuth: false,
                PinnedModels:
                [
                    "us.anthropic.claude-sonnet-4-6",
                    "us.anthropic.claude-haiku-4-5-20251001-v1:0",
                ],
                Enabled: true));

        Assert.Equal(ProviderRegistry.AwsBedrockProviderSlug, saved.Provider);

        var status = Assert.Single(await service.GetSetupStatusAsync(ownerId, organizationId));
        Assert.True(status.Configured);
        Assert.Equal(ProviderAuthKind.AwsProfile.ToStorageString(), status.AuthKind);
        Assert.Equal("1", status.Environment["CLAUDE_CODE_USE_BEDROCK"]);
        Assert.Equal("us-east-1", status.Environment["AWS_REGION"]);
        Assert.Equal("prod-sso", status.Environment["AWS_PROFILE"]);
        Assert.Equal("us.anthropic.claude-sonnet-4-6", status.Environment["ANTHROPIC_DEFAULT_SONNET_MODEL"]);
        Assert.DoesNotContain(status.Environment, pair => pair.Value.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Claude_cloud_setup_requires_enterprise_org_and_pinned_models()
    {
        await using var db = TestDbFactory.Create("provider-setup");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Free);
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveBedrockSetupAsync(
                ownerId,
                new BedrockProviderSetupRequest(
                    organizationId,
                    "Bedrock",
                    "us-east-1",
                    ProviderAuthKind.AwsEnvironment,
                    AwsProfile: null,
                    AwsAccessKeyId: null,
                    AwsSecretAccessKey: null,
                    AwsSessionToken: null,
                    BedrockApiKey: null,
                    BaseUrl: null,
                    SkipProviderAuth: false,
                    PinnedModels: ["us.anthropic.claude-sonnet-4-6"],
                    Enabled: true)));

        var enterpriseOrganizationId = SeedOrganization(db, Guid.NewGuid(), SubscriptionPlan.Enterprise);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveVertexSetupAsync(
                ownerId,
                new VertexProviderSetupRequest(
                    enterpriseOrganizationId,
                    "Vertex",
                    "acme-project",
                    "global",
                    ProviderAuthKind.GoogleApplicationDefault,
                    CredentialsPath: null,
                    BaseUrl: null,
                    SkipProviderAuth: false,
                    PinnedModels: [],
                    Enabled: true)));
    }

    [Fact]
    public async Task Vertex_setup_uses_adc_or_service_account_file_path_not_stored_json_keys()
    {
        await using var db = TestDbFactory.Create("provider-setup");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveVertexSetupAsync(
                ownerId,
                new VertexProviderSetupRequest(
                    organizationId,
                    "Vertex",
                    "acme-project",
                    "global",
                    ProviderAuthKind.GoogleServiceAccountFile,
                    CredentialsPath: null,
                    BaseUrl: null,
                    SkipProviderAuth: false,
                    PinnedModels: ["claude-sonnet-4-6"],
                    Enabled: true)));

        await service.SaveVertexSetupAsync(
            ownerId,
            new VertexProviderSetupRequest(
                organizationId,
                "Vertex",
                "acme-project",
                "global",
                ProviderAuthKind.GoogleServiceAccountFile,
                CredentialsPath: "/var/run/secrets/gcp/claude.json",
                BaseUrl: null,
                SkipProviderAuth: false,
                PinnedModels: ["claude-sonnet-4-6"],
                Enabled: true));

        var status = Assert.Single(await service.GetSetupStatusAsync(ownerId, organizationId));
        Assert.Equal(ProviderRegistry.GoogleVertexProviderSlug, status.Provider);
        Assert.Equal(ProviderAuthKind.GoogleServiceAccountFile.ToStorageString(), status.AuthKind);
        Assert.Equal("1", status.Environment["CLAUDE_CODE_USE_VERTEX"]);
        Assert.Equal("global", status.Environment["CLOUD_ML_REGION"]);
        Assert.Equal("acme-project", status.Environment["ANTHROPIC_VERTEX_PROJECT_ID"]);
        Assert.Equal("/var/run/secrets/gcp/claude.json", status.Environment["GOOGLE_APPLICATION_CREDENTIALS"]);
        Assert.DoesNotContain("client_email", string.Join(' ', status.Environment.Values));
    }

    [Fact]
    public async Task Foundry_setup_supports_only_api_key_or_default_credential_chain()
    {
        await using var db = TestDbFactory.Create("provider-setup");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        var service = CreateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveFoundrySetupAsync(
                ownerId,
                new FoundryProviderSetupRequest(
                    organizationId,
                    "Foundry",
                    Resource: "acme-ai",
                    BaseUrl: null,
                    ProviderAuthKind.AzureEntraClientSecret,
                    ApiKey: null,
                    SkipProviderAuth: false,
                    PinnedModels: ["claude-sonnet-4-6"],
                    Enabled: true)));

        await service.SaveFoundrySetupAsync(
            ownerId,
            new FoundryProviderSetupRequest(
                organizationId,
                "Foundry",
                Resource: "acme-ai",
                BaseUrl: null,
                ProviderAuthKind.AzureApiKey,
                ApiKey: "foundry-key",
                SkipProviderAuth: false,
                PinnedModels: ["claude-sonnet-4-6"],
                Enabled: true));

        var status = Assert.Single(await service.GetSetupStatusAsync(ownerId, organizationId));
        Assert.Equal("acme-ai", status.Environment["ANTHROPIC_FOUNDRY_RESOURCE"]);
        Assert.Equal("<configured>", status.Environment["ANTHROPIC_FOUNDRY_API_KEY"]);
        Assert.DoesNotContain("foundry-key", string.Join(' ', status.Environment.Values));
    }

    [Fact]
    public async Task Gateway_setup_stores_base_url_and_skip_auth_without_cloud_secrets()
    {
        await using var db = TestDbFactory.Create("provider-setup");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        var service = CreateService(db);

        await service.SaveBedrockSetupAsync(
            ownerId,
            new BedrockProviderSetupRequest(
                organizationId,
                "Gateway Bedrock",
                AwsRegion: null,
                ProviderAuthKind.AwsEnvironment,
                AwsProfile: null,
                AwsAccessKeyId: null,
                AwsSecretAccessKey: null,
                AwsSessionToken: null,
                BedrockApiKey: null,
                BaseUrl: "https://llm-gateway.example.com/bedrock",
                SkipProviderAuth: true,
                PinnedModels: ["us.anthropic.claude-sonnet-4-6"],
                Enabled: true));

        var status = Assert.Single(await service.GetSetupStatusAsync(ownerId, organizationId));
        Assert.Equal(ProviderAuthKind.Gateway.ToStorageString(), status.AuthKind);
        Assert.Equal("https://llm-gateway.example.com/bedrock", status.Environment["ANTHROPIC_BEDROCK_BASE_URL"]);
        Assert.Equal("1", status.Environment["CLAUDE_CODE_SKIP_BEDROCK_AUTH"]);
        Assert.DoesNotContain(status.Environment.Keys, key => key.StartsWith("AWS_ACCESS_KEY", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Model_access_check_uses_configured_profile_and_returns_provider_error_body()
    {
        await using var db = TestDbFactory.Create("provider-setup");
        var ownerId = Guid.NewGuid();
        var organizationId = SeedOrganization(db, ownerId, SubscriptionPlan.Enterprise);
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("model access denied"),
        });
        var service = CreateService(db, handler);
        await service.SaveFoundrySetupAsync(
            ownerId,
            new FoundryProviderSetupRequest(
                organizationId,
                "Foundry",
                Resource: "acme-ai",
                BaseUrl: "https://acme.services.ai.azure.com/anthropic",
                ProviderAuthKind.AzureApiKey,
                ApiKey: "foundry-key",
                SkipProviderAuth: false,
                PinnedModels: ["claude-sonnet-4-6"],
                Enabled: true));

        var result = await service.CheckModelAccessAsync(
            ownerId,
            organizationId,
            ProviderRegistry.AzureFoundryProviderSlug,
            "claude-sonnet-4-6");

        Assert.False(result.Accessible);
        Assert.Contains("model access denied", result.Message);
        Assert.Single(handler.Requests);
    }

    private static ProviderSetupService CreateService(EaosDbContext db, RecordingHandler? handler = null)
    {
        var protector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-provider-setup-keys-{Guid.NewGuid():N}"))));
        var profileRepository = new OrganizationProviderProfileRepository(db);
        var organizationRepository = new OrganizationRepository(db);
        var enterprisePolicy = new ProviderEnterprisePolicy(new OrgSubscriptionRepository(db));
        var profileService = new OrganizationProviderProfileService(profileRepository, organizationRepository, protector, enterprisePolicy, new NoopPublisher());
        var dispatcher = new LlmProviderDispatcher(
            new FakeHttpClientFactory(handler ?? new RecordingHandler(_ => HttpResponseFactory.SseResponse("data: [DONE]\n\n"))),
            NullLogger<LlmProviderDispatcher>.Instance);
        return new ProviderSetupService(
            profileRepository,
            organizationRepository,
            profileService,
            protector,
            enterprisePolicy,
            dispatcher);
    }

    private static Guid SeedOrganization(EaosDbContext db, Guid ownerId, SubscriptionPlan plan)
    {
        var organizationId = Guid.NewGuid();
        var email = $"{ownerId:N}@example.com";
        db.Users.Add(new UserEntity { Id = ownerId, Email = email, Name = "Owner", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Organizations.Add(new OrganizationEntity { Id = organizationId, Name = "Acme", OwnerUserId = ownerId, CreatedAt = DateTime.UtcNow });
        db.OrgMembers.Add(new OrgMemberEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = ownerId,
            Email = email,
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

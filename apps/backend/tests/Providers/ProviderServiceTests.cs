using OffceOs.Application.Features.Providers;
using OffceOs.Domain.Features.Providers;
using OffceOs.Configuration;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Management;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Billing;
using OffceOs.Infrastructure.Features.Management;
using OffceOs.Infrastructure.Features.Providers;
using OffceOs.Tests.Shared;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace OffceOs.Tests.Providers;

public sealed class ProviderServiceTests
{
    [Fact]
    public async Task ListAsync_marks_only_platform_providers_with_keys_as_configured()
    {
        var service = ProviderServiceTestFactory.CreateService(
            new PlatformKeysConfig { OpenAiApiKey = "openai-key" },
            new CustomLlmProviderConfig());

        var providers = await service.ListAsync();

        Assert.True(Assert.Single(providers, p => p.Name == "openai").Configured);
        Assert.False(Assert.Single(providers, p => p.Name == "anthropic").Configured);
        Assert.False(Assert.Single(providers, p => p.Name == "google").Configured);
        Assert.False(Assert.Single(providers, p => p.Name == "xai").Configured);
    }

    [Fact]
    public async Task Environment_variables_bind_to_connected_platform_and_custom_providers()
    {
        var previousOpenAi = Environment.GetEnvironmentVariable("PLATFORMKEYS__OPENAIAPIKEY");
        var previousCustomBaseUrl = Environment.GetEnvironmentVariable("CUSTOMLLMPROVIDER__BASEURL");
        var previousCustomModelId = Environment.GetEnvironmentVariable("CUSTOMLLMPROVIDER__MODELID");

        try
        {
            Environment.SetEnvironmentVariable("PLATFORMKEYS__OPENAIAPIKEY", "openai-key");
            Environment.SetEnvironmentVariable("CUSTOMLLMPROVIDER__BASEURL", "http://localhost:11434/v1");
            Environment.SetEnvironmentVariable("CUSTOMLLMPROVIDER__MODELID", "deepseek-r1:8b");

            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            var platformKeys = new PlatformKeysConfig();
            var customProvider = new CustomLlmProviderConfig();
            configuration.GetSection("PlatformKeys").Bind(platformKeys);
            configuration.GetSection("CustomLlmProvider").Bind(customProvider);

            var service = ProviderServiceTestFactory.CreateService(platformKeys, customProvider);
            var providers = await service.ListAsync();

            Assert.True(Assert.Single(providers, p => p.Name == "openai").Configured);
            Assert.False(Assert.Single(providers, p => p.Name == "anthropic").Configured);
            Assert.True(Assert.Single(providers, p => p.Name == ProviderRegistry.CustomProviderSlug).Configured);
        }
        finally
        {
            Environment.SetEnvironmentVariable("PLATFORMKEYS__OPENAIAPIKEY", previousOpenAi);
            Environment.SetEnvironmentVariable("CUSTOMLLMPROVIDER__BASEURL", previousCustomBaseUrl);
            Environment.SetEnvironmentVariable("CUSTOMLLMPROVIDER__MODELID", previousCustomModelId);
        }
    }

    [Fact]
    public async Task ListAsync_includes_configured_custom_provider_with_configured_model()
    {
        var service = ProviderServiceTestFactory.CreateService(
            new PlatformKeysConfig(),
            new CustomLlmProviderConfig
            {
                BaseUrl = "http://localhost:11434/v1",
                ModelId = "deepseek-r1:8b",
                DisplayName = "Local Ollama",
                ModelDisplayName = "DeepSeek R1 8B",
                CostWeight = 3,
            });

        var providers = await service.ListAsync();
        var custom = Assert.Single(providers, p => p.Name == ProviderRegistry.CustomProviderSlug);
        var model = Assert.Single(custom.Models);

        Assert.True(custom.Configured);
        Assert.Equal("Local Ollama", custom.DisplayName);
        Assert.Equal("deepseek-r1:8b", model.Id);
        Assert.Equal("DeepSeek R1 8B", model.DisplayName);
        Assert.Equal(3, model.CostWeight);
    }

    [Fact]
    public async Task ListAsync_includes_unconfigured_custom_provider_without_models()
    {
        var service = ProviderServiceTestFactory.CreateService(
            new PlatformKeysConfig(),
            new CustomLlmProviderConfig { BaseUrl = "http://localhost:11434/v1" });

        var providers = await service.ListAsync();
        var custom = Assert.Single(providers, p => p.Name == ProviderRegistry.CustomProviderSlug);

        Assert.False(custom.Configured);
        Assert.Equal("Self-hosted", custom.DisplayName);
        Assert.Empty(custom.Models);
    }

    [Fact]
    public async Task GetApiKeyForDispatchAsync_returns_empty_string_for_configured_custom_provider_without_key()
    {
        var service = ProviderServiceTestFactory.CreateService(
            new PlatformKeysConfig(),
            new CustomLlmProviderConfig
            {
                BaseUrl = "http://localhost:11434/v1",
                ModelId = "deepseek-r1:8b",
            });

        var key = await service.GetApiKeyForDispatchAsync(ProviderRegistry.CustomProviderSlug);

        Assert.Equal(string.Empty, key);
    }

    [Fact]
    public async Task Workspace_provider_profiles_override_platform_provider_list_and_key()
    {
        await using var db = TestDbFactory.Create("provider-service");
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var protector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-provider-keys-{Guid.NewGuid():N}"))));
        db.Users.Add(new UserEntity { Id = userId, Email = "owner@example.com", Name = "Owner", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Organizations.Add(new OrganizationEntity { Id = organizationId, Name = "Acme", OwnerUserId = userId, CreatedAt = DateTime.UtcNow });
        db.Workspaces.Add(new WorkspaceEntity
        {
            Id = workspaceId,
            OrganizationId = organizationId,
            OwnerKind = WorkspaceOwnerKind.Organization.ToStorageString(),
            Name = "Ops",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        db.OrganizationProviderProfiles.Add(new OrganizationProviderProfileEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Provider = "openai",
            DisplayName = "Org OpenAI",
            AllowedModelsJson = """["gpt-4o-mini"]""",
            EncryptedApiKey = protector.Protect(new Dictionary<string, string> { ["apiKey"] = "org-key" }),
            Enabled = true,
            ConfiguredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        SeedEnterpriseSubscription(db, organizationId);
        await db.SaveChangesAsync();

        var service = new ProviderService(
            new PlatformKeysConfig { AnthropicApiKey = "platform-anthropic" },
            new CustomLlmProviderConfig(),
            new OrganizationProviderProfileRepository(db),
            new WorkspaceRepository(db),
            protector,
            new ProviderEnterprisePolicy(new OrgSubscriptionRepository(db)));

        var providers = await service.ListForWorkspaceAsync(workspaceId);
        var key = await service.GetApiKeyForDispatchAsync("openai", workspaceId);
        var openAi = Assert.Single(providers);

        Assert.Equal("openai", openAi.Name);
        Assert.Equal("Org OpenAI", openAi.DisplayName);
        Assert.Equal("gpt-4o-mini", Assert.Single(openAi.Models).Id);
        Assert.Equal("org-key", key);
        Assert.True(await service.IsModelAllowedAsync("openai", "gpt-4o-mini", workspaceId));
        Assert.False(await service.IsModelAllowedAsync("anthropic", "claude-sonnet-4-5", workspaceId));
    }

    [Fact]
    public async Task Non_enterprise_workspace_does_not_list_or_use_cloud_provider_profiles()
    {
        await using var db = TestDbFactory.Create("provider-service");
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var protector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-provider-keys-{Guid.NewGuid():N}"))));
        db.Users.Add(new UserEntity { Id = userId, Email = "owner@example.com", Name = "Owner", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Organizations.Add(new OrganizationEntity { Id = organizationId, Name = "Acme", OwnerUserId = userId, CreatedAt = DateTime.UtcNow });
        db.Workspaces.Add(new WorkspaceEntity
        {
            Id = workspaceId,
            OrganizationId = organizationId,
            OwnerKind = WorkspaceOwnerKind.Organization.ToStorageString(),
            Name = "Ops",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        db.OrganizationProviderProfiles.Add(new OrganizationProviderProfileEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Provider = ProviderRegistry.AwsBedrockProviderSlug,
            DisplayName = "Bedrock",
            AllowedModelsJson = """["anthropic.claude-sonnet-4-20250514-v1:0"]""",
            EncryptedApiKey = protector.Protect(new Dictionary<string, string>
            {
                ["authKind"] = ProviderAuthKind.AwsIam.ToStorageString(),
                ["awsAccessKeyId"] = "AKIATEST",
                ["awsSecretAccessKey"] = "secret",
                ["awsRegion"] = "us-east-1",
            }),
            Enabled = true,
            ConfiguredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var service = new ProviderService(
            new PlatformKeysConfig { OpenAiApiKey = "platform-openai" },
            new CustomLlmProviderConfig(),
            new OrganizationProviderProfileRepository(db),
            new WorkspaceRepository(db),
            protector,
            new ProviderEnterprisePolicy(new OrgSubscriptionRepository(db)));

        var providers = await service.ListForWorkspaceAsync(workspaceId);

        Assert.DoesNotContain(providers, provider => provider.Name == ProviderRegistry.AwsBedrockProviderSlug);
        Assert.True(Assert.Single(providers, provider => provider.Name == "openai").Configured);
        Assert.Null(await service.GetApiKeyForDispatchAsync(ProviderRegistry.AwsBedrockProviderSlug, workspaceId));
        Assert.Null(await service.GetAuthForDispatchAsync(ProviderRegistry.AwsBedrockProviderSlug, workspaceId));
        Assert.False(await service.IsModelAllowedAsync(
            ProviderRegistry.AwsBedrockProviderSlug,
            "anthropic.claude-sonnet-4-20250514-v1:0",
            workspaceId));
    }

    [Fact]
    public async Task Enterprise_workspace_lists_multiple_cloud_profiles_with_provider_scoped_model_selection()
    {
        await using var db = TestDbFactory.Create("provider-service");
        var userId = Guid.NewGuid();
        var organizationId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var protector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-provider-keys-{Guid.NewGuid():N}"))));
        db.Users.Add(new UserEntity { Id = userId, Email = "owner@example.com", Name = "Owner", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow });
        db.Organizations.Add(new OrganizationEntity { Id = organizationId, Name = "Acme", OwnerUserId = userId, CreatedAt = DateTime.UtcNow });
        db.Workspaces.Add(new WorkspaceEntity
        {
            Id = workspaceId,
            OrganizationId = organizationId,
            OwnerKind = WorkspaceOwnerKind.Organization.ToStorageString(),
            Name = "Ops",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        });
        SeedEnterpriseSubscription(db, organizationId);
        db.OrganizationProviderProfiles.AddRange(
            new OrganizationProviderProfileEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Provider = ProviderRegistry.AwsBedrockProviderSlug,
                DisplayName = "Bedrock",
                AllowedModelsJson = """["anthropic.claude-sonnet-4-20250514-v1:0"]""",
                EncryptedApiKey = protector.Protect(new Dictionary<string, string>
                {
                    ["authKind"] = ProviderAuthKind.AwsIam.ToStorageString(),
                    ["awsAccessKeyId"] = "AKIATEST",
                    ["awsSecretAccessKey"] = "secret",
                    ["awsRegion"] = "us-east-1",
                }),
                Enabled = true,
                ConfiguredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new OrganizationProviderProfileEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Provider = ProviderRegistry.GoogleVertexProviderSlug,
                DisplayName = "Vertex",
                AllowedModelsJson = """["claude-sonnet-4@20250514"]""",
                EncryptedApiKey = protector.Protect(new Dictionary<string, string> { ["apiKey"] = "gcp-key" }),
                Enabled = true,
                ConfiguredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            },
            new OrganizationProviderProfileEntity
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                Provider = ProviderRegistry.AzureFoundryProviderSlug,
                DisplayName = "Foundry",
                AllowedModelsJson = """["claude-sonnet-4-20250514"]""",
                EncryptedApiKey = protector.Protect(new Dictionary<string, string> { ["apiKey"] = "azure-key" }),
                Enabled = false,
                ConfiguredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            });
        await db.SaveChangesAsync();

        var service = new ProviderService(
            new PlatformKeysConfig { OpenAiApiKey = "platform-openai" },
            new CustomLlmProviderConfig(),
            new OrganizationProviderProfileRepository(db),
            new WorkspaceRepository(db),
            protector,
            new ProviderEnterprisePolicy(new OrgSubscriptionRepository(db)));

        var providers = await service.ListForWorkspaceAsync(workspaceId);

        Assert.Equal(
            [ProviderRegistry.AwsBedrockProviderSlug, ProviderRegistry.GoogleVertexProviderSlug],
            providers.Select(provider => provider.Name).OrderBy(name => name).ToList());
        Assert.True(await service.IsModelAllowedAsync(
            ProviderRegistry.AwsBedrockProviderSlug,
            "anthropic.claude-sonnet-4-20250514-v1:0",
            workspaceId));
        Assert.False(await service.IsModelAllowedAsync(
            ProviderRegistry.AwsBedrockProviderSlug,
            "claude-sonnet-4@20250514",
            workspaceId));
        Assert.Equal("gcp-key", await service.GetApiKeyForDispatchAsync(ProviderRegistry.GoogleVertexProviderSlug, workspaceId));
        Assert.Null(await service.GetApiKeyForDispatchAsync(ProviderRegistry.AzureFoundryProviderSlug, workspaceId));
        var bedrockAuth = await service.GetAuthForDispatchAsync(ProviderRegistry.AwsBedrockProviderSlug, workspaceId);
        Assert.NotNull(bedrockAuth);
        Assert.Equal(ProviderAuthKind.AwsIam, bedrockAuth.Kind);
        Assert.Equal("us-east-1", bedrockAuth.Get("awsRegion"));
    }

    private static void SeedEnterpriseSubscription(EaosDbContext db, Guid organizationId)
    {
        db.OrgSubscriptions.Add(new OrgSubscriptionEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId.ToString(),
            Plan = SubscriptionPlan.Enterprise.ToStorageString(),
            ConcurrentAgentLimit = 100,
            CreditBudgetPerMonth = 1_000_000_000,
            CreditsUsedThisMonth = 0,
            PeriodStart = DateTime.UtcNow.AddDays(-1),
            PeriodEnd = DateTime.UtcNow.AddMonths(1),
            IsActive = true,
            OverageEnabled = true,
        });
    }

}

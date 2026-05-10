using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Common.Services;
using OffceOs.Configuration;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Management;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Management;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class ProviderServiceTests
{
    [Fact]
    public async Task ListAsync_marks_only_platform_providers_with_keys_as_configured()
    {
        var service = CreateService(
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

            var service = CreateService(platformKeys, customProvider);
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
        var service = CreateService(
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
        var service = CreateService(
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
        var service = CreateService(
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
        await using var db = CreateDb();
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
        await db.SaveChangesAsync();

        var service = new ProviderService(
            new PlatformKeysConfig { AnthropicApiKey = "platform-anthropic" },
            new CustomLlmProviderConfig(),
            new OrganizationProviderProfileRepository(db),
            new WorkspaceRepository(db),
            protector);

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

    private static EaosDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EaosDbContext>()
            .UseInMemoryDatabase($"provider-service-{Guid.NewGuid():N}")
            .Options;
        return new EaosDbContext(options);
    }

    private static ProviderService CreateService(PlatformKeysConfig platformKeysConfig, CustomLlmProviderConfig customLlmProviderConfig)
    {
        var db = CreateDb();
        var protector = new CredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"eaos-provider-keys-{Guid.NewGuid():N}"))));
        return new ProviderService(
            platformKeysConfig,
            customLlmProviderConfig,
            new OrganizationProviderProfileRepository(db),
            new WorkspaceRepository(db),
            protector);
    }
}

using OffceOs.Application.Features.Agents;
using OffceOs.Domain.Common.Services;
using OffceOs.Configuration;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class ProviderServiceTests
{
    [Fact]
    public async Task ListAsync_marks_only_platform_providers_with_keys_as_configured()
    {
        var service = new ProviderService(
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

            var service = new ProviderService(platformKeys, customProvider);
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
        var service = new ProviderService(
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
        var service = new ProviderService(
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
        var service = new ProviderService(
            new PlatformKeysConfig(),
            new CustomLlmProviderConfig
            {
                BaseUrl = "http://localhost:11434/v1",
                ModelId = "deepseek-r1:8b",
            });

        var key = await service.GetApiKeyForDispatchAsync(ProviderRegistry.CustomProviderSlug);

        Assert.Equal(string.Empty, key);
    }
}

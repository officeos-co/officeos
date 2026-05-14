using OffceOs.Application.Features.AgentDefinitions;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Channels;
using OffceOs.Application.Features.Providers;
using OffceOs.Database;
using OffceOs.Domain.Features.Providers;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Tests.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace OffceOs.Tests.Agents;

public sealed class AgentServiceTests
{
    [Fact]
    public async Task CreateAsync_accepts_configured_non_api_key_provider_auth()
    {
        await using var db = WorkspaceTestHarness.CreateDb("agent-service");
        var service = CreateService(
            db,
            new FakeProviderService(
                new ProviderAuthResult(
                    ProviderAuthKind.CodexChatGptOAuth,
                    new Dictionary<string, string> { ["authJson"] = "{}" })));

        var agent = await service.CreateAsync(
            new CreateAgentRequest(
                "Codex Agent",
                ProviderRegistry.OpenAiCodexProviderSlug,
                "gpt-5.5"),
            ownerId: Guid.NewGuid(),
            workspaceId: Guid.NewGuid());

        Assert.Equal(ProviderRegistry.OpenAiCodexProviderSlug, agent.Provider);
        Assert.Equal("gpt-5.5", agent.Model);
    }

    private static AgentService CreateService(EaosDbContext db, IProviderService providerService)
    {
        var agentRepository = new AgentRepository(db);
        var agentDefinitionRepository = new AgentDefinitionRepository(db);
        return new AgentService(
            agentRepository,
            new FakeAgentDeployer(),
            providerService,
            NullLogger<AgentService>.Instance,
            new InMemoryDistributedCache(),
            new AgentPersonalityRepository(db),
            new NoopPublisher(),
            new AgentChannelBinder(new ChannelRepository(db)),
            new FakeAgentLogService(),
            IntegrationDefinitionServiceTestFactory.CreateService(),
            agentDefinitionRepository,
            new AgentDefinitionParser());
    }
}

using OffceOs.Application.Features.AgentDefinitions;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Channels;
using OffceOs.Application.Features.Providers;
using OffceOs.Database;
using OffceOs.Domain.Features.Providers;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Tests.Shared;
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
                    ProviderAuthKind.AwsBedrockApiKey,
                    new Dictionary<string, string> { ["apiKey"] = "bedrock-key" })));

        var agent = await service.CreateAsync(
            new CreateAgentRequest(
                "Bedrock Agent",
                ProviderRegistry.AwsBedrockProviderSlug,
                "us.anthropic.claude-haiku-4-5-20251001-v1:0"),
            ownerId: Guid.NewGuid(),
            workspaceId: Guid.NewGuid());

        Assert.Equal(ProviderRegistry.AwsBedrockProviderSlug, agent.Provider);
        Assert.Equal("us.anthropic.claude-haiku-4-5-20251001-v1:0", agent.Model);
    }

    private static AgentService CreateService(EaosDbContext db, IProviderService providerService)
    {
        var agentRepository = new AgentRepository(db);
        var agentDefinitionRepository = new AgentDefinitionRepository(db);
        return new AgentService(
            agentRepository,
            new FakeAgentDeployer(),
            providerService,
            new InMemoryDistributedCache(),
            new AgentPersonalityRepository(db),
            new NoopPublisher(),
            new AgentChannelBinder(new ChannelRepository(db)),
            new FakeResourceLogService(),
            new FakeResourceLogWriterService(),
            IntegrationDefinitionServiceTestFactory.CreateService(),
            agentDefinitionRepository,
            new AgentDefinitionParser());
    }
}

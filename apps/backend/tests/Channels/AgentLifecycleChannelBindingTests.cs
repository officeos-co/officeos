using OffceOs.Application.Features.Agents;
using OffceOs.Database.Models;
using OffceOs.Domain.Common.ValueObjects;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Channels;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Tests.Shared;
using Xunit;

namespace OffceOs.Tests.Channels;

public sealed class AgentLifecycleChannelBindingTests
{
    [Fact]
    public async Task CreateAgent_binds_exact_channel_connection_ids_when_multiple_connections_share_kind()
    {
        await using var db = WorkspaceTestHarness.CreateDb("agent-dashboard-channels");
        var ownerId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        var harness = WorkspaceTestHarness.Create(db);
        var workspace = await harness.Workspaces.GetCurrentAsync(ownerId);
        var channelRepository = new ChannelRepository(db);
        var telegramOps = await channelRepository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Ops", ownerId, workspace.Id));
        var telegramSupport = await channelRepository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Support", ownerId, workspace.Id));

        var agent = await harness.AgentLifecycle.CreateAsync(
            new CreateAgentLifecycleRequest(
                Name: "Ops Agent",
                Provider: "openai",
                Model: "gpt-4o-mini",
                Prompt: null,
                ConfigJson: null,
                IntegrationSlugs: null,
                ChannelConnectionIds: [telegramSupport.Id],
                ToolNames: null,
                Resources: null,
                BootstrapMessage: null),
            ownerId,
            workspace.Id);

        var bindings = await channelRepository.ListBindingsAsync(agent.Id);
        var binding = Assert.Single(bindings);
        Assert.Equal(telegramSupport.Id, binding.ChannelConnectionId);
        Assert.NotEqual(telegramOps.Id, binding.ChannelConnectionId);
    }

    [Fact]
    public async Task CreateAgent_deduplicates_channel_resource_and_init_bindings()
    {
        await using var db = WorkspaceTestHarness.CreateDb("agent-dashboard-channel-dedupe");
        var ownerId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        var harness = WorkspaceTestHarness.Create(db);
        var workspace = await harness.Workspaces.GetCurrentAsync(ownerId);
        var channelRepository = new ChannelRepository(db);
        var telegram = await channelRepository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Ops", ownerId, workspace.Id));

        var agent = await harness.AgentLifecycle.CreateAsync(
            new CreateAgentLifecycleRequest(
                Name: "Ops Agent",
                Provider: "openai",
                Model: "gpt-4o-mini",
                Prompt: null,
                ConfigJson: null,
                IntegrationSlugs: null,
                ChannelConnectionIds: [telegram.Id],
                ToolNames: null,
                Resources:
                [
                    new AgentResourceAttachmentRequest(
                        AgentResourceKinds.Channel,
                        telegram.Id,
                        AgentResourceAccessModes.ReadWrite,
                        null),
                ],
                BootstrapMessage: null),
            ownerId,
            workspace.Id);

        var binding = Assert.Single(await channelRepository.ListBindingsAsync(agent.Id));
        Assert.Equal(telegram.Id, binding.ChannelConnectionId);
    }

    [Fact]
    public async Task CreateAgent_rejects_channel_connection_ids_from_another_workspace()
    {
        await using var db = WorkspaceTestHarness.CreateDb("agent-dashboard-channel-scope");
        var ownerId = Guid.NewGuid();
        await WorkspaceTestHarness.SeedUserAsync(db, ownerId, "owner@example.com");
        var harness = WorkspaceTestHarness.Create(db);
        var workspace = await harness.Workspaces.GetCurrentAsync(ownerId);
        var otherWorkspace = await harness.Workspaces.CreateAsync(ownerId, "Other");
        var channelRepository = new ChannelRepository(db);
        var otherTelegram = await channelRepository.CreateConnectionAsync(
            ChannelConnectionRecord.Create(ChannelType.Telegram, "Other Telegram", ownerId, otherWorkspace.Id));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.AgentLifecycle.CreateAsync(
                new CreateAgentLifecycleRequest(
                    Name: "Ops Agent",
                    Provider: "openai",
                    Model: "gpt-4o-mini",
                    Prompt: null,
                    ConfigJson: null,
                    IntegrationSlugs: null,
                    ChannelConnectionIds: [otherTelegram.Id],
                    ToolNames: null,
                    Resources: null,
                    BootstrapMessage: null),
                ownerId,
                workspace.Id));

        var agents = await harness.Agents.ListAsync(new AgentFilter { WorkspaceId = workspace.Id });
        Assert.Empty(agents);
    }
}

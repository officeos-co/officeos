using OffceOs.Application.Features.Channels;
using OffceOs.Application.Features.ResourceLogs;
using OffceOs.Database;
using OffceOs.Database.Models;
using OffceOs.Domain.Features.AgentHarness;
using OffceOs.Domain.Features.Channels;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Agents;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Infrastructure.Features.ResourceLogs;

namespace OffceOs.Tests.Shared;

internal sealed class ChannelTestHarness : IAsyncDisposable
{
    private ChannelTestHarness(EaosDbContext db, IPublisher publisher, IResourceLogWriterService resourceLogWriterService)
    {
        Db = db;
        Publisher = publisher;
        Gateway = new RecordingChannelGateway();
        Service = new ChannelService(
            new ChannelRepository(db),
            Gateway,
            new AgentRepository(db),
            new ChannelCredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(
                Path.Combine(Path.GetTempPath(), $"eaos-channel-test-keys-{Guid.NewGuid():N}")))),
            publisher,
            new ChannelReplyContext(),
            resourceLogWriterService);
    }

    public EaosDbContext Db { get; }
    public IPublisher Publisher { get; }
    public RecordingChannelGateway Gateway { get; }
    public ChannelService Service { get; }
    public Guid OwnerId { get; } = Guid.NewGuid();
    public Guid WorkspaceId { get; } = Guid.NewGuid();

    public IReadOnlyList<object> Notifications => Publisher switch
    {
        RecordingPublisher publisher => publisher.Notifications,
        _ => [],
    };

    public IReadOnlyList<MessageReceivedEvent> MessageEvents =>
        Notifications.OfType<MessageReceivedEvent>().ToList();

    public static ChannelTestHarness Create(string namePrefix) =>
        new(TestDbFactory.Create(namePrefix), new RecordingPublisher(), new FakeResourceLogWriterService());

    public static ChannelTestHarness CreatePersisting(string namePrefix)
    {
        var db = TestDbFactory.Create(namePrefix);
        var resourceLogService = new ResourceLogService(new ResourceLogRepository(db), new FakeControlPlaneResourceCatalogService());
        return new ChannelTestHarness(db, new RecordingPublisher(), new FakeResourceLogWriterService(resourceLogService));
    }

    public void SeedAgents(params Guid[] agentIds)
    {
        Db.Agents.AddRange(agentIds.Select((agentId, index) => new AgentEntity
        {
            Id = agentId,
            Name = $"Agent {index}",
            Provider = "openai",
            Model = "gpt-4o-mini",
            Status = "running",
            CreatedAt = DateTime.UtcNow,
            OwnerId = OwnerId,
            WorkspaceId = WorkspaceId,
        }));
        Db.SaveChanges();
    }

    public async Task<ChannelConnectionRecord> CreateConnectionAsync(string channelType, string displayName) =>
        await Service.CreateConnectionAsync(channelType, displayName, null, OwnerId, WorkspaceId);

    public async Task BindAsync(Guid agentId, Guid connectionId, string? configJson) =>
        await Service.BindAgentAsync(agentId, connectionId, configJson);

    public void AssertNoAgentActivation(IReadOnlyList<Guid> notified, params Guid[] agentIds)
    {
        Assert.Empty(notified);
        Assert.Empty(MessageEvents);
    }

    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync();
    }
}

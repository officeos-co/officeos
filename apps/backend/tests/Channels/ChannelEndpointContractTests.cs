using OffceOs.Api.Features.Channels;
using OffceOs.Domain.Features.Channels;
using OffceOs.Infrastructure.Common.Security;
using OffceOs.Infrastructure.Features.Channels;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.Channels;

public sealed class ChannelEndpointContractTests
{
    [Fact]
    public async Task Active_endpoint_returns_multiple_active_connections_of_the_same_kind_with_distinct_ids()
    {
        await using var db = TestDbFactory.Create("channel-active-endpoint");
        var repository = new ChannelRepository(db);
        var keyRingPath = Path.Combine(Path.GetTempPath(), $"eaos-channel-test-keys-{Guid.NewGuid():N}");
        var protector = new ChannelCredentialProtector(DataProtectionProvider.Create(new DirectoryInfo(keyRingPath)));
        var ownerId = Guid.NewGuid();
        var workspaceId = Guid.NewGuid();
        var telegramOps = ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Ops", ownerId, workspaceId);
        telegramOps.EncryptedCreds = protector.Protect("""{"token":"ops"}""");
        var telegramSupport = ChannelConnectionRecord.Create(ChannelType.Telegram, "Telegram Support", ownerId, workspaceId);
        telegramSupport.EncryptedCreds = protector.Protect("""{"token":"support"}""");
        var disabledSlack = ChannelConnectionRecord.Create(ChannelType.Slack, "Slack Disabled", ownerId, workspaceId);
        disabledSlack.EncryptedCreds = protector.Protect("""{"token":"slack"}""");
        disabledSlack.Enabled = false;
        await repository.CreateConnectionAsync(telegramOps);
        await repository.CreateConnectionAsync(telegramSupport);
        await repository.CreateConnectionAsync(disabledSlack);

        var controller = new ChannelSidecarController();
        var result = await controller.Active(repository, protector, CancellationToken.None);

        var payloadJson = JsonSerializer.Serialize(GetResultValue(result));
        using var document = JsonDocument.Parse(payloadJson);
        var rows = document.RootElement.EnumerateArray().ToList();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, row => row.GetProperty("connectionId").GetString() == telegramOps.Id.ToString());
        Assert.Contains(rows, row => row.GetProperty("connectionId").GetString() == telegramSupport.Id.ToString());
        Assert.DoesNotContain(rows, row => row.GetProperty("channelType").GetString() == "slack");
    }

    [Fact]
    public async Task Inbound_endpoint_routes_by_connection_id_not_channel_type()
    {
        var connectionId = Guid.NewGuid();
        var service = new RecordingInboundChannelService([Guid.NewGuid(), Guid.NewGuid()]);
        var request = new ChannelInboundInput(
            ConnectionId: connectionId,
            SenderIdentifier: "sender",
            MessageText: "hello",
            IsGroupMessage: false,
            MessageId: "message",
            ChannelId: "chat");

        var controller = new ChannelSidecarController();
        var result = await controller.Inbound(request, service, CancellationToken.None);

        Assert.Equal(connectionId, service.ConnectionId);
        Assert.Equal("hello", service.MessageText);
        Assert.Contains(service.AgentIds[0].ToString(), JsonSerializer.Serialize(GetResultValue(result)));
    }

    private static object? GetResultValue(IActionResult result) =>
        (result as ObjectResult)?.Value;

    private sealed class RecordingInboundChannelService : IChannelService
    {
        public RecordingInboundChannelService(IReadOnlyList<Guid> agentIds)
        {
            AgentIds = agentIds;
        }

        public IReadOnlyList<Guid> AgentIds { get; }
        public Guid? ConnectionId { get; private set; }
        public string? MessageText { get; private set; }

        public Task<IReadOnlyList<Guid>> RouteInboundAsync(
            Guid connectionId,
            string senderIdentifier,
            string messageText,
            bool isGroupMessage,
            string? messageId,
            string? channelId,
            CancellationToken ct = default)
        {
            ConnectionId = connectionId;
            MessageText = messageText;
            return Task.FromResult(AgentIds);
        }

        public Task<IReadOnlyList<Guid>> SendInternalMessageAsync(Guid senderAgentId, Guid channelConnectionId, string content, CancellationToken ct = default) => throw new NotSupportedException();
        public Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SendTestMessageAsync(Guid connectionId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ChannelConnectionRecord> CreateConnectionAsync(string channelType, string displayName, string? configJson, Guid createdById, Guid workspaceId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ChannelConnectionRecord> UpdateConnectionAsync(Guid id, string? displayName, string? configJson, bool? enabled, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ChannelConnectionRecord> UpdateOwnedConnectionAsync(Guid id, Guid ownerId, Guid workspaceId, string? displayName, string? configJson, bool? enabled, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> DeleteOwnedConnectionAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SaveChannelCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AgentChannelBindingRecord>> ListBindingsForOwnedAgentAsync(Guid agentId, Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AgentChannelBindingRecord> BindAgentAsync(Guid agentId, Guid channelConnectionId, string? configJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AgentChannelBindingRecord> BindOwnedAgentAsync(Guid agentId, Guid channelConnectionId, Guid ownerId, Guid workspaceId, string? configJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<ChannelConnectionRecord> CreateOwnedInternalConnectionAsync(string displayName, IReadOnlyList<InternalChannelBindingRequest> bindings, Guid ownerId, Guid workspaceId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> UnbindAgentAsync(Guid agentId, Guid channelConnectionId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<bool> UnbindOwnedAgentAsync(Guid agentId, Guid channelConnectionId, Guid ownerId, Guid workspaceId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AgentChannelBindingRecord> UpdateBindingConfigAsync(Guid agentId, Guid channelConnectionId, string configJson, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<AgentChannelBindingRecord> UpdateOwnedBindingConfigAsync(Guid agentId, Guid channelConnectionId, Guid ownerId, Guid workspaceId, string configJson, CancellationToken ct = default) => throw new NotSupportedException();
    }
}

using OffceOs.Domain.Features.Channels;

namespace OffceOs.Tests.Shared;

public sealed class FakeChannelService : IChannelService
{
    public Task<IReadOnlyList<Guid>> RouteInboundAsync(
        Guid connectionId,
        string senderIdentifier,
        string messageText,
        bool isGroupMessage,
        string? messageId,
        string? channelId,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task<IReadOnlyList<Guid>> RouteInboundByChannelTypeAsync(
        string channelType,
        string senderIdentifier,
        string messageText,
        bool isGroupMessage,
        string? messageId,
        string? channelId,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default) => Task.CompletedTask;

    public Task SendTestMessageAsync(Guid connectionId, CancellationToken ct = default) => Task.CompletedTask;

    public Task<ChannelConnectionRecord> CreateConnectionAsync(
        string channelType,
        string displayName,
        string? configJson,
        Guid createdById,
        Guid workspaceId,
        CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<ChannelConnectionRecord> UpdateConnectionAsync(Guid id, string? displayName, bool? enabled, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<ChannelConnectionRecord> UpdateOwnedConnectionAsync(
        Guid id,
        Guid ownerId,
        Guid workspaceId,
        string? displayName,
        bool? enabled,
        CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);

    public Task<bool> DeleteOwnedConnectionAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default) =>
        Task.FromResult(false);

    public Task SaveChannelCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<IReadOnlyList<AgentChannelBindingRecord>> ListBindingsForOwnedAgentAsync(
        Guid agentId,
        Guid ownerId,
        Guid? workspaceId = null,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentChannelBindingRecord>>([]);

    public Task<AgentChannelBindingRecord> BindAgentAsync(
        Guid agentId,
        Guid channelConnectionId,
        string? configJson,
        CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<bool> UnbindAgentAsync(Guid agentId, Guid channelConnectionId, CancellationToken ct = default) =>
        Task.FromResult(false);

    public Task<AgentChannelBindingRecord> UpdateBindingConfigAsync(
        Guid agentId,
        Guid channelConnectionId,
        string configJson,
        CancellationToken ct = default) =>
        throw new NotSupportedException();
}

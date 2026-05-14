namespace OffceOs.Domain.Features.Channels;

/// <summary>
/// Proxy to the channel sidecar. Runs in the same K8s pod on localhost.
/// </summary>
public interface IChannelGateway
{
    Task SendAsync(Guid connectionId, string channelType, string platformId, string? threadId,
                   ChannelMessage message, CancellationToken ct = default);
    Task ReloadAsync(CancellationToken ct = default);
}

/// <summary>
/// Application-level channel orchestration. Backend owns bindings + metadata,
/// delegates platform delivery to the channel sidecar via IChannelGateway.
/// </summary>
public interface IChannelService
{
    Task<IReadOnlyList<Guid>> RouteInboundAsync(Guid connectionId, string senderIdentifier, string messageText, bool isGroupMessage, string? messageId, string? channelId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> SendInternalMessageAsync(Guid senderAgentId, Guid channelConnectionId, string content, CancellationToken ct = default);
    Task BroadcastAsync(Guid agentId, string text, CancellationToken ct = default);
    Task SendTestMessageAsync(Guid connectionId, CancellationToken ct = default);
    Task<ChannelConnectionRecord> CreateConnectionAsync(string channelType, string displayName, string? configJson, Guid createdById, Guid workspaceId, CancellationToken ct = default);
    Task<ChannelConnectionRecord> UpdateConnectionAsync(Guid id, string? displayName, string? configJson, bool? enabled, CancellationToken ct = default);
    Task<ChannelConnectionRecord> UpdateOwnedConnectionAsync(Guid id, Guid ownerId, Guid workspaceId, string? displayName, string? configJson, bool? enabled, CancellationToken ct = default);
    Task<bool> DeleteConnectionAsync(Guid id, CancellationToken ct = default);
    Task<bool> DeleteOwnedConnectionAsync(Guid id, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task SaveChannelCredsAsync(Guid connectionId, string credsJson, CancellationToken ct = default);
    Task<IReadOnlyList<AgentChannelBindingRecord>> ListBindingsForOwnedAgentAsync(Guid agentId, Guid ownerId, Guid? workspaceId = null, CancellationToken ct = default);
    Task<AgentChannelBindingRecord> BindAgentAsync(Guid agentId, Guid channelConnectionId, string? configJson, CancellationToken ct = default);
    Task<AgentChannelBindingRecord> BindOwnedAgentAsync(Guid agentId, Guid channelConnectionId, Guid ownerId, Guid workspaceId, string? configJson, CancellationToken ct = default);
    Task<ChannelConnectionRecord> CreateOwnedInternalConnectionAsync(string displayName, IReadOnlyList<InternalChannelBindingRequest> bindings, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<bool> UnbindAgentAsync(Guid agentId, Guid channelConnectionId, CancellationToken ct = default);
    Task<bool> UnbindOwnedAgentAsync(Guid agentId, Guid channelConnectionId, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<AgentChannelBindingRecord> UpdateBindingConfigAsync(Guid agentId, Guid channelConnectionId, string configJson, CancellationToken ct = default);
    Task<AgentChannelBindingRecord> UpdateOwnedBindingConfigAsync(Guid agentId, Guid channelConnectionId, Guid ownerId, Guid workspaceId, string configJson, CancellationToken ct = default);
}

public sealed record InternalChannelBindingRequest(
    Guid AgentId,
    bool CanSend,
    bool CanReceive,
    bool ReplyOnly,
    string? Label);

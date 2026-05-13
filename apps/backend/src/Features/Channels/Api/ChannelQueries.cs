namespace OffceOs.Api.Features.Channels;

[ExtendObjectType(typeof(GraphQLQueries))]
public class ChannelQueries
{
    [GraphQLDescription("Lists all channel connections configured by the user.")]
    public async Task<IReadOnlyList<ChannelConnectionPayload>> GetChannelConnections(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelRepository repo,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var rows = await repo.ListConnectionsAsync(new ChannelConnectionFilter { WorkspaceId = workspace.Id }, ct);
        var lastMessages = await logs.GetLastRelevantMessagesForChannelConnectionsAsync(
            rows.Select(row => row.Id).ToList(),
            workspace.Id,
            ct);

        return rows
            .Select(row => ChannelGraphQLMapper.ToPayload(
                row,
                lastMessages.TryGetValue(row.Id, out var lastMessage) ? lastMessage : null))
            .ToList();
    }

    [GraphQLDescription("Returns a single channel connection by ID.")]
    public async Task<ChannelConnectionPayload?> GetChannelConnection(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelRepository repo,
        [Service] IAgentLogService logs,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var row = await repo.GetConnectionByAsync(new ChannelConnectionFilter { Id = id, WorkspaceId = workspace.Id }, ct);
        if (row is null) return null;
        var lastMessage = await logs.GetLastRelevantMessageForChannelConnectionAsync(row.Id, workspace.Id, ct);
        return ChannelGraphQLMapper.ToPayload(row, lastMessage);
    }

    [GraphQLDescription("Returns all supported channel types with display names, descriptions, logos, and onboarding step definitions.")]
    public IReadOnlyList<ChannelTypeDefinition> GetChannelKinds()
    {
        return ChannelKinds.All;
    }

    [GraphQLDescription("Lists all channel bindings for a specific agent showing which channels the agent listens on.")]
    public async Task<IReadOnlyList<AgentChannelBindingPayload>> GetAgentChannelBindings(
        Guid agentId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelService channels,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var rows = await channels.ListBindingsForOwnedAgentAsync(agentId, user.Id, workspace.Id, ct);
        return rows.Select(ChannelGraphQLMapper.ToPayload).ToList();
    }
}

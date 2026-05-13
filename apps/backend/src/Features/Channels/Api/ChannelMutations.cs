namespace OffceOs.Api.Features.Channels;

[ExtendObjectType(typeof(GraphQLMutations))]
public class ChannelMutations
{
    private static async Task InvalidateChannelCachesAsync(
        IDistributedCache cache,
        Guid userId,
        Guid workspaceId,
        Guid? connectionId,
        CancellationToken ct)
    {
        await cache.RemoveAsync($"channels:list:{userId}:workspace:{workspaceId}", ct);
        if (connectionId.HasValue)
            await cache.RemoveAsync($"channels:{connectionId.Value}:user:{userId}:workspace:{workspaceId}", ct);
    }

    [GraphQLDescription("Creates a new channel connection (e.g. Slack bot, Telegram bot). ConfigJson contains the encrypted credentials payload.")]
    public async Task<ChannelConnectionPayload> CreateChannelConnection(
        CreateChannelConnectionInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var created = await channelService.CreateConnectionAsync(
                input.ChannelType, input.DisplayName, input.ConfigJson,
                user.Id, workspace.Id, ct);

            await InvalidateChannelCachesAsync(cache, user.Id, workspace.Id, null, ct);
            return ChannelGraphQLMapper.ToPayload(created);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    [GraphQLDescription("Creates an internal agent channel with directed send/receive bindings.")]
    public async Task<ChannelConnectionPayload> CreateInternalChannelConnection(
        CreateInternalChannelConnectionInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var created = await channelService.CreateOwnedInternalConnectionAsync(
                input.DisplayName,
                ChannelGraphQLMapper.ToRequests(input.Bindings),
                user.Id,
                workspace.Id,
                ct);

            await InvalidateChannelCachesAsync(cache, user.Id, workspace.Id, null, ct);
            return ChannelGraphQLMapper.ToPayload(created);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    [GraphQLDescription("Updates display name and/or enabled status of an existing channel connection.")]
    public async Task<ChannelConnectionPayload> UpdateChannelConnection(
        Guid id,
        UpdateChannelConnectionInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var updated = await channelService.UpdateOwnedConnectionAsync(
                id, user.Id, workspace.Id, input.DisplayName, input.ConfigJson, input.Enabled, ct);

            await InvalidateChannelCachesAsync(cache, user.Id, workspace.Id, id, ct);
            return ChannelGraphQLMapper.ToPayload(updated);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("NOT_FOUND").Build());
        }
    }

    [GraphQLDescription("Permanently deletes a channel connection and all its agent bindings.")]
    public async Task<bool> DeleteChannelConnection(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var result = await channelService.DeleteOwnedConnectionAsync(id, user.Id, workspace.Id, ct);
            await InvalidateChannelCachesAsync(cache, user.Id, workspace.Id, id, ct);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("NOT_FOUND").Build());
        }
    }

    [GraphQLDescription("Binds a channel connection to an agent so it receives messages from that channel. Optional config specifies platform/thread IDs.")]
    public async Task<AgentChannelBindingPayload> BindChannelToAgent(
        Guid agentId,
        Guid channelConnectionId,
        ChannelBindingConfigInput? config,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var binding = await channelService.BindOwnedAgentAsync(
                agentId,
                channelConnectionId,
                user.Id,
                workspace.Id,
                config is null ? null : ChannelGraphQLMapper.SerializeConfig(config),
                ct);

            await cache.RemoveAsync(AgentCacheKeys.DashboardDetail(agentId, user.Id, workspace.Id), ct);
            return ChannelGraphQLMapper.ToPayload(binding);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("NOT_FOUND").Build());
        }
    }

    [GraphQLDescription("Removes a channel binding from an agent.")]
    public async Task<bool> UnbindChannelFromAgent(
        Guid agentId,
        Guid channelConnectionId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var result = await channelService.UnbindOwnedAgentAsync(
                agentId, channelConnectionId, user.Id, workspace.Id, ct);

            await cache.RemoveAsync(AgentCacheKeys.DashboardDetail(agentId, user.Id, workspace.Id), ct);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("NOT_FOUND").Build());
        }
    }

    [GraphQLDescription("Updates the routing config (platformId, threadId) on an existing agent-channel binding.")]
    public async Task<AgentChannelBindingPayload> UpdateChannelBindingConfig(
        Guid agentId,
        Guid channelConnectionId,
        ChannelBindingConfigInput config,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IChannelService channelService,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var binding = await channelService.UpdateOwnedBindingConfigAsync(
                agentId,
                channelConnectionId,
                user.Id,
                workspace.Id,
                ChannelGraphQLMapper.SerializeConfig(config),
                ct);

            return ChannelGraphQLMapper.ToPayload(binding);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("NOT_FOUND").Build());
        }
    }
}

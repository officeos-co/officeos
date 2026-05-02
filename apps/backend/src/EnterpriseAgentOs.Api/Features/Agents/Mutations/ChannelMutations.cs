namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class ChannelMutations
{
    private const string ChannelListCacheKey = "channels:list";

    private static async Task InvalidateChannelCachesAsync(
        IDistributedCache cache,
        Guid? connectionId,
        CancellationToken ct)
    {
        await cache.RemoveAsync(ChannelListCacheKey, ct);
        if (connectionId.HasValue)
            await cache.RemoveAsync($"channels:{connectionId.Value}", ct);
    }

    [GraphQLDescription("Creates a new channel connection (e.g. Slack bot, Telegram bot). ConfigJson contains the encrypted credentials payload.")]
    public async Task<ChannelConnectionGqlDto> CreateChannelConnection(
        CreateChannelConnectionInput input,
        IResolverContext context,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);

        try
        {
            var created = await channelService.CreateConnectionAsync(
                input.ChannelType, input.DisplayName, input.ConfigJson,
                user.Id, ct);

            await InvalidateChannelCachesAsync(cache, null, ct);
            return ChannelGraphQLMapper.ToDto(created);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    [GraphQLDescription("Updates display name and/or enabled status of an existing channel connection.")]
    public async Task<ChannelConnectionGqlDto> UpdateChannelConnection(
        Guid id,
        UpdateChannelConnectionInput input,
        IResolverContext context,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        try
        {
            var updated = await channelService.UpdateConnectionAsync(
                id, input.DisplayName, input.Enabled, ct);

            await InvalidateChannelCachesAsync(cache, id, ct);
            return ChannelGraphQLMapper.ToDto(updated);
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
        IResolverContext context,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var result = await channelService.DeleteConnectionAsync(id, ct);
        await InvalidateChannelCachesAsync(cache, id, ct);
        return result;
    }

    [GraphQLDescription("Binds a channel connection to an agent so it receives messages from that channel. Optional config specifies platform/thread IDs.")]
    public async Task<AgentChannelBindingGqlDto> BindChannelToAgent(
        Guid agentId,
        Guid channelConnectionId,
        ChannelBindingConfigInput? config,
        IResolverContext context,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        try
        {
            var configJson = config is null ? null : ChannelGraphQLMapper.SerializeConfig(config);
            var created = await channelService.BindAgentAsync(agentId, channelConnectionId, configJson, ct);
            await cache.RemoveAsync($"agents:dashboard:{agentId}", ct);
            return ChannelGraphQLMapper.ToDto(created);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("This channel is already bound to the agent.")
                    .SetCode("DUPLICATE_BINDING")
                    .Build());
        }
    }

    [GraphQLDescription("Removes a channel binding from an agent.")]
    public async Task<bool> UnbindChannelFromAgent(
        Guid agentId,
        Guid channelConnectionId,
        IResolverContext context,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var result = await channelService.UnbindAgentAsync(agentId, channelConnectionId, ct);
        await cache.RemoveAsync($"agents:dashboard:{agentId}", ct);
        return result;
    }

    [GraphQLDescription("Updates the routing config (platformId, threadId) on an existing agent-channel binding.")]
    public async Task<AgentChannelBindingGqlDto> UpdateChannelBindingConfig(
        Guid agentId,
        Guid channelConnectionId,
        ChannelBindingConfigInput config,
        IResolverContext context,
        [Service] IChannelService channelService,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        try
        {
            var configJson = ChannelGraphQLMapper.SerializeConfig(config);
            var updated = await channelService.UpdateBindingConfigAsync(agentId, channelConnectionId, configJson, ct);
            return ChannelGraphQLMapper.ToDto(updated);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode("NOT_FOUND")
                    .Build());
        }
    }
}

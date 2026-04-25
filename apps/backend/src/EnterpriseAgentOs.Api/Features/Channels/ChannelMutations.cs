namespace EnterpriseAgentOs.Api.Features.Channels;

[ExtendObjectType(typeof(GraphQLMutations))]
public class ChannelMutations
{
    private const string ChannelListCacheKey = "channels:list";

    private static void InvalidateChannelCaches(IMemoryCache cache, Guid? connectionId = null)
    {
        cache.Remove(ChannelListCacheKey);
        if (connectionId.HasValue)
            cache.Remove($"channels:{connectionId.Value}");
    }

    public async Task<ChannelConnectionGqlDto> CreateChannelConnection(
        CreateChannelConnectionInput input,
        IResolverContext context,
        [Service] IChannelService channelService,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);

        try
        {
            var created = await channelService.CreateConnectionAsync(
                input.ChannelType, input.DisplayName, input.ConfigJson,
                user.Id, ct);

            InvalidateChannelCaches(cache);
            return ChannelGraphQLMapper.ToDto(created);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("BAD_INPUT").Build());
        }
    }

    public async Task<ChannelConnectionGqlDto> UpdateChannelConnection(
        Guid id,
        UpdateChannelConnectionInput input,
        IResolverContext context,
        [Service] IChannelService channelService,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        try
        {
            var updated = await channelService.UpdateConnectionAsync(
                id, input.DisplayName, input.Enabled, ct);

            InvalidateChannelCaches(cache, id);
            return ChannelGraphQLMapper.ToDto(updated);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("NOT_FOUND").Build());
        }
    }

    public async Task<bool> DeleteChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] IChannelService channelService,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var result = await channelService.DeleteConnectionAsync(id, ct);
        InvalidateChannelCaches(cache, id);
        return result;
    }

    public async Task<AgentChannelBindingGqlDto> BindChannelToAgent(
        Guid agentId,
        Guid channelConnectionId,
        ChannelBindingConfigInput? config,
        IResolverContext context,
        [Service] IChannelService channelService,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        try
        {
            var configJson = config is null ? null : ChannelGraphQLMapper.SerializeConfig(config);
            var created = await channelService.BindAgentAsync(agentId, channelConnectionId, configJson, ct);
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
    }

    public async Task<bool> UnbindChannelFromAgent(
        Guid agentId,
        Guid channelConnectionId,
        IResolverContext context,
        [Service] IChannelService channelService,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await channelService.UnbindAgentAsync(agentId, channelConnectionId, ct);
    }

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

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
                input.DefaultChannelId, user.Id, ct);

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
                id, input.DisplayName, input.Enabled, input.ConfigJson, ct);

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
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var connection = await repo.GetConnectionAsync(channelConnectionId, ct);
        if (connection is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Channel connection not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }

        var record = new AgentChannelBindingRecord
        {
            AgentId = agentId,
            ChannelConnectionId = channelConnectionId,
            Config = config is null ? null : ChannelGraphQLMapper.SerializeConfig(config),
        };

        var created = await repo.CreateBindingAsync(record, ct);
        return ChannelGraphQLMapper.ToDto(created);
    }

    public async Task<bool> UnbindChannelFromAgent(
        Guid agentId,
        Guid channelConnectionId,
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var bindings = await repo.ListBindingsAsync(agentId, ct);
        var match = bindings.FirstOrDefault(b => b.ChannelConnectionId == channelConnectionId);
        if (match is null) return false;
        return await repo.DeleteBindingAsync(match.Id, ct);
    }

    public async Task<AgentChannelBindingGqlDto> UpdateChannelBindingConfig(
        Guid agentId,
        Guid channelConnectionId,
        ChannelBindingConfigInput config,
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var bindings = await repo.ListBindingsAsync(agentId, ct);
        var match = bindings.FirstOrDefault(b => b.ChannelConnectionId == channelConnectionId);
        if (match is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Binding not found for agent + channel connection.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }

        var updated = await repo.UpdateBindingAsync(match.Id, row =>
        {
            if (row.AgentId != agentId) return;
            row.Config = ChannelGraphQLMapper.SerializeConfig(config);
        }, ct);

        if (updated is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Binding not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return ChannelGraphQLMapper.ToDto(updated);
    }
}

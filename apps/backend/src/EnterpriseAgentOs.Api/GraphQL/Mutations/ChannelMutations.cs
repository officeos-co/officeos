namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

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

    public async Task<Types.ChannelConnectionGqlDto> CreateChannelConnection(
        Types.CreateChannelConnectionInput input,
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] ChannelConfigProtector protector,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);

        if (ChannelTypes.GetByType(input.ChannelType) is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Unknown channel type: {input.ChannelType}")
                    .SetCode("BAD_INPUT")
                    .Build());
        }

        string? encrypted = null;
        if (!string.IsNullOrWhiteSpace(input.ConfigJson))
        {
            encrypted = protector.Protect(input.ConfigJson);
        }

        var record = new ChannelConnectionRecord
        {
            ChannelType = input.ChannelType.ToLowerInvariant(),
            DisplayName = input.DisplayName,
            EncryptedConfig = encrypted,
            CreatedById = user.Id,
        };

        var created = await repo.CreateConnectionAsync(record, ct);
        InvalidateChannelCaches(cache);
        return Types.ChannelGraphQLMapper.ToDto(created);
    }

    public async Task<Types.ChannelConnectionGqlDto> UpdateChannelConnection(
        Guid id,
        Types.UpdateChannelConnectionInput input,
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] ChannelConfigProtector protector,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);

        var updated = await repo.UpdateConnectionAsync(id, row =>
        {
            if (input.DisplayName is not null)
                row.DisplayName = input.DisplayName;
            if (input.Enabled.HasValue)
                row.Enabled = input.Enabled.Value;
            if (!string.IsNullOrWhiteSpace(input.ConfigJson))
                row.EncryptedConfig = protector.Protect(input.ConfigJson);
        }, ct);

        if (updated is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Channel connection '{id}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        InvalidateChannelCaches(cache, id);
        return Types.ChannelGraphQLMapper.ToDto(updated);
    }

    public async Task<bool> DeleteChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var result = await repo.DeleteConnectionAsync(id, ct);
        InvalidateChannelCaches(cache, id);
        return result;
    }

    public async Task<Types.AgentChannelBindingGqlDto> BindChannelToAgent(
        Guid agentId,
        Guid channelConnectionId,
        Types.ChannelBindingConfigInput? config,
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);

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
            Config = config is null ? null : Types.ChannelGraphQLMapper.SerializeConfig(config),
        };

        var created = await repo.CreateBindingAsync(record, ct);
        return Types.ChannelGraphQLMapper.ToDto(created);
    }

    public async Task<bool> UnbindChannelFromAgent(
        Guid agentId,
        Guid channelConnectionId,
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);

        var bindings = await repo.ListBindingsAsync(agentId, ct);
        var match = bindings.FirstOrDefault(b => b.ChannelConnectionId == channelConnectionId);
        if (match is null) return false;
        return await repo.DeleteBindingAsync(match.Id, ct);
    }

    public async Task<Types.AgentChannelBindingGqlDto> UpdateChannelBindingConfig(
        Guid agentId,
        Guid channelConnectionId,
        Types.ChannelBindingConfigInput config,
        IResolverContext context,
        [Service] IChannelRepository repo,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);

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
            row.Config = Types.ChannelGraphQLMapper.SerializeConfig(config);
        }, ct);

        if (updated is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Binding not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return Types.ChannelGraphQLMapper.ToDto(updated);
    }
}

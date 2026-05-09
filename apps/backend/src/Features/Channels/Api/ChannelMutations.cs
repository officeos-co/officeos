namespace EnterpriseAgentOs.Api.Features.Channels;

[ExtendObjectType(typeof(GraphQLMutations))]
public class ChannelMutations
{
    private static async Task InvalidateChannelCachesAsync(
        IDistributedCache cache,
        Guid userId,
        Guid? connectionId,
        CancellationToken ct)
    {
        await cache.RemoveAsync($"channels:list:{userId}", ct);
        if (connectionId.HasValue)
            await cache.RemoveAsync($"channels:{connectionId.Value}:user:{userId}", ct);
    }

    [GraphQLDescription("Creates a new channel connection (e.g. Slack bot, Telegram bot). ConfigJson contains the encrypted credentials payload.")]
    public async Task<ChannelConnectionGqlDto> CreateChannelConnection(
        CreateChannelConnectionInput input,
        [Service] UserContext user,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var created = await channelService.CreateConnectionAsync(
                input.ChannelType, input.DisplayName, input.ConfigJson,
                user.Id, ct);

            await InvalidateChannelCachesAsync(cache, user.Id, null, ct);
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
        [Service] UserContext user,
        [Service] IChannelService channelService,
        [Service] IChannelRepository channelRepository,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            await EnsureOwnedChannelConnectionAsync(channelRepository, id, user.Id, ct);
            var updated = await channelService.UpdateConnectionAsync(
                id, input.DisplayName, input.Enabled, ct);

            await InvalidateChannelCachesAsync(cache, user.Id, id, ct);
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
        [Service] UserContext user,
        [Service] IChannelService channelService,
        [Service] IChannelRepository channelRepository,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        await EnsureOwnedChannelConnectionAsync(channelRepository, id, user.Id, ct);
        var result = await channelService.DeleteConnectionAsync(id, ct);
        await InvalidateChannelCachesAsync(cache, user.Id, id, ct);
        return result;
    }

    [GraphQLDescription("Binds a channel connection to an agent so it receives messages from that channel. Optional config specifies platform/thread IDs.")]
    public Task<AgentChannelBindingGqlDto> BindChannelToAgent(
        Guid agentId,
        Guid channelConnectionId,
        ChannelBindingConfigInput? config,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        _ = channelService;
        _ = cache;
        _ = ct;
        throw ImmutableChannelBindingError();
    }

    [GraphQLDescription("Removes a channel binding from an agent.")]
    public Task<bool> UnbindChannelFromAgent(
        Guid agentId,
        Guid channelConnectionId,
        [Service] IChannelService channelService,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        _ = channelService;
        _ = cache;
        _ = ct;
        throw ImmutableChannelBindingError();
    }

    [GraphQLDescription("Updates the routing config (platformId, threadId) on an existing agent-channel binding.")]
    public Task<AgentChannelBindingGqlDto> UpdateChannelBindingConfig(
        Guid agentId,
        Guid channelConnectionId,
        ChannelBindingConfigInput config,
        [Service] IChannelService channelService,
        CancellationToken ct)
    {
        _ = channelService;
        _ = ct;
        throw ImmutableChannelBindingError();
    }

    private static GraphQLException ImmutableChannelBindingError() =>
        new(ErrorBuilder.New()
            .SetMessage("Agent channels are immutable after agent creation. Create a new agent with the desired channels.")
            .SetCode("IMMUTABLE_AGENT_CAPABILITIES")
            .Build());

    private static async Task EnsureOwnedChannelConnectionAsync(
        IChannelRepository channelRepository,
        Guid id,
        Guid userId,
        CancellationToken ct)
    {
        var connection = await channelRepository.GetConnectionByAsync(new ChannelConnectionFilter
        {
            Id = id,
            CreatedById = userId,
        }, ct);

        if (connection is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Channel connection not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
    }
}

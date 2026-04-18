namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class ChannelMutations
{
    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.ChannelConnectionGqlDto> CreateChannelConnection(
        EnterpriseAgentOs.Api.GraphQL.Types.CreateChannelConnectionInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository repo,
        [Service] EnterpriseAgentOs.Infrastructure.Security.ChannelConfigProtector protector,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);

        if (EnterpriseAgentOs.Domain.DTOs.Channels.ChannelTypes.GetByType(input.ChannelType) is null)
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

        var record = new EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord
        {
            ChannelType = input.ChannelType.ToLowerInvariant(),
            DisplayName = input.DisplayName,
            EncryptedConfig = encrypted,
            CreatedById = user.Id,
        };

        var created = await repo.CreateConnectionAsync(record, ct);
        return EnterpriseAgentOs.Api.GraphQL.Types.ChannelGraphQLMapper.ToDto(created);
    }

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.ChannelConnectionGqlDto> UpdateChannelConnection(
        Guid id,
        EnterpriseAgentOs.Api.GraphQL.Types.UpdateChannelConnectionInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository repo,
        [Service] EnterpriseAgentOs.Infrastructure.Security.ChannelConfigProtector protector,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);

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
        return EnterpriseAgentOs.Api.GraphQL.Types.ChannelGraphQLMapper.ToDto(updated);
    }

    public async Task<bool> DeleteChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await repo.DeleteConnectionAsync(id, ct);
    }

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.AgentChannelBindingGqlDto> BindChannelToAgent(
        Guid agentId,
        Guid channelConnectionId,
        EnterpriseAgentOs.Api.GraphQL.Types.ChannelBindingConfigInput? config,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);

        var connection = await repo.GetConnectionAsync(channelConnectionId, ct);
        if (connection is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Channel connection not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }

        var record = new EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord
        {
            AgentId = agentId,
            ChannelConnectionId = channelConnectionId,
            Config = config is null ? null : EnterpriseAgentOs.Api.GraphQL.Types.ChannelGraphQLMapper.SerializeConfig(config),
        };

        var created = await repo.CreateBindingAsync(record, ct);
        return EnterpriseAgentOs.Api.GraphQL.Types.ChannelGraphQLMapper.ToDto(created);
    }

    public async Task<bool> UnbindChannelFromAgent(
        Guid agentId,
        Guid channelConnectionId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);

        var bindings = await repo.ListBindingsAsync(agentId, ct);
        var match = bindings.FirstOrDefault(b => b.ChannelConnectionId == channelConnectionId);
        if (match is null) return false;
        return await repo.DeleteBindingAsync(match.Id, ct);
    }

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.AgentChannelBindingGqlDto> UpdateChannelBindingConfig(
        Guid agentId,
        Guid channelConnectionId,
        EnterpriseAgentOs.Api.GraphQL.Types.ChannelBindingConfigInput config,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository repo,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);

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
            row.Config = EnterpriseAgentOs.Api.GraphQL.Types.ChannelGraphQLMapper.SerializeConfig(config);
        }, ct);

        if (updated is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Binding not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return EnterpriseAgentOs.Api.GraphQL.Types.ChannelGraphQLMapper.ToDto(updated);
    }
}

namespace EnterpriseAgentOs.Api.Channels;

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
        [Service] IChannelRepository repo,
        [Service] ChannelConfigProtector protector,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);

        if (ChannelTypes.GetByType(input.ChannelType) is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Unknown channel type: {input.ChannelType}")
                    .SetCode("BAD_INPUT")
                    .Build());
        }

        string? encrypted = null;
        if (!string.IsNullOrWhiteSpace(input.ConfigJson)
            && input.ConfigJson.Trim() != "{}")
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

        // For WhatsApp, start the sidecar connection (QR code pairing)
        if (string.Equals(input.ChannelType, "whatsapp", StringComparison.OrdinalIgnoreCase))
        {
            var gateway = context.Services.GetRequiredService<WhatsAppGatewayService>();
            await gateway.StartConnectionAsync(created.Id);
        }

        return ChannelGraphQLMapper.ToDto(created);
    }

    public async Task<ChannelConnectionGqlDto> UpdateChannelConnection(
        Guid id,
        UpdateChannelConnectionInput input,
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] ChannelConfigProtector protector,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

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
        return ChannelGraphQLMapper.ToDto(updated);
    }

    public async Task<bool> DeleteChannelConnection(
        Guid id,
        IResolverContext context,
        [Service] IChannelRepository repo,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        // Stop WhatsApp connection if applicable
        var existing = await repo.GetConnectionAsync(id, ct);
        if (existing is not null && string.Equals(existing.ChannelType, "whatsapp", StringComparison.OrdinalIgnoreCase))
        {
            var gateway = context.Services.GetRequiredService<WhatsAppGatewayService>();
            await gateway.StopConnectionAsync(id);
        }

        var result = await repo.DeleteConnectionAsync(id, ct);
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

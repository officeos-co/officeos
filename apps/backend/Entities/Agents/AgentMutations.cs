namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class AgentMutations
{
    public async Task<EnterpriseAgentOs.Api.Entities.Agents.AgentDto> CreateAgent(
        EnterpriseAgentOs.Api.Entities.Agents.Types.CreateAgentInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Agents.IAgentService agents,
        [Service] EnterpriseAgentOs.Api.Entities.AgentSkills.IAgentSkillRepository agentSkills,
        [Service] EnterpriseAgentOs.Api.Entities.Channels.IChannelRepository channels,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        EnterpriseAgentOs.Api.Entities.Agents.AgentDto dto;
        try
        {
            dto = await agents.CreateAsync(
                new EnterpriseAgentOs.Api.Entities.Agents.CreateAgentRequest(input.Name, input.Provider, input.Model, input.Prompt),
                ownerId: user.Id,
                ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode("VALIDATION")
                    .Build());
        }

        if (input.IntegrationSlugs is { Count: > 0 })
        {
            await agentSkills.AssignAsync(dto.Id, input.IntegrationSlugs, ct);
        }

        if (input.ChannelSlugs is { Count: > 0 })
        {
            var connections = await channels.ListConnectionsAsync(ct);
            foreach (var slug in input.ChannelSlugs)
            {
                var match = connections.FirstOrDefault(c =>
                    string.Equals(c.ChannelType, slug, StringComparison.OrdinalIgnoreCase));
                if (match is null) continue; // silently skip
                try
                {
                    await channels.CreateBindingAsync(new EnterpriseAgentOs.Api.Database.Models.AgentChannelBindingRecord
                    {
                        AgentId = dto.Id,
                        ChannelConnectionId = match.Id,
                    }, ct);
                }
                catch (DbUpdateException)
                {
                    // already bound — skip
                }
            }
        }

        return dto;
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Agents.AgentDto> UpdateAgent(
        Guid id,
        EnterpriseAgentOs.Api.Entities.Agents.Types.UpdateAgentInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Agents.IAgentService agents,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var dto = await agents.PatchAsync(
            id,
            new EnterpriseAgentOs.Api.Entities.Agents.PatchAgentRequest(input.Provider, input.Model, input.Name, input.Prompt),
            ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Agent '{id}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return dto;
    }

    public async Task<bool> DeleteAgent(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Agents.IAgentService agents,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await agents.DeleteAsync(id, ct);
    }
}

using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentMutations
{
    public async Task<AgentDto> CreateAgent(
        CreateAgentInput input,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IAgentSkillRepository agentSkills,
        [Service] IChannelRepository channels,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var dto = await agents.CreateAsync(
            new CreateAgentRequest(input.Name, input.Provider, input.Model, input.Prompt),
            ownerId: user.Id,
            ct);

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
                    await channels.CreateBindingAsync(new AgentChannelBindingRecord
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

    public async Task<AgentDto> UpdateAgent(
        Guid id,
        UpdateAgentInput input,
        IResolverContext context,
        [Service] IAgentService agents,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var dto = await agents.PatchAsync(
            id,
            new PatchAgentRequest(input.Provider, input.Model, input.Name, input.Prompt),
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
        [Service] IAgentService agents,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await agents.DeleteAsync(id, ct);
    }
}

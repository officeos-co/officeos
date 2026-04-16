using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Entities.Agents.GraphQL;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentMutations
{
    public async Task<AgentDto> CreateAgent(
        CreateAgentInput input,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IAgentSkillRepository agentSkills,
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

        // TODO Stage 5b: bind agent to channels (input.ChannelSlugs) once
        // IChannelRepository exposes a slug-based BindAsync(agentId, slug) helper.
        // Current IChannelRepository only supports binding via ChannelConnectionId.

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

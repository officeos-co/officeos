using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Entities.AgentTemplates.GraphQL;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentTemplateQueries
{
    public Task<IReadOnlyList<AgentTemplateDto>> GetAgentTemplates(
        IResolverContext context,
        [Service] IAgentTemplateService templates,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return templates.ListAsync(ct);
    }
}

namespace EnterpriseAgentOs.Api.GraphQL;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentTemplateQueries
{
    public Task<IReadOnlyList<AgentTemplateDto>> GetAgentTemplates(
        IResolverContext context,
        [Service] IAgentTemplateService templates,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return templates.ListAsync(ct);
    }
}

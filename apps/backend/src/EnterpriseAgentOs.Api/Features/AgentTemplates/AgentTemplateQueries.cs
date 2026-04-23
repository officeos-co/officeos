namespace EnterpriseAgentOs.Api.Features.AgentTemplates;

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

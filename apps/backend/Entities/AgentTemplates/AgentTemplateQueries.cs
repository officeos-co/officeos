namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AgentTemplateQueries
{
    public Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.AgentTemplates.AgentTemplateDto>> GetAgentTemplates(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.AgentTemplates.IAgentTemplateService templates,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return templates.ListAsync(ct);
    }
}

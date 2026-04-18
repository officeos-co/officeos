namespace EnterpriseAgentOs.Api.GraphQL.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AgentTemplateQueries
{
    public Task<IReadOnlyList<EnterpriseAgentOs.Domain.DTOs.AgentTemplates.AgentTemplateDto>> GetAgentTemplates(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.AgentTemplates.IAgentTemplateService templates,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return templates.ListAsync(ct);
    }
}

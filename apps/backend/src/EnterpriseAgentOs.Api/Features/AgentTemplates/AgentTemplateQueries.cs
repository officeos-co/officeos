namespace EnterpriseAgentOs.Api.Features.AgentTemplates;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentTemplateQueries
{
    [GraphQLDescription("Lists all available agent templates (built-in and user-created) for the create-agent-from-template flow.")]
    public Task<IReadOnlyList<AgentTemplateDto>> GetAgentTemplates(
        IResolverContext context,
        [Service] IAgentTemplateService templates,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return templates.ListAsync(ct);
    }
}

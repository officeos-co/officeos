namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentTemplateMutations
{
    [GraphQLDescription("Creates a new agent pre-configured from a template (prompt, skills, channels).")]
    public async Task<AgentDto> CreateAgentFromTemplate(
        Guid templateId,
        string name,
        string provider,
        string? model,
        IResolverContext context,
        [Service] IAgentTemplateService templates,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        try
        {
            return await templates.CreateAgentFromTemplateAsync(templateId, name, provider, model, user.Id, ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage(ex.Message).SetCode("INVALID_OPERATION").Build());
        }
    }
}

namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class AgentTemplateMutations
{
    public async Task<EnterpriseAgentOs.Domain.DTOs.Agents.AgentDto> CreateAgentFromTemplate(
        Guid templateId,
        string name,
        string provider,
        string? model,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.AgentTemplates.IAgentTemplateService templates,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
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

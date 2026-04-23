namespace EnterpriseAgentOs.Api.Features.SkillGateway;

public class AgentAuthInterceptor : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(
        HttpContext context,
        HotChocolate.Execution.IRequestExecutor requestExecutor,
        HotChocolate.Execution.OperationRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        var auth = context.Request.Headers.Authorization.FirstOrDefault();
        if (auth is null || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            throw new GraphQLException("Missing agent token. Use Authorization: Bearer <agent-uuid>.");
        }

        var token = auth["Bearer ".Length..].Trim();
        if (!Guid.TryParse(token, out var agentId))
        {
            throw new GraphQLException("Invalid agent token format.");
        }

        var repo = context.RequestServices.GetRequiredService<IAgentRepository>();
        var agent = await repo.GetAsync(agentId, cancellationToken);
        var exists = agent is not null && !agent.IsDeleted;
        if (!exists)
        {
            throw new GraphQLException("Agent not found.");
        }

        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}

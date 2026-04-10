using HotChocolate.AspNetCore;
using HotChocolate.Execution;

namespace EnterpriseAgentOs.Api.Entities.Skills.GraphQL;

public class AgentAuthInterceptor : DefaultHttpRequestInterceptor
{
    public override async ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        OperationRequestBuilder requestBuilder,
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

        var db = context.RequestServices.GetRequiredService<EaosDbContext>();
        var exists = await db.Agents.AnyAsync(a => a.Id == agentId && !a.IsDeleted, cancellationToken);
        if (!exists)
        {
            throw new GraphQLException("Agent not found.");
        }

        await base.OnCreateAsync(context, requestExecutor, requestBuilder, cancellationToken);
    }
}

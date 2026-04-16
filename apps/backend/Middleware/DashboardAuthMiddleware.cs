using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Middleware;

/// <summary>
/// GraphQL field middleware that enforces a dashboard session on every resolver call.
/// <c>SessionAuthMiddleware</c> already populates <c>HttpContext.Items["User"]</c> for requests
/// carrying a valid <c>eaos-session</c> cookie. This middleware rejects the request with a
/// GraphQL error if no user is present.
/// </summary>
public sealed class DashboardAuthMiddleware
{
    private readonly FieldDelegate _next;

    public DashboardAuthMiddleware(FieldDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        var http = context.Service<IHttpContextAccessor>().HttpContext;
        var user = http?.Items["User"] as UserRecord;
        if (user is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Unauthenticated. Sign in to access the dashboard.")
                    .SetCode("UNAUTHENTICATED")
                    .Build());
        }

        await _next(context);
    }
}

/// <summary>
/// Helper to read the authenticated dashboard user inside resolvers.
/// </summary>
public static class DashboardAuthContextExtensions
{
    public static UserRecord GetUser(this IResolverContext context)
    {
        var http = context.Service<IHttpContextAccessor>().HttpContext;
        var user = http?.Items["User"] as UserRecord
            ?? throw new GraphQLException("Unauthenticated.");
        return user;
    }
}

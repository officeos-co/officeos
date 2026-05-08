namespace EnterpriseAgentOs.Api.Common.Middleware;

/// <summary>
/// GraphQL field middleware that enforces a dashboard session on every resolver call.
/// <c>SessionAuthMiddleware</c> already populates <c>HttpContext.Items["User"]</c> for requests
/// carrying a valid <c>eaos-session</c> cookie. This middleware rejects the request with a
/// GraphQL error if no user is present.
/// </summary>
public sealed class DashboardAuthMiddleware
{
    private readonly FieldDelegate _fieldDelegate;

    public DashboardAuthMiddleware(FieldDelegate next)
    {
        _fieldDelegate = next;
    }

    public async Task InvokeAsync(IMiddlewareContext context)
    {
        _ = context.Service<UserContext>().Record;
        await _fieldDelegate(context);
    }
}

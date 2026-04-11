using Microsoft.AspNetCore.Mvc.Filters;

namespace EnterpriseAgentOs.Api.Entities.Runners;

/// <summary>
/// Validates runner <c>Authorization: Bearer &lt;auth-token&gt;</c> by
/// looking up the SHA-256 hash in the Runners table.
/// On success, stashes the runner record in <c>HttpContext.Items["Runner"]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RunnerAuthAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var header = http.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var token = header.Substring("Bearer ".Length).Trim();
        var hash = Auth.SessionAuthMiddleware.HashToken(token);

        var repo = http.RequestServices.GetRequiredService<IRunnerRepository>();
        var runner = await repo.GetByAuthTokenHashAsync(hash);

        if (runner is null || runner.Status == "pending")
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        http.Items["Runner"] = runner;
    }
}

using OffceOs.Domain.Features.Agents;

namespace OffceOs.Api.Common.Middleware;

/// <summary>
/// Validates <c>Authorization: Bearer &lt;agent-uuid&gt;</c> by looking up
/// the UUID directly in the Agents table. The agent's UUID IS its
/// credential — no encrypted token storage, no key rotation. Acceptable
/// because the UUID is only known to the backend (which created the agent)
/// and the pod (which received it as <c>ZEROCLAW_AGENT_ID</c>), and both
/// are cluster-internal.
///
/// On success, stashes the resolved agent id in
/// <c>HttpContext.Items["agent-id"]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class AgentTokenAuthAttribute : Attribute, IAsyncAuthorizationFilter
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

        var tokenStr = header.Substring("Bearer ".Length).Trim();
        if (!Guid.TryParse(tokenStr, out var agentId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var repo = http.RequestServices.GetRequiredService<IAgentRepository>();
        var agent = await repo.GetByAsync(new AgentFilter { Id = agentId });
        var exists = agent is not null && !agent.IsDeleted;

        if (!exists)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        http.Items["agent-id"] = agentId;
    }
}

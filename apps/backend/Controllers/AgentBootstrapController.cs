namespace EnterpriseAgentOs.Api.Controllers;

/// <summary>
/// Pod-facing bootstrap endpoint. A zeroclaw-core pod boots with only
/// <c>ZEROCLAW_AGENT_ID</c> set; it calls <c>GET /api/agents/{id}</c>
/// (authenticated by its agent-uuid bearer token) to fetch everything
/// else it needs: display name, the user-supplied system prompt,
/// provider/proxy/gateway endpoints, installed skills, and per-tool
/// allow/deny overrides.
///
/// Credentials are NEVER returned here — the LLM proxy injects provider
/// keys per-request, and skill credentials live behind the skill gateway.
/// Personality <c>.md</c> templates (SOUL/IDENTITY/AGENTS/BOOTSTRAP) are
/// embedded inside the zeroclaw-core binary and written to the pod's PVC
/// on first boot; the backend only ships the system prompt string.
/// This payload is safe to log.
/// </summary>
[ApiController]
[Route("api/agents")]
[EnterpriseAgentOs.Api.Middleware.AgentTokenAuth]
public sealed class AgentBootstrapController : ControllerBase
{
    private readonly IAgentService _agents;
    private readonly EnterpriseAgentOs.Domain.Interfaces.AgentSkills.IAgentSkillRepository _agentSkills;
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;
    private readonly EnterpriseAgentOs.Infrastructure.Configuration.SkillGatewayConfig _gateway;
    private readonly EnterpriseAgentOs.Domain.Interfaces.AgentLogs.IAgentLogService _logs;

    public AgentBootstrapController(
        IAgentService agents,
        EnterpriseAgentOs.Domain.Interfaces.AgentSkills.IAgentSkillRepository agentSkills,
        EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        EnterpriseAgentOs.Infrastructure.Configuration.SkillGatewayConfig gateway,
        EnterpriseAgentOs.Domain.Interfaces.AgentLogs.IAgentLogService logs)
    {
        _agents = agents;
        _agentSkills = agentSkills;
        _db = db;
        _gateway = gateway;
        _logs = logs;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EnterpriseAgentOs.Api.GraphQL.Types.AgentBootstrapPayload>> GetBootstrap(
        Guid id,
        CancellationToken ct)
    {
        // Enforce that the bearer token belongs to the requested agent —
        // an agent must never read another agent's bootstrap.
        var authedAgentId = (Guid)HttpContext.Items["agent-id"]!;
        if (authedAgentId != id) return Forbid();

        var agent = await _agents.GetAsync(id, ct);
        if (agent is null) return NotFound();

        // Fetch the raw record so we can read the persisted system prompt
        // (AgentDto is the API/Graph-facing projection and exposes `Prompt`
        // already, but we read the record directly to stay one-hop from the
        // source of truth).
        var record = await _db.Agents
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (record is null) return NotFound();

        var skills = await _agentSkills.ListSkillNamesByAgentAsync(id, ct);
        var permRows = await _db.AgentToolPermissions
            .AsNoTracking()
            .Where(p => p.AgentId == id)
            .ToListAsync(ct);

        // Backend URL the pod should call for the LLM proxy. Prefer the
        // in-cluster service hostname when available; fall back to the
        // request's scheme+host which works for local dev.
        var backend = $"{Request.Scheme}://{Request.Host.Value}";
        var proxyUrl = $"{backend}/v1";

        // WebSocket gateway bind address — the pod listens on all interfaces
        // at the well-known zeroclaw port (42617).  The previous code was
        // sending the skill-gateway hostname here, which failed SocketAddr
        // parsing on the Rust side ("invalid socket address syntax").
        var gatewayHost = "0.0.0.0";
        var gatewayPort = 42617;

        var payload = new EnterpriseAgentOs.Api.GraphQL.Types.AgentBootstrapPayload(
            AgentId: id,
            DisplayName: agent.Name,
            SystemPrompt: record.Prompt,
            Provider: new EnterpriseAgentOs.Api.GraphQL.Types.AgentProviderBootstrap(
                Name: agent.Provider,
                Model: agent.Model ?? "auto",
                ApiUrl: proxyUrl,
                TokenRef: null),
            Proxy: new EnterpriseAgentOs.Api.GraphQL.Types.AgentProxyBootstrap(
                Url: proxyUrl,
                Token: null),
            Gateway: new EnterpriseAgentOs.Api.GraphQL.Types.AgentGatewayBootstrap(
                Host: gatewayHost,
                Port: gatewayPort,
                TlsCertRef: null),
            Skills: skills
                .Select(n => new EnterpriseAgentOs.Api.GraphQL.Types.AgentInstalledSkillSummary(n))
                .ToList(),
            ToolPermissions: new EnterpriseAgentOs.Api.GraphQL.Types.AgentToolPermissionsBootstrap(
                permRows
                    .Select(p => new EnterpriseAgentOs.Api.GraphQL.Types.AgentBootstrapToolPermission(
                        Skill: p.SkillName,
                        Tool: p.ToolName,
                        Mode: p.Permission.ToString().ToLowerInvariant()))
                    .ToList()));

        await _logs.AppendAsync(new EnterpriseAgentOs.Domain.Models.AgentLogRecord
        {
            AgentId = id,
            Time = DateTime.UtcNow,
            Type = EnterpriseAgentOs.Domain.Models.AgentLogType.AgentStartup,
            Content = $"Agent '{agent.Name}' started (pod bootstrap)",
        }, ct);

        return Ok(payload);
    }
}

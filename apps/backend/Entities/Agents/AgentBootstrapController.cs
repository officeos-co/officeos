namespace EnterpriseAgentOs.Api.Entities.Agents;

/// <summary>
/// Pod-facing bootstrap endpoint. A zeroclaw-core pod boots with only
/// <c>ZEROCLAW_AGENT_ID</c> set; it calls <c>GET /api/agents/{id}</c>
/// (authenticated by its agent-uuid bearer token) to fetch everything
/// else it needs: provider + model hints, LLM-proxy endpoint, vault
/// base URL, installed skills, and per-tool allow/deny overrides.
///
/// Credentials are NEVER returned here — the LLM proxy still injects
/// provider keys per-request, and skill credentials live behind the
/// skill gateway. This payload is safe to log.
/// </summary>
[ApiController]
[Route("api/agents")]
[EnterpriseAgentOs.Api.Entities.Skills.AgentTokenAuth]
public sealed class AgentBootstrapController : ControllerBase
{
    private readonly IAgentService _agents;
    private readonly EnterpriseAgentOs.Api.Entities.AgentSkills.IAgentSkillRepository _agentSkills;
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;
    private readonly EnterpriseAgentOs.Api.Properties.CouchDbConfig _couch;
    private readonly EnterpriseAgentOs.Api.Properties.LiteLlmConfig _liteLlm;

    public AgentBootstrapController(
        IAgentService agents,
        EnterpriseAgentOs.Api.Entities.AgentSkills.IAgentSkillRepository agentSkills,
        EnterpriseAgentOs.Api.Database.EaosDbContext db,
        EnterpriseAgentOs.Api.Properties.CouchDbConfig couch,
        EnterpriseAgentOs.Api.Properties.LiteLlmConfig liteLlm)
    {
        _agents = agents;
        _agentSkills = agentSkills;
        _db = db;
        _couch = couch;
        _liteLlm = liteLlm;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<EnterpriseAgentOs.Api.Entities.Agents.Types.AgentBootstrapPayload>> GetBootstrap(
        Guid id,
        CancellationToken ct)
    {
        // Enforce that the bearer token belongs to the requested agent —
        // an agent must never read another agent's bootstrap.
        var authedAgentId = (Guid)HttpContext.Items["agent-id"]!;
        if (authedAgentId != id) return Forbid();

        var agent = await _agents.GetAsync(id, ct);
        if (agent is null) return NotFound();

        var skills = await _agentSkills.ListSkillNamesByAgentAsync(id, ct);
        var permRows = await _db.AgentToolPermissions
            .AsNoTracking()
            .Where(p => p.AgentId == id)
            .ToListAsync(ct);

        // Backend URL the pod should call for the LLM proxy. Prefer the
        // in-cluster service hostname when available; fall back to the
        // request's scheme+host which works for local dev.
        var backend = $"{Request.Scheme}://{Request.Host.Value}";
        var proxyEndpoint = $"{backend}/v1";

        var payload = new EnterpriseAgentOs.Api.Entities.Agents.Types.AgentBootstrapPayload(
            AgentId: id,
            Name: agent.Name,
            Provider: new EnterpriseAgentOs.Api.Entities.Agents.Types.AgentProviderBootstrap(
                Backend: backend,
                Model: agent.Model ?? "auto",
                ProxyEndpoint: proxyEndpoint),
            Vault: new EnterpriseAgentOs.Api.Entities.Agents.Types.AgentVaultBootstrap(
                BaseUrl: _couch.Url ?? string.Empty),
            Skills: skills
                .Select(n => new EnterpriseAgentOs.Api.Entities.Agents.Types.AgentInstalledSkillSummary(n))
                .ToList(),
            ToolPermissions: permRows
                .Select(p => new EnterpriseAgentOs.Api.Entities.Agents.Types.AgentBootstrapToolPermission(
                    Skill: p.SkillName,
                    Tool: p.ToolName,
                    Mode: p.Permission.ToString().ToLowerInvariant()))
                .ToList());

        return Ok(payload);
    }
}

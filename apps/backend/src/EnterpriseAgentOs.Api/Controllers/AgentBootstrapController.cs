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
[Middleware.AgentTokenAuth]
public sealed class AgentBootstrapController : ControllerBase
{
    private readonly IAgentService _agents;
    private readonly IAgentRepository _agentRepo;
    private readonly IAgentSkillRepository _agentSkills;
    private readonly SkillGatewayConfig _gateway;
    private readonly IAgentLogService _logs;

    public AgentBootstrapController(
        IAgentService agents,
        IAgentRepository agentRepo,
        IAgentSkillRepository agentSkills,
        SkillGatewayConfig gateway,
        IAgentLogService logs)
    {
        _agents = agents;
        _agentRepo = agentRepo;
        _agentSkills = agentSkills;
        _gateway = gateway;
        _logs = logs;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GraphQL.Types.AgentBootstrapPayload>> GetBootstrap(
        Guid id,
        CancellationToken ct)
    {
        // Enforce that the bearer token belongs to the requested agent —
        // an agent must never read another agent's bootstrap.
        var authedAgentId = (Guid)HttpContext.Items["agent-id"]!;
        if (authedAgentId != id) return Forbid();

        var agent = await _agents.GetAsync(id, ct);
        if (agent is null) return NotFound();

        // Fetch the raw record so we can read the persisted system prompt.
        var record = await _agentRepo.GetAsync(id, ct);
        if (record is null) return NotFound();

        var skills = await _agentSkills.ListSkillNamesByAgentAsync(id, ct);
        var permRows = await _agentSkills.ListToolPermissionsAsync(id, ct);

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

        var payload = new GraphQL.Types.AgentBootstrapPayload(
            AgentId: id,
            DisplayName: agent.Name,
            SystemPrompt: record.Prompt,
            Provider: new GraphQL.Types.AgentProviderBootstrap(
                Name: agent.Provider,
                Model: agent.Model ?? "auto",
                ApiUrl: proxyUrl,
                TokenRef: null),
            Proxy: new GraphQL.Types.AgentProxyBootstrap(
                Url: proxyUrl,
                Token: null),
            Gateway: new GraphQL.Types.AgentGatewayBootstrap(
                Host: gatewayHost,
                Port: gatewayPort,
                TlsCertRef: null),
            Skills: skills
                .Select(n => new GraphQL.Types.AgentInstalledSkillSummary(n))
                .ToList(),
            ToolPermissions: new GraphQL.Types.AgentToolPermissionsBootstrap(
                permRows
                    .Select(p => new GraphQL.Types.AgentBootstrapToolPermission(
                        Skill: p.SkillName,
                        Tool: p.ToolName,
                        Mode: p.Permission.ToString().ToLowerInvariant()))
                    .ToList()));

        await _logs.AppendAsync(new AgentLogRecord
        {
            AgentId = id,
            Time = DateTime.UtcNow,
            Type = AgentLogType.AgentStartup,
            Content = $"Agent '{agent.Name}' started (pod bootstrap)",
        }, ct);

        return Ok(payload);
    }
}

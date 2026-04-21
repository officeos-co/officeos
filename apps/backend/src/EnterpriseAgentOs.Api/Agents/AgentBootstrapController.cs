namespace EnterpriseAgentOs.Api.Agents;

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
[AgentTokenAuth]
public sealed class AgentBootstrapController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly SkillGatewayConfig _skillGatewayConfig;
    private readonly IAgentLogService _agentLogService;

    public AgentBootstrapController(
        IAgentService agents,
        IAgentRepository agentRepo,
        IAgentSkillRepository agentSkills,
        SkillGatewayConfig gateway,
        IAgentLogService logs)
    {
        _agentService = agents;
        _agentRepository = agentRepo;
        _agentSkillRepository = agentSkills;
        _skillGatewayConfig = gateway;
        _agentLogService = logs;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentBootstrapPayload>> GetBootstrap(
        Guid id,
        CancellationToken ct)
    {
        // Enforce that the bearer token belongs to the requested agent —
        // an agent must never read another agent's bootstrap.
        var authedAgentId = (Guid)HttpContext.Items["agent-id"]!;
        if (authedAgentId != id) return Forbid();

        var agent = await _agentService.GetAsync(id, ct);
        if (agent is null) return NotFound();

        // Fetch the raw record so we can read the persisted system prompt.
        var record = await _agentRepository.GetAsync(id, ct);
        if (record is null) return NotFound();

        var skills = await _agentSkillRepository.ListSkillNamesByAgentAsync(id, ct);
        var permRows = await _agentSkillRepository.ListToolPermissionsAsync(id, ct);

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

        var payload = new AgentBootstrapPayload(
            AgentId: id,
            DisplayName: agent.Name,
            SystemPrompt: record.Prompt,
            Provider: new AgentProviderBootstrap(
                Name: agent.Provider,
                Model: agent.Model ?? "auto",
                ApiUrl: proxyUrl,
                TokenRef: null),
            Proxy: new AgentProxyBootstrap(
                Url: proxyUrl,
                Token: null),
            Gateway: new AgentGatewayBootstrap(
                Host: gatewayHost,
                Port: gatewayPort,
                TlsCertRef: null),
            Skills: skills
                .Select(n => new AgentInstalledSkillSummary(n))
                .ToList(),
            ToolPermissions: new AgentToolPermissionsBootstrap(
                permRows
                    .Select(p => new AgentBootstrapToolPermission(
                        Skill: p.SkillName,
                        Tool: p.ToolName,
                        Mode: p.Permission.ToString().ToLowerInvariant()))
                    .ToList()));

        await _agentLogService.AppendAsync(new AgentLogRecord
        {
            AgentId = id,
            Time = DateTime.UtcNow,
            Type = AgentLogType.AgentStartup,
            Content = $"Agent '{agent.Name}' started (pod bootstrap)",
        }, ct);

        return Ok(payload);
    }
}

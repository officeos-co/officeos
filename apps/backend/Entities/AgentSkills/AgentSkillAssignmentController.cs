namespace EnterpriseAgentOs.Api.Entities.AgentSkills;

[ApiController]
[Route("api/agents/{agentId:guid}/skills")]
public sealed class AgentSkillAssignmentController : ControllerBase
{
    private readonly IAgentSkillRepository _repository;
    private readonly IAgentRepository _agents;

    public AgentSkillAssignmentController(
        IAgentSkillRepository repository,
        IAgentRepository agents)
    {
        _repository = repository;
        _agents = agents;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentSkillRecord>>> List(Guid agentId, CancellationToken ct)
    {
        var agent = await _agents.GetAsync(agentId, ct);
        if (agent is null) return NotFound();

        var skills = await _repository.ListByAgentAsync(agentId, ct);
        return Ok(skills);
    }

    public sealed record AssignSkillsRequest(string[] SkillNames);

    [HttpPost]
    public async Task<IActionResult> Assign(
        Guid agentId,
        [FromBody] AssignSkillsRequest request,
        CancellationToken ct)
    {
        var agent = await _agents.GetAsync(agentId, ct);
        if (agent is null) return NotFound();

        if (request.SkillNames.Length == 0)
            return BadRequest(new { error = "At least one skill name is required." });

        await _repository.AssignAsync(agentId, request.SkillNames, ct);
        var skills = await _repository.ListByAgentAsync(agentId, ct);
        return Ok(skills);
    }

    [HttpDelete("{name}")]
    public async Task<IActionResult> Remove(Guid agentId, string name, CancellationToken ct)
    {
        var agent = await _agents.GetAsync(agentId, ct);
        if (agent is null) return NotFound();

        var removed = await _repository.RemoveAsync(agentId, name, ct);
        if (!removed) return NotFound();

        return NoContent();
    }
}

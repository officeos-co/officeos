
namespace EnterpriseAgentOs.Api.Entities.Agents;

[ApiController]
[Route("api/agents")]
public sealed class AgentsController : ControllerBase
{
    private readonly IAgentService _service;
    private readonly IVaultClient _vault;

    public AgentsController(IAgentService service, IVaultClient vault)
    {
        _service = service;
        _vault = vault;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentDto>>> List(CancellationToken ct)
    {
        var agents = await _service.ListAsync(ct);
        return Ok(agents);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AgentDto>> Get(Guid id, CancellationToken ct)
    {
        var agent = await _service.GetAsync(id, ct);
        if (agent is null)
        {
            return NotFound();
        }
        return Ok(agent);
    }

    [HttpPost]
    public async Task<ActionResult<AgentDto>> Create([FromBody] CreateAgentRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(List), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}/memory")]
    public async Task<ActionResult<IReadOnlyList<string>>> ListMemory(Guid id, CancellationToken ct)
    {
        if (await _service.GetAsync(id, ct) is null)
        {
            return NotFound();
        }
        var files = await _vault.ListFilesAsync(id, ct);
        return Ok(files);
    }

    [HttpGet("{id:guid}/memory/{*fileName}")]
    public async Task<IActionResult> GetMemoryFile(Guid id, string fileName, CancellationToken ct)
    {
        if (await _service.GetAsync(id, ct) is null)
        {
            return NotFound();
        }
        var content = await _vault.GetFileAsync(id, fileName, ct);
        if (content is null)
        {
            return NotFound();
        }
        return Content(content, "text/markdown");
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var deleted = await _service.DeleteAsync(id, ct);
        if (!deleted)
        {
            return NotFound();
        }
        return NoContent();
    }
}

using EnterpriseAgentOs.Api.Entities.Skills;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseAgentOs.Api.Entities.Skills;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController : ControllerBase
{
    private readonly ISkillService _service;

    public SkillsController(ISkillService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkillDto>>> List(CancellationToken ct)
    {
        var skills = await _service.ListAsync(ct);
        return Ok(skills);
    }
}

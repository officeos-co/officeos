using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills;

[ApiController]
[Route("api/skills")]
public sealed class SkillController : ControllerBase
{
    private readonly ISkillService _service;
    private readonly SkillRuntimeClient _runtime;

    public SkillController(
        ISkillService service,
        SkillRuntimeClient runtime)
    {
        _service = service;
        _runtime = runtime;
    }

    // ---------- catalog ----------

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkillDto>>> List(CancellationToken ct)
    {
        return Ok(await _service.ListAsync(ct));
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<SkillDto>> Get(string name, CancellationToken ct)
    {
        var dto = await _service.GetAsync(name, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpGet("{name}/doc")]
    public async Task<ActionResult> GetDoc(string name, CancellationToken ct)
    {
        var manifests = await _runtime.GetManifestsAsync(ct);
        var manifest = manifests.FirstOrDefault(m =>
            string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        if (manifest is null)
        {
            return NotFound();
        }
        return Content(manifest.Doc, "text/markdown");
    }

    [HttpPost("{name}/install")]
    public async Task<ActionResult<SkillDto>> Install(string name, CancellationToken ct)
    {
        var dto = await _service.InstallAsync(name, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost("{name}/uninstall")]
    public async Task<ActionResult<SkillDto>> Uninstall(string name, CancellationToken ct)
    {
        var dto = await _service.UninstallAsync(name, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut("{name}/credentials")]
    public async Task<ActionResult<SkillDto>> PutCredentials(
        string name,
        [FromBody] PutCredentialsRequest request,
        CancellationToken ct)
    {
        try
        {
            var dto = await _service.PutCredentialsAsync(name, request.Credentials, ct);
            return dto is null ? NotFound() : Ok(dto);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    [HttpPut("{name}/run-target")]
    public async Task<ActionResult<SkillDto>> SetRunTarget(
        string name,
        [FromBody] SetRunTargetRequest request,
        CancellationToken ct)
    {
        if (request.RunTarget is not "cloud" and not "runner")
            return BadRequest(new { error = "runTarget must be 'cloud' or 'runner'" });

        var dto = await _service.SetRunTargetAsync(name, request.RunTarget, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    // ---------- user-auth capabilities (dashboard introspection) ----------

    [HttpGet("/api/capabilities")]
    public async Task<ActionResult<CapabilitiesResponse>> Capabilities(CancellationToken ct)
    {
        var response = await _service.ListCapabilitiesAsync(agentId: null, ct);
        return Ok(response);
    }

    // ---------- user-auth execution (dashboard test buttons) ----------

    [HttpPost("{skill}/{action}")]
    public async Task<IActionResult> ExecuteAction(
        string skill,
        string action,
        [FromBody] JsonElement body,
        CancellationToken ct)
    {
        var creds = await _service.GetDecryptedCredentialsAsync(skill, ct);
        if (creds is null)
        {
            return Conflict(new { error = $"Skill '{skill}' is not installed or not configured." });
        }
        try
        {
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(body.GetRawText())
                ?? new Dictionary<string, object>();
            var result = await _runtime.ExecuteAsync(skill, action, parameters, creds, ct: ct);
            if (result.Success)
            {
                return Ok(result.Result);
            }
            return UnprocessableEntity(new { error = result.Error });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }
}

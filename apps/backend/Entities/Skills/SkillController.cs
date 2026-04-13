using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using EnterpriseAgentOs.Api.Database.Models;
using EnterpriseAgentOs.Api.Entities.Events;

namespace EnterpriseAgentOs.Api.Entities.Skills;

[ApiController]
[Route("api/skills")]
public sealed class SkillController : ControllerBase
{
    private readonly ISkillService _service;
    private readonly ISkillCatalogRepository _catalog;
    private readonly SkillRuntimeClient _runtime;
    private readonly IAmazonS3 _s3;
    private readonly SkillStorageConfig _storage;
    private readonly ISystemEventService _events;

    public SkillController(
        ISkillService service,
        ISkillCatalogRepository catalog,
        SkillRuntimeClient runtime,
        IAmazonS3 s3,
        SkillStorageConfig storage,
        ISystemEventService events)
    {
        _service = service;
        _catalog = catalog;
        _runtime = runtime;
        _s3 = s3;
        _storage = storage;
        _events = events;
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
        var skill = await _catalog.GetByNameAsync(name, ct);
        if (skill is null) return NotFound();

        var manifest = JsonSerializer.Deserialize<RuntimeManifest>(skill.ManifestJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (manifest is null) return NotFound();

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

    // ---------- bundle download (for skill-runtime on-demand loading) ----------

    [HttpGet("{name}/bundle")]
    public async Task<IActionResult> GetBundle(string name, CancellationToken ct)
    {
        var skill = await _catalog.GetByNameAsync(name, ct);
        if (skill is null || string.IsNullOrEmpty(skill.BundleS3Key))
            return NotFound(new { error = "No bundle available for this skill" });

        try
        {
            var response = await _s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _storage.Bucket,
                Key = skill.BundleS3Key,
            }, ct);

            return File(response.ResponseStream, "application/javascript", $"{name}.js");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { error = "Bundle not found in storage" });
        }
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
            await _events.RecordAsync(new SystemEventRecord
            {
                Severity = "error",
                Category = "skill_execution",
                Message = $"Skill {skill}.{action} failed: {result.Error}",
                SkillName = skill,
                CorrelationId = Response.Headers["X-Correlation-Id"].FirstOrDefault(),
            });
            return UnprocessableEntity(new { error = result.Error });
        }
        catch (HttpRequestException ex)
        {
            await _events.RecordAsync(new SystemEventRecord
            {
                Severity = "error",
                Category = "skill_execution",
                Message = $"Cloud skill-runtime unreachable for {skill}.{action}: {ex.Message}",
                SkillName = skill,
                CorrelationId = Response.Headers["X-Correlation-Id"].FirstOrDefault(),
            });
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }
}

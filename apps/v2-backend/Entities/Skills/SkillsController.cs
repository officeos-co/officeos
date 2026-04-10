using EnterpriseAgentOs.Api.Entities.Skills.Implementations;

namespace EnterpriseAgentOs.Api.Entities.Skills;

[ApiController]
[Route("api/skills")]
public sealed class SkillsController : ControllerBase
{
    private readonly ISkillService _service;
    private readonly NotionSkill _notion;
    private readonly GithubSkill _github;
    private readonly GoogleSkill _google;

    public SkillsController(
        ISkillService service,
        NotionSkill notion,
        GithubSkill github,
        GoogleSkill google)
    {
        _service = service;
        _notion = notion;
        _github = github;
        _google = google;
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
    public ActionResult GetDoc(string name)
    {
        if (!SkillManifests.AllWithDocs.TryGetValue(name, out var manifest) || manifest.Doc is null)
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

    // ---------- user-auth capabilities (dashboard introspection) ----------

    [HttpGet("/api/capabilities")]
    public async Task<ActionResult<CapabilitiesResponse>> Capabilities(CancellationToken ct)
    {
        var response = await _service.ListCapabilitiesAsync(ct);
        return Ok(response);
    }

    // ---------- user-auth execution (dashboard test buttons) ----------

    [HttpPost("notion/search")]
    public Task<IActionResult> NotionSearch([FromBody] NotionSearchRequest body, CancellationToken ct) =>
        ExecuteAsync("notion", async creds => await _notion.SearchAsync(body, creds, ct), ct);

    [HttpPost("notion/read_page")]
    public Task<IActionResult> NotionReadPage([FromBody] NotionReadPageRequest body, CancellationToken ct) =>
        ExecuteAsync("notion", async creds => await _notion.ReadPageAsync(body, creds, ct), ct);

    [HttpPost("github/list_repos")]
    public Task<IActionResult> GithubListRepos([FromBody] GithubListReposRequest body, CancellationToken ct) =>
        ExecuteAsync("github", async creds => await _github.ListReposAsync(body, creds, ct), ct);

    [HttpPost("github/list_issues")]
    public Task<IActionResult> GithubListIssues([FromBody] GithubRepoRequest body, CancellationToken ct) =>
        ExecuteAsync("github", async creds => await _github.ListIssuesAsync(body, creds, ct), ct);

    [HttpPost("github/list_prs")]
    public Task<IActionResult> GithubListPrs([FromBody] GithubRepoRequest body, CancellationToken ct) =>
        ExecuteAsync("github", async creds => await _github.ListPrsAsync(body, creds, ct), ct);

    [HttpPost("google/drive_search")]
    public Task<IActionResult> GoogleDriveSearch([FromBody] GoogleDriveSearchRequest body, CancellationToken ct) =>
        ExecuteAsync("google", async creds => await _google.DriveSearchAsync(body, creds, ct), ct);

    [HttpPost("google/calendar_upcoming")]
    public Task<IActionResult> GoogleCalendarUpcoming([FromBody] GoogleCalendarUpcomingRequest body, CancellationToken ct) =>
        ExecuteAsync("google", async creds => await _google.CalendarUpcomingAsync(body, creds, ct), ct);

    private async Task<IActionResult> ExecuteAsync(
        string skillName,
        Func<IReadOnlyDictionary<string, string>, Task<object>> handler,
        CancellationToken ct)
    {
        var creds = await _service.GetDecryptedCredentialsAsync(skillName, ct);
        if (creds is null)
        {
            return Conflict(new { error = $"Skill '{skillName}' is not installed or not configured." });
        }
        try
        {
            var result = await handler(creds);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { error = ex.Message });
        }
    }
}

using EnterpriseAgentOs.Api.Entities.Skills.Implementations;

namespace EnterpriseAgentOs.Api.Entities.Skills;

[ApiController]
[Route("api/agents/me")]
[AgentTokenAuth]
public sealed class AgentSkillsController : ControllerBase
{
    private readonly ISkillService _service;
    private readonly NotionSkill _notion;
    private readonly GithubSkill _github;
    private readonly GoogleSkill _google;

    public AgentSkillsController(
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

    [HttpGet("capabilities")]
    public async Task<ActionResult<CapabilitiesResponse>> Capabilities(CancellationToken ct)
    {
        var response = await _service.ListCapabilitiesAsync(ct);
        return Ok(response);
    }

    [HttpPost("skills/notion/search")]
    public Task<IActionResult> NotionSearch([FromBody] NotionSearchRequest body, CancellationToken ct) =>
        ExecuteAsync("notion", async creds => await _notion.SearchAsync(body, creds, ct), ct);

    [HttpPost("skills/notion/read_page")]
    public Task<IActionResult> NotionReadPage([FromBody] NotionReadPageRequest body, CancellationToken ct) =>
        ExecuteAsync("notion", async creds => await _notion.ReadPageAsync(body, creds, ct), ct);

    [HttpPost("skills/github/list_repos")]
    public Task<IActionResult> GithubListRepos([FromBody] GithubListReposRequest body, CancellationToken ct) =>
        ExecuteAsync("github", async creds => await _github.ListReposAsync(body, creds, ct), ct);

    [HttpPost("skills/github/list_issues")]
    public Task<IActionResult> GithubListIssues([FromBody] GithubRepoRequest body, CancellationToken ct) =>
        ExecuteAsync("github", async creds => await _github.ListIssuesAsync(body, creds, ct), ct);

    [HttpPost("skills/github/list_prs")]
    public Task<IActionResult> GithubListPrs([FromBody] GithubRepoRequest body, CancellationToken ct) =>
        ExecuteAsync("github", async creds => await _github.ListPrsAsync(body, creds, ct), ct);

    [HttpPost("skills/google/drive_search")]
    public Task<IActionResult> GoogleDriveSearch([FromBody] GoogleDriveSearchRequest body, CancellationToken ct) =>
        ExecuteAsync("google", async creds => await _google.DriveSearchAsync(body, creds, ct), ct);

    [HttpPost("skills/google/calendar_upcoming")]
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

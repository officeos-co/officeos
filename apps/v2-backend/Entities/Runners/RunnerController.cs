using System.Security.Cryptography;

namespace EnterpriseAgentOs.Api.Entities.Runners;

/// <summary>
/// Dashboard-facing runner management (session auth via middleware).
/// </summary>
[ApiController]
[Route("api/runners")]
public sealed class RunnerController : ControllerBase
{
    private readonly IRunnerRepository _runners;
    private readonly IRunnerJobRepository _jobs;
    private readonly ILogger<RunnerController> _logger;

    public RunnerController(IRunnerRepository runners, IRunnerJobRepository jobs, ILogger<RunnerController> logger)
    {
        _runners = runners;
        _jobs = jobs;
        _logger = logger;
    }

    private UserRecord? CurrentUser => HttpContext.Items["User"] as UserRecord;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRunnerRequest body, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { error = "Name is required" });

        var registrationToken = $"sr_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}";
        var hash = Auth.SessionAuthMiddleware.HashToken(registrationToken);

        var runner = await _runners.CreateAsync(CurrentUser.Id, body.Name.Trim(), hash, ct);

        _logger.LogInformation("Runner {RunnerId} ({RunnerName}) created by user {UserId}",
            runner.Id, runner.Name, CurrentUser.Id);

        return Ok(new
        {
            id = runner.Id,
            name = runner.Name,
            registrationToken,
        });
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();

        var runners = await _runners.ListByOwnerAsync(CurrentUser.Id, ct);
        return Ok(runners.Select(r => new
        {
            id = r.Id,
            name = r.Name,
            status = r.Status,
            lastHeartbeatAt = r.LastHeartbeatAt,
            version = r.Version,
            createdAt = r.CreatedAt,
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();

        var runner = await _runners.GetByIdAsync(id, ct);
        if (runner is null || runner.OwnerId != CurrentUser.Id)
            return NotFound();

        var recentJobs = await _jobs.GetRecentByRunnerAsync(id, 20, ct);

        return Ok(new
        {
            id = runner.Id,
            name = runner.Name,
            status = runner.Status,
            lastHeartbeatAt = runner.LastHeartbeatAt,
            version = runner.Version,
            createdAt = runner.CreatedAt,
            recentJobs = recentJobs.Select(j => new
            {
                id = j.Id,
                status = j.Status,
                createdAt = j.CreatedAt,
                startedAt = j.StartedAt,
                completedAt = j.CompletedAt,
            }),
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (CurrentUser is null) return Unauthorized();

        var runner = await _runners.GetByIdAsync(id, ct);
        if (runner is null || runner.OwnerId != CurrentUser.Id)
            return NotFound();

        await _runners.DeleteAsync(id, ct);
        _logger.LogInformation("Runner {RunnerId} deleted by user {UserId}", id, CurrentUser.Id);
        return NoContent();
    }
}

public sealed record CreateRunnerRequest(string Name);

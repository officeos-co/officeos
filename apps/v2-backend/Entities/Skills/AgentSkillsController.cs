using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Skills;

[ApiController]
[Route("api/agents/me")]
[AgentTokenAuth]
public sealed class AgentSkillsController : ControllerBase
{
    private readonly ISkillService _service;
    private readonly SkillRuntimeClient _runtime;
    private readonly IRunnerRepository _runners;
    private readonly IRunnerJobRepository _runnerJobs;
    private readonly RunnerJobWaiter _jobWaiter;

    private readonly ILogger<AgentSkillsController> _logger;

    public AgentSkillsController(
        ISkillService service,
        SkillRuntimeClient runtime,
        IRunnerRepository runners,
        IRunnerJobRepository runnerJobs,
        RunnerJobWaiter jobWaiter,
        ILogger<AgentSkillsController> logger)
    {
        _service = service;
        _runtime = runtime;
        _runners = runners;
        _runnerJobs = runnerJobs;
        _jobWaiter = jobWaiter;
        _logger = logger;
    }

    [HttpGet("capabilities")]
    public async Task<ActionResult<CapabilitiesResponse>> Capabilities(CancellationToken ct)
    {
        var response = await _service.ListCapabilitiesAsync(ct);
        return Ok(response);
    }

    /// <summary>
    /// Generic skill execution endpoint — routes to cloud skill-runtime
    /// or a self-hosted runner based on the skill's run target setting.
    /// </summary>
    [HttpPost("skill-exec")]
    public async Task<IActionResult> SkillExec([FromBody] SkillExecRequest body, CancellationToken ct)
    {
        var runTarget = await _service.GetRunTargetAsync(body.Skill, ct);
        _logger.LogInformation("Agent skill-exec: {Skill}.{Action} (target={RunTarget})",
            body.Skill, body.Action, runTarget);

        if (runTarget == "runner")
        {
            return await DispatchToRunnerAsync(body, ct);
        }

        // Cloud execution (default)
        var creds = await _service.GetDecryptedCredentialsAsync(body.Skill, ct);
        if (creds is null)
        {
            _logger.LogWarning("Skill {Skill} not configured for cloud execution", body.Skill);
            return Conflict(new { error = $"Skill '{body.Skill}' is not installed or not configured. Configure credentials on the Skills page, or set it to run on a self-hosted runner." });
        }
        try
        {
            var result = await _runtime.ExecuteAsync(
                body.Skill,
                body.Action,
                body.Params ?? new Dictionary<string, object>(),
                creds,
                ct);
            if (result.Success)
            {
                return Ok(result);
            }
            return UnprocessableEntity(result);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                new { error = $"Cloud skill-runtime unreachable: {ex.Message}" });
        }
    }

    private async Task<IActionResult> DispatchToRunnerAsync(SkillExecRequest body, CancellationToken ct)
    {
        // Find any online runner
        var onlineRunners = await _runners.GetOnlineRunnersAsync(ct);
        if (onlineRunners.Count == 0)
        {
            _logger.LogWarning("No online runners available for skill {Skill}", body.Skill);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = $"Skill '{body.Skill}' is configured to run on a self-hosted runner, but no runners are currently online. "
                    + "Start a runner with `docker run -e PLATFORM_URL=... -e REGISTRATION_TOKEN=... harkro123/skill-runner`, "
                    + "or switch the skill back to cloud execution on the Skills page.",
            });
        }

        // Pick the runner with the most recent heartbeat (most responsive)
        var runner = onlineRunners.OrderByDescending(r => r.LastHeartbeatAt).First();

        var payload = JsonSerializer.Serialize(new
        {
            skill = body.Skill,
            action = body.Action,
            @params = body.Params ?? new Dictionary<string, object>(),
        });

        var job = await _runnerJobs.CreateAsync(runner.Id, payload, TimeSpan.FromSeconds(60), ct);
        _logger.LogInformation("Dispatched job {JobId} to runner {RunnerId} ({RunnerName}) for {Skill}.{Action}",
            job.Id, runner.Id, runner.Name, body.Skill, body.Action);
        var tcs = _jobWaiter.Register(job.Id);

        try
        {
            var result = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ct);
            if (result.Success)
                return Ok(new { success = true, result = result.Result });
            return UnprocessableEntity(new
            {
                success = false,
                error = result.Error,
                executedBy = "runner",
                runnerName = runner.Name,
            });
        }
        catch (TimeoutException)
        {
            _jobWaiter.Remove(job.Id);
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                error = $"Runner '{runner.Name}' did not complete the job within 30 seconds. "
                    + "The runner may be overloaded, or the skill execution is taking too long. "
                    + "Check the runner logs or try again.",
            });
        }
    }

    // --- Legacy per-action routes (dispatch through runtime) ---

    [HttpPost("skills/{skill}/{action}")]
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
            // Convert JsonElement body to dictionary for the runtime
            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(body.GetRawText())
                ?? new Dictionary<string, object>();
            var result = await _runtime.ExecuteAsync(skill, action, parameters, creds, ct);
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

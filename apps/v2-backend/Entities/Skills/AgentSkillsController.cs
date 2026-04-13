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
    private readonly IBrowserSessionRepository _browserSessions;

    private readonly ILogger<AgentSkillsController> _logger;

    public AgentSkillsController(
        ISkillService service,
        SkillRuntimeClient runtime,
        IRunnerRepository runners,
        IRunnerJobRepository runnerJobs,
        RunnerJobWaiter jobWaiter,
        IBrowserSessionRepository browserSessions,
        ILogger<AgentSkillsController> logger)
    {
        _service = service;
        _runtime = runtime;
        _runners = runners;
        _runnerJobs = runnerJobs;
        _jobWaiter = jobWaiter;
        _browserSessions = browserSessions;
        _logger = logger;
    }

    [HttpGet("capabilities")]
    public async Task<ActionResult<CapabilitiesResponse>> Capabilities(CancellationToken ct)
    {
        var agentId = (Guid)HttpContext.Items["agent-id"]!;
        var response = await _service.ListCapabilitiesAsync(agentId, ct);
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

        // Browser session injection
        SessionContext? sessionContext = null;
        Guid? agentId = null;
        if (string.Equals(body.Skill, "browser", StringComparison.OrdinalIgnoreCase))
        {
            agentId = (Guid)HttpContext.Items["agent-id"]!;
            var existingSession = await _browserSessions.GetByAgentAsync(agentId.Value, ct);
            if (existingSession is not null)
            {
                sessionContext = new SessionContext
                {
                    SessionId = existingSession.RuntimeSessionId,
                    CookiesJson = existingSession.CookiesJson,
                };
            }
        }

        try
        {
            var result = await _runtime.ExecuteAsync(
                body.Skill,
                body.Action,
                body.Params ?? new Dictionary<string, object>(),
                creds,
                sessionContext,
                ct);

            // Persist browser session state
            if (string.Equals(body.Skill, "browser", StringComparison.OrdinalIgnoreCase)
                && agentId.HasValue && result.SessionMeta is not null)
            {
                if (string.IsNullOrEmpty(result.SessionMeta.SessionId))
                {
                    await _browserSessions.DeleteByAgentAsync(agentId.Value, ct);
                }
                else
                {
                    await _browserSessions.UpsertAsync(agentId.Value,
                        result.SessionMeta.SessionId, result.SessionMeta.CookiesJson, ct);
                }
            }

            // Strip session metadata from the response to the agent
            if (result.Success)
                return Ok(new { success = result.Success, result = result.Result });
            return UnprocessableEntity(new { success = result.Success, error = result.Error });
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

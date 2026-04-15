using System.Diagnostics;
using System.Text.Json;
using EnterpriseAgentOs.Api.Database.Models;
using EnterpriseAgentOs.Api.Entities.ApprovalQueue;
using EnterpriseAgentOs.Api.Entities.Audit;
using EnterpriseAgentOs.Api.Entities.Events;
using EnterpriseAgentOs.Api.Entities.RateLimiting;

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
    private readonly ISystemEventService _events;
    private readonly IAuditService _audit;
    private readonly IRateLimitService _rateLimiter;
    private readonly ISkillCatalogRepository _catalog;
    private readonly ISkillRepository _skillRepo;
    private readonly IApprovalService _approvalService;

    private readonly ILogger<AgentSkillsController> _logger;

    public AgentSkillsController(
        ISkillService service,
        SkillRuntimeClient runtime,
        IRunnerRepository runners,
        IRunnerJobRepository runnerJobs,
        RunnerJobWaiter jobWaiter,
        IBrowserSessionRepository browserSessions,
        ISystemEventService events,
        IAuditService audit,
        IRateLimitService rateLimiter,
        ISkillCatalogRepository catalog,
        ISkillRepository skillRepo,
        IApprovalService approvalService,
        ILogger<AgentSkillsController> logger)
    {
        _service = service;
        _runtime = runtime;
        _runners = runners;
        _runnerJobs = runnerJobs;
        _jobWaiter = jobWaiter;
        _browserSessions = browserSessions;
        _events = events;
        _audit = audit;
        _rateLimiter = rateLimiter;
        _catalog = catalog;
        _skillRepo = skillRepo;
        _approvalService = approvalService;
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
        var execAgentId = (Guid)HttpContext.Items["agent-id"]!;

        // --- Rate limiting ---
        var generalDecision = await _rateLimiter.CheckSkillExecAsync(execAgentId, ct);
        if (!generalDecision.Allowed)
        {
            _logger.LogWarning("Rate limit exceeded for agent {AgentId} (skill_exec)", execAgentId);
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = "rate_limit_exceeded",
                retryAfterSeconds = generalDecision.RetryAfterSeconds,
            });
        }

        // Check email category rate limit if applicable
        var skillRecord = await _catalog.GetByNameAsync(body.Skill, ct);
        if (skillRecord is not null)
        {
            var manifest = DeserializeManifestSafe(skillRecord.ManifestJson);
            if (string.Equals(manifest?.Category, "email", StringComparison.OrdinalIgnoreCase))
            {
                var emailDecision = await _rateLimiter.CheckEmailAsync(execAgentId, ct);
                if (!emailDecision.Allowed)
                {
                    _logger.LogWarning("Email rate limit exceeded for agent {AgentId}", execAgentId);
                    return StatusCode(StatusCodes.Status429TooManyRequests, new
                    {
                        error = "email_rate_limit_exceeded",
                        retryAfterSeconds = emailDecision.RetryAfterSeconds,
                    });
                }
            }
        }

        // --- Approval gate ---
        var approvalRequired = await RequiresApprovalAsync(body.Skill, skillRecord, ct);
        if (approvalRequired)
        {
            var approvalParamsJson = JsonSerializer.Serialize(body.Params ?? new Dictionary<string, object>());
            var approvalRecord = await _approvalService.CreatePendingAsync(execAgentId, body.Skill, body.Action, approvalParamsJson, ct);
            _logger.LogInformation("Skill {Skill}.{Action} requires approval; created approval request {ApprovalId}",
                body.Skill, body.Action, approvalRecord.Id);
            return Accepted(new { status = "approval_pending", approvalRequestId = approvalRecord.Id });
        }

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

        var paramsJson = JsonSerializer.Serialize(body.Params ?? new Dictionary<string, object>());
        var sw = Stopwatch.StartNew();
        string? resultSummary = null;

        try
        {
            var result = await _runtime.ExecuteAsync(
                body.Skill,
                body.Action,
                body.Params ?? new Dictionary<string, object>(),
                creds,
                sessionContext,
                ct);

            sw.Stop();

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
            {
                resultSummary = "success";
                await RecordAuditAsync(execAgentId, body.Skill, body.Action, paramsJson, resultSummary, sw.ElapsedMilliseconds);
                return Ok(new { success = result.Success, result = result.Result });
            }

            resultSummary = $"error: {result.Error}";
            await RecordAuditAsync(execAgentId, body.Skill, body.Action, paramsJson, resultSummary, sw.ElapsedMilliseconds);

            await _events.RecordAsync(new SystemEventRecord
            {
                Severity = "error",
                Category = "skill_execution",
                Message = $"Skill {body.Skill}.{body.Action} failed: {result.Error}",
                SkillName = body.Skill,
                AgentId = execAgentId,
                CorrelationId = Response.Headers["X-Correlation-Id"].FirstOrDefault(),
            });
            return UnprocessableEntity(new { success = result.Success, error = result.Error });
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            resultSummary = $"error: {ex.Message}";
            await RecordAuditAsync(execAgentId, body.Skill, body.Action, paramsJson, resultSummary, sw.ElapsedMilliseconds);

            await _events.RecordAsync(new SystemEventRecord
            {
                Severity = "error",
                Category = "skill_execution",
                Message = $"Cloud skill-runtime unreachable for {body.Skill}.{body.Action}: {ex.Message}",
                SkillName = body.Skill,
                AgentId = execAgentId,
                CorrelationId = Response.Headers["X-Correlation-Id"].FirstOrDefault(),
            });
            return StatusCode(StatusCodes.Status502BadGateway,
                new { error = $"Cloud skill-runtime unreachable: {ex.Message}" });
        }
    }

    private async Task RecordAuditAsync(Guid agentId, string skill, string action, string paramsJson, string? resultSummary, long durationMs)
    {
        try
        {
            await _audit.RecordToolCallAsync(agentId, null, skill, action, paramsJson, resultSummary, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit entry for {Skill}.{Action}", skill, action);
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

            await _events.RecordAsync(new SystemEventRecord
            {
                Severity = "error",
                Category = "skill_execution",
                Message = $"Runner skill {body.Skill}.{body.Action} failed on '{runner.Name}': {result.Error}",
                SkillName = body.Skill,
                CorrelationId = Response.Headers["X-Correlation-Id"].FirstOrDefault(),
            });
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
            await _events.RecordAsync(new SystemEventRecord
            {
                Severity = "error",
                Category = "skill_execution",
                Message = $"Runner '{runner.Name}' timed out executing {body.Skill}.{body.Action} (30s limit)",
                SkillName = body.Skill,
                CorrelationId = Response.Headers["X-Correlation-Id"].FirstOrDefault(),
            });
            return StatusCode(StatusCodes.Status504GatewayTimeout, new
            {
                error = $"Runner '{runner.Name}' did not complete the job within 30 seconds. "
                    + "The runner may be overloaded, or the skill execution is taking too long. "
                    + "Check the runner logs or try again.",
            });
        }
    }

    private static readonly JsonSerializerOptions ManifestJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private static RuntimeManifest? DeserializeManifestSafe(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<RuntimeManifest>(json, ManifestJsonOptions);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves whether a skill execution requires manual approval.
    /// DB override (RequiresApprovalOverride) takes precedence over manifest flag.
    /// </summary>
    private async Task<bool> RequiresApprovalAsync(string skillName, SkillRecord? skillRecord, CancellationToken ct)
    {
        // Check DB override first
        var credRow = await _skillRepo.GetByNameAsync(skillName, ct);
        if (credRow?.RequiresApprovalOverride.HasValue == true)
            return credRow.RequiresApprovalOverride.Value;

        // Fall back to manifest flag
        var manifest = DeserializeManifestSafe(skillRecord?.ManifestJson);
        return manifest?.RequiresApproval ?? false;
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

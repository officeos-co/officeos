namespace EnterpriseAgentOs.Api.Entities.Skills;

[ApiController]
[Route("api/agents/me")]
[EnterpriseAgentOs.Api.Entities.Skills.AgentTokenAuth]
public sealed class AgentSkillsController : ControllerBase
{
    private readonly ISkillService _service;
    private readonly SkillRuntimeClient _runtime;
    private readonly IBrowserSessionRepository _browserSessions;
    private readonly EnterpriseAgentOs.Api.Entities.AgentLogs.IAgentLogService _agentLogs;
    private readonly EnterpriseAgentOs.Api.Entities.RateLimiting.IRateLimitService _rateLimiter;
    private readonly ISkillCatalogRepository _catalog;

    private readonly ILogger<AgentSkillsController> _logger;

    public AgentSkillsController(
        ISkillService service,
        SkillRuntimeClient runtime,
        IBrowserSessionRepository browserSessions,
        EnterpriseAgentOs.Api.Entities.AgentLogs.IAgentLogService agentLogs,
        EnterpriseAgentOs.Api.Entities.RateLimiting.IRateLimitService rateLimiter,
        ISkillCatalogRepository catalog,
        ILogger<AgentSkillsController> logger)
    {
        _service = service;
        _runtime = runtime;
        _browserSessions = browserSessions;
        _agentLogs = agentLogs;
        _rateLimiter = rateLimiter;
        _catalog = catalog;
        _logger = logger;
    }

    [HttpGet("capabilities")]
    public async Task<ActionResult<CapabilitiesResponse>> Capabilities(CancellationToken ct)
    {
        var agentId = (Guid)HttpContext.Items["agent-id"]!;
        var response = await _service.ListCapabilitiesAsync(agentId, ct);
        return Ok(response);
    }

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

        _logger.LogInformation("Agent skill-exec: {Skill}.{Action}", body.Skill, body.Action);

        var creds = await _service.GetDecryptedCredentialsAsync(body.Skill, ct);
        if (creds is null)
        {
            _logger.LogWarning("Skill {Skill} not configured for cloud execution", body.Skill);
            return Conflict(new { error = $"Skill '{body.Skill}' is not installed or not configured. Configure credentials on the Skills page." });
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

            if (result.Success)
            {
                resultSummary = "success";
                await RecordAuditAsync(execAgentId, body.Skill, body.Action, paramsJson, resultSummary, sw.ElapsedMilliseconds);
                return Ok(new { success = result.Success, result = result.Result });
            }

            resultSummary = $"error: {result.Error}";
            await RecordAuditAsync(execAgentId, body.Skill, body.Action, paramsJson, resultSummary, sw.ElapsedMilliseconds);

            await _agentLogs.AppendAsync(new EnterpriseAgentOs.Api.Database.Models.AgentLogRecord
            {
                AgentId = execAgentId,
                Type = EnterpriseAgentOs.Api.Database.Models.AgentLogType.System,
                Content = $"Skill {body.Skill}.{body.Action} failed: {result.Error}",
                Integration = body.Skill,
                Tool = body.Action,
                CorrelationId = Response.Headers["X-Correlation-Id"].FirstOrDefault(),
            });
            return UnprocessableEntity(new { success = result.Success, error = result.Error });
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            resultSummary = $"error: {ex.Message}";
            await RecordAuditAsync(execAgentId, body.Skill, body.Action, paramsJson, resultSummary, sw.ElapsedMilliseconds);

            await _agentLogs.AppendAsync(new EnterpriseAgentOs.Api.Database.Models.AgentLogRecord
            {
                AgentId = execAgentId,
                Type = EnterpriseAgentOs.Api.Database.Models.AgentLogType.System,
                Content = $"Cloud skill-runtime unreachable for {body.Skill}.{body.Action}: {ex.Message}",
                Integration = body.Skill,
                Tool = body.Action,
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
            await _agentLogs.RecordToolCallAsync(agentId, null, skill, action, paramsJson, resultSummary, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit entry for {Skill}.{Action}", skill, action);
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

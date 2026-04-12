using System.Security.Cryptography;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;

namespace EnterpriseAgentOs.Api.Entities.Runners;

/// <summary>
/// Runner-facing API (polling endpoints). Registration uses token hash lookup;
/// all other endpoints use RunnerAuth bearer token.
/// </summary>
[ApiController]
[Route("api/runner")]
public sealed class RunnerApiController : ControllerBase
{
    private readonly IRunnerRepository _runners;
    private readonly IRunnerJobRepository _jobs;
    private readonly RunnerJobWaiter _waiter;
    private readonly ICustomSkillRepository _customSkills;
    private readonly IAmazonS3 _s3;
    private readonly SkillStorageConfig _storage;

    private readonly ILogger<RunnerApiController> _logger;

    public RunnerApiController(
        IRunnerRepository runners,
        IRunnerJobRepository jobs,
        RunnerJobWaiter waiter,
        ICustomSkillRepository customSkills,
        IAmazonS3 s3,
        SkillStorageConfig storage,
        ILogger<RunnerApiController> logger)
    {
        _runners = runners;
        _jobs = jobs;
        _waiter = waiter;
        _customSkills = customSkills;
        _s3 = s3;
        _storage = storage;
        _logger = logger;
    }

    private RunnerRecord? CurrentRunner => HttpContext.Items["Runner"] as RunnerRecord;

    /// <summary>
    /// Runner registers with its one-time registration token.
    /// Returns a long-lived auth token for subsequent API calls.
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRunnerRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.RegistrationToken))
            return BadRequest(new { error = "registrationToken is required" });

        var hash = Auth.SessionAuthMiddleware.HashToken(body.RegistrationToken);
        var runner = await _runners.GetByRegistrationTokenHashAsync(hash, ct);

        if (runner is null)
        {
            _logger.LogWarning("Runner registration failed: invalid token");
            return Unauthorized(new { error = "Invalid registration token" });
        }

        if (runner.Status != "pending")
        {
            _logger.LogWarning("Runner {RunnerId} registration rejected: already registered (status={Status})",
                runner.Id, runner.Status);
            return Conflict(new { error = "Runner already registered" });
        }

        // Issue auth token
        var authToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var authHash = Auth.SessionAuthMiddleware.HashToken(authToken);

        runner.AuthTokenHash = authHash;
        runner.Status = "online";
        runner.LastHeartbeatAt = DateTime.UtcNow;
        runner.Version = body.Version;
        await _runners.UpdateAsync(runner, ct);

        _logger.LogInformation("Runner {RunnerId} ({RunnerName}) registered successfully", runner.Id, runner.Name);

        return Ok(new
        {
            authToken,
            runnerId = runner.Id,
            name = runner.Name,
        });
    }

    /// <summary>
    /// Runner polls for pending jobs. Returns at most 1 job (atomically claimed).
    /// </summary>
    [HttpGet("jobs")]
    [RunnerAuth]
    public async Task<IActionResult> PollJobs(CancellationToken ct)
    {
        var runner = CurrentRunner!;
        var job = await _jobs.ClaimNextAsync(runner.Id, ct);

        if (job is null)
            return NoContent();

        _logger.LogInformation("Runner {RunnerId} claimed job {JobId}", runner.Id, job.Id);

        return Ok(new
        {
            id = job.Id,
            payload = JsonSerializer.Deserialize<JsonElement>(job.Payload),
        });
    }

    /// <summary>
    /// Runner posts the result of a completed job.
    /// </summary>
    [HttpPost("jobs/{jobId:guid}/result")]
    [RunnerAuth]
    public async Task<IActionResult> PostResult(Guid jobId, [FromBody] JobResultRequest body, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(jobId, ct);
        if (job is null || job.RunnerId != CurrentRunner!.Id)
            return NotFound();

        job.Status = body.Success ? "completed" : "failed";
        job.Result = JsonSerializer.Serialize(new { body.Success, body.Result, body.Error });
        job.CompletedAt = DateTime.UtcNow;
        await _jobs.UpdateAsync(job, ct);

        _logger.LogInformation("Job {JobId} completed by runner {RunnerId} (success={Success})",
            jobId, CurrentRunner!.Id, body.Success);

        // Notify the waiter so skill_exec can return
        _waiter.Complete(jobId, new RunnerJobResult
        {
            Success = body.Success,
            Result = body.Result,
            Error = body.Error,
        });

        return Ok(new { ok = true });
    }

    [HttpPost("heartbeat")]
    [RunnerAuth]
    public async Task<IActionResult> Heartbeat(CancellationToken ct)
    {
        var runner = CurrentRunner!;
        runner.LastHeartbeatAt = DateTime.UtcNow;
        runner.Status = "online";
        await _runners.UpdateAsync(runner, ct);
        return Ok(new { ok = true });
    }

    /// <summary>
    /// Returns the list of custom skills available for this runner to sync.
    /// Each entry includes name and lastSyncAt so the runner can skip unchanged skills.
    /// </summary>
    [HttpGet("skills")]
    [RunnerAuth]
    public async Task<IActionResult> ListSkills(CancellationToken ct)
    {
        var skills = await _customSkills.ListAsync(ct);
        var ready = skills
            .Where(s => s.BuildStatus == "ready" && !string.IsNullOrEmpty(s.BundlePath))
            .Select(s => new
            {
                name = s.Name,
                updatedAt = s.LastSyncAt ?? s.CreatedAt,
            });
        return Ok(ready);
    }

    /// <summary>
    /// Streams the skill zip from MinIO so the runner can download and build it locally.
    /// </summary>
    [HttpGet("skills/{name}/download")]
    [RunnerAuth]
    public async Task<IActionResult> DownloadSkill(string name, CancellationToken ct)
    {
        var skill = await _customSkills.GetByNameAsync(name, ct);
        if (skill is null || skill.BuildStatus != "ready" || string.IsNullOrEmpty(skill.BundlePath))
            return NotFound(new { error = $"Skill '{name}' not found or not ready" });

        var s3Key = $"{name}/{name}.zip";
        try
        {
            var response = await _s3.GetObjectAsync(new GetObjectRequest
            {
                BucketName = _storage.Bucket,
                Key = s3Key,
            }, ct);

            return File(response.ResponseStream, "application/zip", $"{name}.zip");
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return NotFound(new { error = $"Skill archive not found in storage" });
        }
    }
}

public sealed record RegisterRunnerRequest(string RegistrationToken, string? Version = null);
public sealed record JobResultRequest(bool Success, JsonElement? Result = null, string? Error = null);

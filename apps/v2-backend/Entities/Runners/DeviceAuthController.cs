using System.Security.Cryptography;

namespace EnterpriseAgentOs.Api.Entities.Runners;

/// <summary>
/// RFC 8628 Device Authorization Grant flow for runner registration.
/// Runner CLI calls /device/code, displays user_code, polls /device/token.
/// User visits /device/verify in browser, approves with session auth.
/// </summary>
[ApiController]
[Route("api/runner/device")]
public sealed class DeviceAuthController : ControllerBase
{
    private readonly EaosDbContext _db;
    private readonly IRunnerRepository _runners;

    private static readonly TimeSpan CodeLifetime = TimeSpan.FromMinutes(15);
    private const int PollIntervalSeconds = 5;

    private readonly ILogger<DeviceAuthController> _logger;

    public DeviceAuthController(EaosDbContext db, IRunnerRepository runners, ILogger<DeviceAuthController> logger)
    {
        _db = db;
        _runners = runners;
        _logger = logger;
    }

    private UserRecord? CurrentUser => HttpContext.Items["User"] as UserRecord;

    /// <summary>
    /// Runner CLI calls this to start the device flow.
    /// Returns a device_code (high entropy, kept secret by runner),
    /// a user_code (short, displayed to user), and a verification URL.
    /// </summary>
    [HttpPost("code")]
    public async Task<IActionResult> RequestCode([FromBody] DeviceCodeRequest body, CancellationToken ct)
    {
        var deviceCode = Guid.NewGuid().ToString("N");
        var userCode = GenerateUserCode();

        var record = new DeviceCodeRecord
        {
            DeviceCode = deviceCode,
            UserCode = userCode,
            RunnerName = body.RunnerName?.Trim(),
            ExpiresAt = DateTime.UtcNow.Add(CodeLifetime),
        };

        _db.DeviceCodes.Add(record);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Device code issued: user_code={UserCode} runner={RunnerName}",
            FormatUserCode(userCode), body.RunnerName);

        var host = $"{Request.Scheme}://{Request.Host}";

        return Ok(new
        {
            device_code = deviceCode,
            user_code = userCode,
            verification_uri = $"{host}/api/runner/device/verify",
            expires_in = (int)CodeLifetime.TotalSeconds,
            interval = PollIntervalSeconds,
        });
    }

    /// <summary>
    /// Runner CLI polls this with device_code until the user approves.
    /// Returns authorization_pending, slow_down, expired, or the auth token.
    /// </summary>
    [HttpPost("token")]
    public async Task<IActionResult> PollToken([FromBody] DeviceTokenRequest body, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(body.DeviceCode))
            return BadRequest(new { error = "device_code is required" });

        var record = await _db.DeviceCodes
            .FirstOrDefaultAsync(d => d.DeviceCode == body.DeviceCode, ct);

        if (record is null)
            return BadRequest(new { error = "invalid_device_code" });

        if (record.ExpiresAt < DateTime.UtcNow)
        {
            record.Status = "expired";
            await _db.SaveChangesAsync(ct);
            return BadRequest(new { error = "expired_token" });
        }

        // Rate limit: if polled less than 5s ago, return slow_down
        if (record.LastPolledAt is not null &&
            (DateTime.UtcNow - record.LastPolledAt.Value).TotalSeconds < PollIntervalSeconds)
        {
            return StatusCode(428, new { error = "slow_down" });
        }

        record.LastPolledAt = DateTime.UtcNow;

        if (record.Status == "pending")
        {
            await _db.SaveChangesAsync(ct);
            return Ok(new { error = "authorization_pending" });
        }

        if (record.Status != "approved" || record.UserId is null)
        {
            await _db.SaveChangesAsync(ct);
            return BadRequest(new { error = "access_denied" });
        }

        // Approved — create the runner and issue an auth token
        var runnerName = record.RunnerName ?? $"runner-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var registrationTokenHash = SessionAuthMiddleware.HashToken(Guid.NewGuid().ToString());
        var runner = await _runners.CreateAsync(record.UserId.Value, runnerName, registrationTokenHash, ct);

        var authToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var authHash = SessionAuthMiddleware.HashToken(authToken);

        runner.AuthTokenHash = authHash;
        runner.Status = "online";
        runner.LastHeartbeatAt = DateTime.UtcNow;
        await _runners.UpdateAsync(runner, ct);

        // Mark device code as consumed
        record.Status = "consumed";
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Device flow completed: runner {RunnerId} ({RunnerName}) created for user {UserId}",
            runner.Id, runner.Name, record.UserId);

        return Ok(new
        {
            auth_token = authToken,
            runner_id = runner.Id,
            name = runner.Name,
        });
    }

    /// <summary>
    /// User visits this in their browser to see the approval page.
    /// Returns JSON with the code details (frontend renders the approve button).
    /// </summary>
    [HttpGet("verify")]
    public async Task<IActionResult> Verify([FromQuery] string? code, CancellationToken ct)
    {
        if (CurrentUser is null)
            return Unauthorized(new { error = "Please log in to approve the runner" });

        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { error = "code query parameter is required" });

        var normalized = code.Trim().ToUpperInvariant().Replace("-", "");
        var record = await _db.DeviceCodes
            .Where(d => d.UserCode == normalized && d.Status == "pending" && d.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (record is null)
            return NotFound(new { error = "Invalid or expired code" });

        return Ok(new
        {
            user_code = FormatUserCode(record.UserCode),
            runner_name = record.RunnerName,
            expires_in = (int)(record.ExpiresAt - DateTime.UtcNow).TotalSeconds,
        });
    }

    /// <summary>
    /// User clicks approve in the browser — links their session to the device code.
    /// </summary>
    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody] DeviceApproveRequest body, CancellationToken ct)
    {
        if (CurrentUser is null)
            return Unauthorized(new { error = "Please log in to approve the runner" });

        if (string.IsNullOrWhiteSpace(body.UserCode))
            return BadRequest(new { error = "user_code is required" });

        var normalized = body.UserCode.Trim().ToUpperInvariant().Replace("-", "");
        var record = await _db.DeviceCodes
            .Where(d => d.UserCode == normalized && d.Status == "pending" && d.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync(ct);

        if (record is null)
            return NotFound(new { error = "Invalid or expired code" });

        record.Status = "approved";
        record.UserId = CurrentUser.Id;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Device code {UserCode} approved by user {UserId}",
            FormatUserCode(normalized), CurrentUser.Id);

        return Ok(new { approved = true });
    }

    /// <summary>
    /// Generates an 8-character alphanumeric user code (stored without dash, displayed as XXXX-XXXX).
    /// Uses only uppercase letters and digits, excluding ambiguous characters (0/O, 1/I/L).
    /// </summary>
    private static string GenerateUserCode()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
        return string.Create(8, chars, static (span, c) =>
        {
            Span<byte> random = stackalloc byte[8];
            RandomNumberGenerator.Fill(random);
            for (int i = 0; i < span.Length; i++)
                span[i] = c[random[i] % c.Length];
        });
    }

    private static string FormatUserCode(string code) =>
        code.Length == 8 ? $"{code[..4]}-{code[4..]}" : code;
}

public sealed record DeviceCodeRequest(string? RunnerName = null);
public sealed record DeviceTokenRequest(string DeviceCode);
public sealed record DeviceApproveRequest(string UserCode);

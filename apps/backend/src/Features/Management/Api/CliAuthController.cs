namespace OffceOs.Api.Features.Management;

[ApiController]
[Route("api/cli")]
public sealed class CliAuthController : ControllerBase
{
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return Unauthorized(new { error = "Unauthenticated." });

        return Ok(new CliMePayload(user.Id, user.Email, user.Name, user.DisplayName));
    }

    [HttpPost("device/code")]
    public async Task<ActionResult<CliDeviceCodeResult>> CreateDeviceCode(
        [FromBody] CliDeviceCodeInput? input,
        [FromServices] ICliAuthService cliAuth,
        CancellationToken ct)
    {
        var result = await cliAuth.CreateDeviceCodeAsync(new CliDeviceCodeRequest(input?.RunnerName), ct);
        return Ok(result);
    }

    [HttpPost("device/authorize")]
    public async Task<IActionResult> AuthorizeDeviceCode(
        [FromBody] CliDeviceAuthorizeInput input,
        [FromServices] ICliAuthService cliAuth,
        CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return Unauthorized(new { error = "Sign in before authorizing the CLI." });

        try
        {
            await cliAuth.AuthorizeDeviceCodeAsync(input.UserCode, user.Id, ct);
            return Ok(new { ok = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("device/token")]
    public async Task<ActionResult<CliDeviceTokenResult>> PollToken(
        [FromBody] CliDeviceTokenInput input,
        [FromServices] ICliAuthService cliAuth,
        CancellationToken ct)
    {
        try
        {
            return Ok(await cliAuth.PollTokenAsync(input.DeviceCode, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public sealed record CliDeviceCodeInput(string? RunnerName);
public sealed record CliDeviceAuthorizeInput(string UserCode);
public sealed record CliDeviceTokenInput(string DeviceCode);
public sealed record CliMePayload(Guid Id, string Email, string? Name, string? DisplayName);

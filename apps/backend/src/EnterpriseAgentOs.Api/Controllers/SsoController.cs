namespace EnterpriseAgentOs.Api.Controllers;

[ApiController]
[Route("api/sso")]
public sealed class SsoController : ControllerBase
{
    private readonly WorkOsConfig _workOsConfig;
    private readonly IWorkOsAuthService _workOsAuthService;
    private readonly ILogger<SsoController> _logger;

    public SsoController(
        WorkOsConfig config,
        IWorkOsAuthService workOs,
        ILogger<SsoController> logger)
    {
        _workOsConfig = config;
        _workOsAuthService = workOs;
        _logger = logger;
    }

    [HttpGet("initiate")]
    public async Task<IActionResult> Initiate([FromQuery] string? org, CancellationToken ct)
    {
        if (!_workOsConfig.Enabled)
        {
            return StatusCode(503, new { error = "SSO not configured" });
        }

        if (string.IsNullOrWhiteSpace(org))
        {
            return BadRequest(new { error = "org query parameter is required" });
        }

        var redirectUrl = await _workOsAuthService.InitiateSsoAsync(org, ct);

        _logger.LogInformation("SSO initiation redirect for org {OrgId}", org);

        return Redirect(redirectUrl);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        CancellationToken ct)
    {
        if (!_workOsConfig.Enabled)
        {
            return StatusCode(503, new { error = "SSO not configured" });
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            return BadRequest(new { error = "code and state query parameters are required" });
        }

        var userInfo = await _workOsAuthService.HandleCallbackAsync(code, state, ct);

        _logger.LogInformation("SSO callback succeeded for user {Email} in org {OrgId}",
            userInfo.Email, userInfo.OrganizationId);

        // TODO: Issue a session cookie using the same mechanism as AuthController.
        return Ok(new
        {
            email = userInfo.Email,
            organizationId = userInfo.OrganizationId,
        });
    }
}

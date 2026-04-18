namespace EnterpriseAgentOs.Api.Controllers;

[ApiController]
[Route("api/scim/v2")]
public sealed class ScimController : ControllerBase
{
    private readonly EnterpriseAgentOs.Infrastructure.Configuration.WorkOsConfig _config;
    private readonly IWorkOsAuthService _workOs;
    private readonly ILogger<ScimController> _logger;

    public ScimController(
        EnterpriseAgentOs.Infrastructure.Configuration.WorkOsConfig config,
        IWorkOsAuthService workOs,
        ILogger<ScimController> logger)
    {
        _config = config;
        _workOs = workOs;
        _logger = logger;
    }

    [HttpPost("Users")]
    public async Task<IActionResult> ProvisionUser(
        [FromBody] ScimUserPayload payload,
        CancellationToken ct)
    {
        if (!_config.Enabled)
        {
            return StatusCode(503, new { error = "SSO not configured" });
        }

        var evt = new ScimEvent(
            EventType: "provision",
            ExternalId: payload.ExternalId,
            Email: payload.Email,
            DisplayName: payload.DisplayName
        );

        await _workOs.HandleScimProvisionAsync(evt, ct);

        _logger.LogInformation("SCIM: provisioned user {ExternalId}", payload.ExternalId);

        return StatusCode(201, new { externalId = payload.ExternalId });
    }

    [HttpDelete("Users/{id}")]
    public async Task<IActionResult> DeprovisionUser(string id, CancellationToken ct)
    {
        if (!_config.Enabled)
        {
            return StatusCode(503, new { error = "SSO not configured" });
        }

        var evt = new ScimEvent(
            EventType: "deprovision",
            ExternalId: id,
            Email: null,
            DisplayName: null
        );

        await _workOs.HandleScimProvisionAsync(evt, ct);

        _logger.LogInformation("SCIM: deprovisioned user {ExternalId}", id);

        return NoContent();
    }
}

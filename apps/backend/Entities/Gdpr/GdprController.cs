using System.Text.Json;
using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.Gdpr;

[ApiController]
[Route("api/gdpr")]
public sealed class GdprController : ControllerBase
{
    private readonly IGdprService _gdpr;

    public GdprController(IGdprService gdpr)
    {
        _gdpr = gdpr;
    }

    private UserRecord? CurrentUser => HttpContext.Items["User"] as UserRecord;

    /// <summary>
    /// GET /api/gdpr/export
    /// Returns a full JSON export of all data for the authenticated user.
    /// Responds with Content-Disposition: attachment so browsers trigger a download.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        if (CurrentUser is null)
            return Unauthorized();

        var export = await _gdpr.ExportAsync(CurrentUser.Id, ct);

        var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        });

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        Response.Headers["Content-Disposition"] = "attachment; filename=\"eaos-export.json\"";
        return File(bytes, "application/json", "eaos-export.json");
    }

    /// <summary>
    /// DELETE /api/gdpr/purge
    /// Permanently deletes all data owned by the authenticated user, including their
    /// session (making the current cookie invalid) and user record.
    /// </summary>
    [HttpDelete("purge")]
    public async Task<IActionResult> Purge(CancellationToken ct)
    {
        if (CurrentUser is null)
            return Unauthorized();

        await _gdpr.PurgeAsync(CurrentUser.Id, ct);

        return NoContent();
    }
}

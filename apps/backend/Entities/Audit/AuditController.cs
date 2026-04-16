using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.Audit;

[ApiController]
[Route("api/agents/{agentId:guid}")]
public sealed class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    private UserRecord? CurrentUser => HttpContext.Items["User"] as UserRecord;

    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        Guid agentId,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        if (CurrentUser is null)
            return Unauthorized();

        if (limit < 1) limit = 1;
        if (limit > 100) limit = 100;
        if (offset < 0) offset = 0;

        var (items, total) = await _auditService.GetAuditLogAsync(agentId, limit, offset);

        var dtos = items.Select(r => new AuditLogEntryDto(
            r.Id,
            r.AgentId,
            null,
            r.Integration ?? string.Empty,
            r.Tool ?? string.Empty,
            r.Content,
            null,
            r.DurationMs ?? 0,
            r.Time)).ToList();

        return Ok(new AuditLogResponse(dtos, total));
    }
}

using System.Text.Json;
using EnterpriseAgentOs.Api.Database.Models;
using EnterpriseAgentOs.Api.Entities.Skills;

namespace EnterpriseAgentOs.Api.Entities.ApprovalQueue;

/// <summary>
/// Dashboard endpoints for the manual approval queue.
/// </summary>
[ApiController]
[Route("api/approval-requests")]
public sealed class ApprovalController : ControllerBase
{
    private readonly IApprovalService _service;
    private readonly ISkillRepository _skillRepo;

    public ApprovalController(IApprovalService service, ISkillRepository skillRepo)
    {
        _service = service;
        _skillRepo = skillRepo;
    }

    private UserRecord? CurrentUser => HttpContext.Items["User"] as UserRecord;

    // GET /api/approval-requests — list pending (dashboard auth)
    [HttpGet]
    public async Task<IActionResult> ListPending(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken ct = default)
    {
        if (CurrentUser is null) return Unauthorized();

        var records = await _service.GetPendingAsync(limit, offset, ct);
        var dtos = records.Select(ToDto).ToList();
        return Ok(dtos);
    }

    // GET /api/approval-requests/{id} — get by id (dashboard auth)
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        if (CurrentUser is null) return Unauthorized();

        var record = await _service.GetByIdAsync(id, ct);
        if (record is null) return NotFound();

        return Ok(ToDto(record));
    }

    // POST /api/approval-requests/{id}/approve — approve and execute (dashboard auth)
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct = default)
    {
        if (CurrentUser is null) return Unauthorized();

        var record = await _service.ApproveAndExecuteAsync(id, CurrentUser.Id, ct);
        if (record is null) return NotFound();

        return Ok(ToDto(record));
    }

    // POST /api/approval-requests/{id}/reject — reject (dashboard auth)
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct = default)
    {
        if (CurrentUser is null) return Unauthorized();

        var record = await _service.RejectAsync(id, CurrentUser.Id, ct);
        if (record is null) return NotFound();

        return Ok(ToDto(record));
    }

    // GET /api/agents/{agentId}/approval-requests — per-agent list (dashboard auth)
    [HttpGet("/api/agents/{agentId:guid}/approval-requests")]
    public async Task<IActionResult> GetByAgent(Guid agentId, CancellationToken ct = default)
    {
        if (CurrentUser is null) return Unauthorized();

        var records = await _service.GetByAgentAsync(agentId, ct);
        var dtos = records.Select(ToDto).ToList();
        return Ok(dtos);
    }

    private static ApprovalRequestDto ToDto(ApprovalRequestRecord r) =>
        new(r.Id, r.AgentId, r.SkillName, r.Action, r.ParamsJson,
            r.Status, r.RequestedAt, r.DecidedAt, r.DecidedByUserId, r.ResultJson);
}

/// <summary>
/// Agent-facing endpoint for polling approval request status (bearer auth).
/// </summary>
[ApiController]
[Route("api/agents/me")]
[AgentTokenAuth]
public sealed class AgentApprovalController : ControllerBase
{
    private readonly IApprovalService _service;

    public AgentApprovalController(IApprovalService service)
    {
        _service = service;
    }

    // GET /api/agents/me/approval-requests/{id} — agent polls for result (agent bearer auth)
    [HttpGet("approval-requests/{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct = default)
    {
        var agentId = (Guid)HttpContext.Items["agent-id"]!;

        var record = await _service.GetByIdAsync(id, ct);
        if (record is null) return NotFound();

        // Agents can only see their own approval requests
        if (record.AgentId != agentId) return Forbid();

        // Parse resultJson to provide a clean `result` field for the agent
        JsonElement? resultElement = null;
        if (!string.IsNullOrEmpty(record.ResultJson))
        {
            try
            {
                var parsed = JsonDocument.Parse(record.ResultJson);
                if (parsed.RootElement.TryGetProperty("result", out var r))
                    resultElement = r;
                else
                    resultElement = parsed.RootElement;
            }
            catch { /* return null result */ }
        }

        return Ok(new ApprovalRequestAgentDto(
            record.Id,
            record.AgentId,
            record.SkillName,
            record.Action,
            record.Status,
            record.RequestedAt,
            record.DecidedAt,
            resultElement));
    }
}

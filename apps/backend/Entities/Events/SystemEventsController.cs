using System.Text.Json;
using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.Events;

[ApiController]
[Route("api/system-events")]
public sealed class SystemEventsController : ControllerBase
{
    private readonly ISystemEventService _events;
    private readonly SystemEventBroadcaster _broadcaster;

    public SystemEventsController(ISystemEventService events, SystemEventBroadcaster broadcaster)
    {
        _events = events;
        _broadcaster = broadcaster;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SystemEventRecord>>> List(
        [FromQuery] int limit = 50,
        [FromQuery] string? severity = null,
        [FromQuery] string? category = null,
        CancellationToken ct = default)
    {
        var events = await _events.ListRecentAsync(limit, severity, category, ct);
        return Ok(events);
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.Connection = "keep-alive";

        var reader = _broadcaster.Subscribe();

        try
        {
            await foreach (var ev in reader.ReadAllAsync(ct))
            {
                var json = JsonSerializer.Serialize(new
                {
                    id = ev.Id,
                    severity = ev.Severity,
                    category = ev.Category,
                    message = ev.Message,
                    skillName = ev.SkillName,
                    agentId = ev.AgentId,
                    correlationId = ev.CorrelationId,
                    createdAt = ev.CreatedAt,
                });
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            _broadcaster.Unsubscribe(reader);
        }
    }

    [HttpPost("{id:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct)
    {
        await _events.AcknowledgeAsync(id, ct);
        return NoContent();
    }
}

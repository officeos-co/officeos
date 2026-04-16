namespace EnterpriseAgentOs.Api.Entities.Events;

/// <summary>
/// System events REST surface. Dashboard list/acknowledge has moved to GraphQL.
/// Only the SSE stream endpoint — consumed by the dashboard for live push —
/// remains as REST (SSE is not a natural fit for HotChocolate subscriptions
/// when the consumer is a plain EventSource).
/// </summary>
[ApiController]
[Route("api/system-events")]
public sealed class SystemEventsController : ControllerBase
{
    private readonly SystemEventBroadcaster _broadcaster;

    public SystemEventsController(SystemEventBroadcaster broadcaster)
    {
        _broadcaster = broadcaster;
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
}

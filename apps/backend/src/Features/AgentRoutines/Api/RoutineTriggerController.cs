namespace OffceOs.Api.Features.AgentRoutines;

[ApiController]
[Route("api/agent-routines")]
public sealed class RoutineTriggerController : ControllerBase
{
    private const string SecretHeader = "X-Agent-Routine-Secret";

    [HttpPost("triggers/{triggerId:guid}/invoke")]
    public async Task<IActionResult> InvokeApiTrigger(
        Guid triggerId,
        [FromServices] IAgentRoutineExecutionService executionService,
        CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue(SecretHeader, out var secret) || string.IsNullOrWhiteSpace(secret))
            return Unauthorized();

        string payload;
        using (var reader = new StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync(ct);

        var result = await executionService.ExecuteApiTriggerAsync(triggerId, secret.ToString(), payload, ct);
        return result.TriggeredCount == 0
            ? Unauthorized()
            : Ok(new { triggeredCount = result.TriggeredCount, routineIds = result.RoutineIds });
    }

    [HttpPost("github/webhook")]
    public async Task<IActionResult> GitHubWebhook(
        [FromServices] IAgentRoutineExecutionService executionService,
        CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue("X-GitHub-Event", out var githubEvent) || string.IsNullOrWhiteSpace(githubEvent))
            return BadRequest(new { error = "Missing X-GitHub-Event header" });

        if (!Request.Headers.TryGetValue("X-Hub-Signature-256", out var signature) || string.IsNullOrWhiteSpace(signature))
            return Unauthorized();

        string payload;
        using (var reader = new StreamReader(Request.Body))
            payload = await reader.ReadToEndAsync(ct);

        var result = await executionService.ExecuteGitHubWebhookAsync(
            new GitHubRoutineWebhookRequest(githubEvent.ToString(), signature.ToString(), payload),
            ct);
        return Ok(new { received = true, triggeredCount = result.TriggeredCount, routineIds = result.RoutineIds });
    }
}

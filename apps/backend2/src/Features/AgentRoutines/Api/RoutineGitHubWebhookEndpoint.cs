namespace OffceOs.Api.Features.AgentRoutines;

public static class RoutineGitHubWebhookEndpoint
{
    public static async Task<IResult> Handle(
        HttpRequest request,
        IAgentRoutineExecutionService executionService,
        CancellationToken ct)
    {
        if (!request.Headers.TryGetValue("X-GitHub-Event", out var githubEvent) || string.IsNullOrWhiteSpace(githubEvent))
            return Results.BadRequest(new { error = "Missing X-GitHub-Event header" });

        if (!request.Headers.TryGetValue("X-Hub-Signature-256", out var signature) || string.IsNullOrWhiteSpace(signature))
            return Results.Unauthorized();

        string payload;
        using (var reader = new StreamReader(request.Body))
            payload = await reader.ReadToEndAsync(ct);

        var result = await executionService.ExecuteGitHubWebhookAsync(new GitHubRoutineWebhookRequest(githubEvent.ToString(), signature.ToString(), payload), ct);
        return Results.Ok(new { received = true, triggeredCount = result.TriggeredCount, routineIds = result.RoutineIds });
    }
}

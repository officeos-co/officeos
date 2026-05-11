namespace OffceOs.Api.Features.AgentRoutines;

public static class RoutineApiEndpoint
{
    private const string SecretHeader = "X-Agent-Routine-Secret";

    public static async Task<IResult> Handle(
        Guid triggerId,
        HttpRequest request,
        IAgentRoutineExecutionService executionService,
        CancellationToken ct)
    {
        if (!request.Headers.TryGetValue(SecretHeader, out var secret) || string.IsNullOrWhiteSpace(secret))
            return Results.Unauthorized();

        string payload;
        using (var reader = new StreamReader(request.Body))
            payload = await reader.ReadToEndAsync(ct);

        var result = await executionService.ExecuteApiTriggerAsync(triggerId, secret.ToString(), payload, ct);
        return result.TriggeredCount == 0
            ? Results.Unauthorized()
            : Results.Ok(new { triggeredCount = result.TriggeredCount, routineIds = result.RoutineIds });
    }
}

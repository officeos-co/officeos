namespace OffceOs.Application.Features.AgentRoutines;

internal sealed class AgentRoutineExecutionService : IAgentRoutineExecutionService
{
    private readonly IAgentRoutineRepository _agentRoutineRepository;
    private readonly IAgentLogService _agentLogService;
    private readonly IAgentService _agentService;
    private readonly CredentialProtector _credentialProtector;
    private readonly IPublisher _publisher;
    private readonly ILogger<AgentRoutineExecutionService> _logger;

    public AgentRoutineExecutionService(
        IAgentRoutineRepository agentRoutineRepository,
        IAgentLogService agentLogService,
        IAgentService agentService,
        CredentialProtector credentialProtector,
        IPublisher publisher,
        ILogger<AgentRoutineExecutionService> logger)
    {
        _agentRoutineRepository = agentRoutineRepository;
        _agentLogService = agentLogService;
        _agentService = agentService;
        _credentialProtector = credentialProtector;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<AgentRoutineExecutionResult> RunDueSchedulesAsync(DateTime now, CancellationToken ct = default)
    {
        var routines = await _agentRoutineRepository.ListAllEnabledForExecutionAsync(ct);
        var fired = new List<Guid>();

        foreach (var execution in routines)
        {
            var routine = execution.Routine;
            foreach (var trigger in routine.Triggers.Where(trigger => trigger.Enabled && trigger.Kind == AgentRoutineTriggerKinds.Schedule))
            {
                try
                {
                    var expression = GetScheduleExpression(trigger);
                    if (trigger.NextRunAt is null)
                    {
                        trigger.SetNextRun(NextOccurrence(expression, now));
                        await _agentRoutineRepository.UpsertAsync(routine, ct);
                        continue;
                    }

                    if (trigger.NextRunAt > now)
                        continue;

                    await ExecuteAsync(routine, trigger, now, null, execution.WorkspaceId, ct);
                    trigger.SetNextRun(NextOccurrence(expression, now));
                    await _agentRoutineRepository.UpsertAsync(routine, ct);
                    fired.Add(routine.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute scheduled routine trigger {TriggerId}", trigger.Id);
                    var error = new AgentError(AgentErrorCategory.TurnOrchestration, $"Routine trigger '{trigger.Name}' failed: {ex.Message}", ex.ToString());
                    await _agentLogService.AppendAsync(error.ToLogRecord(routine.AgentId), ct);
                    await _publisher.Publish(new RoutineTriggerFailedEvent(
                        routine.Id,
                        routine.Name,
                        routine.AgentId,
                        execution.WorkspaceId,
                        trigger.Id,
                        trigger.Name,
                        trigger.Kind,
                        ex.Message), ct);
                }
            }
        }

        return new AgentRoutineExecutionResult(fired.Count, fired);
    }

    public async Task<AgentRoutineExecutionResult> ExecuteApiTriggerAsync(Guid triggerId, string secret, string? payloadJson, CancellationToken ct = default)
    {
        var trigger = await _agentRoutineRepository.GetTriggerByAsync(triggerId, ct);
        if (trigger is null || trigger.Kind != AgentRoutineTriggerKinds.Api || !trigger.Enabled || string.IsNullOrWhiteSpace(trigger.SecretHash))
            return new AgentRoutineExecutionResult(0, []);

        if (!FixedTimeEquals(trigger.SecretHash, AgentRoutineService.HashSecret(secret)))
            return new AgentRoutineExecutionResult(0, []);

        var routine = await _agentRoutineRepository.GetByAsync(new AgentRoutineFilter { Id = trigger.RoutineId, Enabled = true }, ct);
        if (routine is null)
            return new AgentRoutineExecutionResult(0, []);

        var currentTrigger = routine.Triggers.First(item => item.Id == trigger.Id);
        await ExecuteAsync(routine, currentTrigger, DateTime.UtcNow, payloadJson, null, ct);
        await _agentRoutineRepository.UpsertAsync(routine, ct);
        return new AgentRoutineExecutionResult(1, [routine.Id]);
    }

    public async Task<AgentRoutineExecutionResult> ExecuteGitHubWebhookAsync(GitHubRoutineWebhookRequest request, CancellationToken ct = default)
    {
        var repositoryFullName = ExtractRepositoryFullName(request.Payload);
        if (string.IsNullOrWhiteSpace(repositoryFullName))
            return new AgentRoutineExecutionResult(0, []);

        var parts = repositoryFullName.Split('/', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return new AgentRoutineExecutionResult(0, []);

        var routines = await _agentRoutineRepository.ListAllEnabledForExecutionAsync(ct);
        var fired = new List<Guid>();
        var now = DateTime.UtcNow;

        foreach (var execution in routines)
        {
            var routine = execution.Routine;
            var changed = false;
            foreach (var trigger in routine.Triggers.Where(trigger => trigger.Enabled && trigger.Kind == AgentRoutineTriggerKinds.GitHub))
            {
                var config = DeserializeGitHubConfig(trigger.ConfigJson);
                if (!config.Mode.Equals(GitHubRoutineTriggerModes.Webhook, StringComparison.OrdinalIgnoreCase)
                    || !config.Owner.Equals(parts[0], StringComparison.OrdinalIgnoreCase)
                    || !config.Repo.Equals(parts[1], StringComparison.OrdinalIgnoreCase)
                    || !config.Events.Any(item => item.Equals(request.Event, StringComparison.OrdinalIgnoreCase)))
                    continue;

                await ExecuteAsync(routine, trigger, now, request.Payload, execution.WorkspaceId, ct);
                fired.Add(routine.Id);
                changed = true;
            }

            if (changed)
                await _agentRoutineRepository.UpsertAsync(routine, ct);
        }

        return new AgentRoutineExecutionResult(fired.Count, fired);
    }

    public async Task<AgentRoutineExecutionResult> ExecuteGitHubPollTriggerAsync(Guid triggerId, string payloadJson, CancellationToken ct = default)
    {
        var trigger = await _agentRoutineRepository.GetTriggerByAsync(triggerId, ct);
        if (trigger is null || trigger.Kind != AgentRoutineTriggerKinds.GitHub || !trigger.Enabled)
            return new AgentRoutineExecutionResult(0, []);

        var routine = await _agentRoutineRepository.GetByAsync(new AgentRoutineFilter { Id = trigger.RoutineId, Enabled = true }, ct);
        if (routine is null)
            return new AgentRoutineExecutionResult(0, []);

        var currentTrigger = routine.Triggers.First(item => item.Id == trigger.Id);
        await ExecuteAsync(routine, currentTrigger, DateTime.UtcNow, payloadJson, null, ct);
        await _agentRoutineRepository.UpsertAsync(routine, ct);
        return new AgentRoutineExecutionResult(1, [routine.Id]);
    }

    private async Task ExecuteAsync(
        AgentRoutineRecord routine,
        AgentRoutineTriggerRecord trigger,
        DateTime now,
        string? payloadJson,
        Guid? workspaceId,
        CancellationToken ct)
    {
        var prompt = BuildPrompt(routine, trigger, payloadJson);
        var work = await _agentService.SendMessageAsync(routine.AgentId, prompt, Guid.Empty, ct, AgentWorkPurposeKinds.Routine);
        routine.MarkTriggered(now);
        trigger.MarkTriggered(now);
        await _publisher.Publish(new RoutineTriggerFiredEvent(
            routine.Id,
            routine.Name,
            routine.AgentId,
            workspaceId ?? work.WorkspaceId,
            trigger.Id,
            trigger.Name,
            trigger.Kind,
            work.CorrelationId,
            string.IsNullOrWhiteSpace(payloadJson) ? null : payloadJson.Length), ct);
    }

    private static string BuildPrompt(AgentRoutineRecord routine, AgentRoutineTriggerRecord trigger, string? payloadJson)
    {
        var builder = new StringBuilder()
            .AppendLine($"[Routine: {routine.Name}]")
            .AppendLine($"[Trigger: {trigger.Name}]")
            .AppendLine($"[Trigger type: {trigger.Kind}]")
            .AppendLine()
            .AppendLine(routine.Prompt);

        if (!string.IsNullOrWhiteSpace(payloadJson))
        {
            builder
                .AppendLine()
                .AppendLine("[Trigger payload]")
                .AppendLine(payloadJson);
        }

        return builder.ToString();
    }

    private static string GetScheduleExpression(AgentRoutineTriggerRecord trigger)
    {
        using var document = JsonDocument.Parse(trigger.ConfigJson);
        return document.RootElement.TryGetProperty("expression", out var expression)
            ? expression.GetString() ?? string.Empty
            : string.Empty;
    }

    private static DateTime? NextOccurrence(string expression, DateTime fromUtc)
    {
        var cron = Cronos.CronExpression.Parse(expression);
        return cron.GetNextOccurrence(fromUtc, inclusive: false);
    }

    private static string? ExtractRepositoryFullName(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        return document.RootElement.TryGetProperty("repository", out var repository)
            && repository.TryGetProperty("full_name", out var fullName)
            ? fullName.GetString()
            : null;
    }

    private static GitHubRoutineTriggerConfig DeserializeGitHubConfig(string configJson)
    {
        return JsonSerializer.Deserialize<GitHubRoutineTriggerConfig>(configJson)
            ?? new GitHubRoutineTriggerConfig();
    }

    private static bool FixedTimeEquals(string expected, string actual)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return expectedBytes.Length == actualBytes.Length && CryptographicOperations.FixedTimeEquals(expectedBytes, actualBytes);
    }
}

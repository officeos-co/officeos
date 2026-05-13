namespace OffceOs.Application.Features.Agents;

/// <summary>
/// Owns persistence and ambient context for an agent run.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> creating an `AgentRunRecord`, opening the ambient
/// `AgentRunContext`, and marking the run completed or failed.</para>
/// <para><strong>Responsible only for:</strong> run lifecycle state. It does not decide why a turn
/// succeeds or fails, publish domain events, or execute LLM/tool work.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when `AgentRunRecord`
/// lifecycle semantics, run status transitions, or ambient run scoping changes.</para>
/// </remarks>
internal sealed class AgentRunLifecycle
{
    private readonly IAgentRunRepository _agentRunRepository;

    public AgentRunLifecycle(IAgentRunRepository agentRunRepository)
    {
        _agentRunRepository = agentRunRepository;
    }

    public async Task<AgentRunScope> BeginAsync(Guid agentId, string correlationId, string prompt, CancellationToken ct)
    {
        var run = await _agentRunRepository.CreateAsync(new AgentRunRecord
        {
            AgentId = agentId,
            ParentRunId = AgentRunContext.RunId,
            ParentCorrelationId = correlationId,
            Kind = "turn",
            Status = "running",
            Name = "Agent turn",
            Prompt = prompt,
        }, ct);

        return new AgentRunScope(run, AgentRunContext.Begin(run.Id, run.ParentRunId));
    }

    public Task CompleteAsync(AgentRunScope scope, string? result, CancellationToken ct)
        => FinishAsync(scope, "completed", result, null, ct);

    public Task FailAsync(AgentRunScope scope, string error, CancellationToken ct)
        => FinishAsync(scope, "failed", null, error, ct);

    private async Task FinishAsync(AgentRunScope scope, string status, string? result, string? error, CancellationToken ct)
    {
        scope.Record.Status = status;
        scope.Record.Result = result;
        scope.Record.Error = error;
        scope.Record.CompletedAt = DateTime.UtcNow;
        scope.Record.UpdatedAt = DateTime.UtcNow;
        await _agentRunRepository.UpdateAsync(scope.Record, ct);
    }
}

/// <summary>
/// Holds a persisted run record together with the ambient run context scope.
/// </summary>
/// <remarks>
/// <para><strong>Responsible for:</strong> keeping the `AgentRunRecord` and disposing the ambient
/// `AgentRunContext` when the application turn exits.</para>
/// <para><strong>Responsible only for:</strong> scoped run context lifetime. It does not mutate run
/// status, publish events, or execute turn work.</para>
/// <para><strong>Acceptance criteria:</strong> this class should change only when ambient run context
/// lifetime or the run scope shape changes.</para>
/// </remarks>
internal sealed class AgentRunScope : IDisposable
{
    private readonly IDisposable _ambientScope;

    public AgentRunScope(AgentRunRecord record, IDisposable ambientScope)
    {
        Record = record;
        _ambientScope = ambientScope;
    }

    public AgentRunRecord Record { get; }

    public void Dispose() => _ambientScope.Dispose();
}

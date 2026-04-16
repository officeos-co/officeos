namespace EnterpriseAgentOs.Api.Entities.Runners;

public sealed class RunnerJobResult
{
    public bool Success { get; init; }
    public JsonElement? Result { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Singleton that bridges the gap between synchronous skill_exec calls
/// and asynchronous runner polling. Uses TaskCompletionSource to let
/// the skill_exec endpoint await a runner job result.
/// </summary>
public sealed class RunnerJobWaiter
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<RunnerJobResult>> _waiters = new();

    public TaskCompletionSource<RunnerJobResult> Register(Guid jobId)
    {
        var tcs = new TaskCompletionSource<RunnerJobResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        _waiters[jobId] = tcs;
        return tcs;
    }

    public void Complete(Guid jobId, RunnerJobResult result)
    {
        if (_waiters.TryRemove(jobId, out var tcs))
            tcs.TrySetResult(result);
    }

    public void Fail(Guid jobId, string error)
    {
        if (_waiters.TryRemove(jobId, out var tcs))
            tcs.TrySetResult(new RunnerJobResult { Success = false, Error = error });
    }

    public void Remove(Guid jobId)
    {
        _waiters.TryRemove(jobId, out _);
    }
}

namespace OffceOs.Application.Features.Agents;

public interface IAgentWorkExecutionService
{
    Task ExecuteQueuedWorkAsync(AgentLogRecord work, CancellationToken ct = default);
}

public interface IOpenCodeProcessService
{
    Task<ProcessRunResult> RunAsync(
        ProcessRunRequest request,
        Func<string, CancellationToken, Task> onStdoutLine,
        Func<string, CancellationToken, Task> onStderrLine,
        CancellationToken ct = default);
}

public sealed record ProcessRunRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment);

public sealed record ProcessRunResult(int ExitCode, string StandardError);

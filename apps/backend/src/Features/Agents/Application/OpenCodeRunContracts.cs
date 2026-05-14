namespace OffceOs.Application.Features.Agents;

public sealed record CreateControlPlaneRunRequest(
    string AgentRef,
    string Task,
    string? EngineRef,
    string? Repository,
    string? Ref,
    string? InputJson,
    bool Wait);

public sealed record ControlPlaneRunResult(
    AgentRunRecord Run,
    string EngineType,
    string EngineRef);

public sealed record ControlPlaneRunLogResult(
    IReadOnlyList<AgentLogRecord> Entries);

public interface IControlPlaneRunService
{
    Task<ControlPlaneRunResult> CreateAsync(CreateControlPlaneRunRequest request, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<AgentRunRecord>> ListAsync(Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<AgentRunRecord?> GetAsync(Guid runId, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<bool> CancelAsync(Guid runId, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task<ControlPlaneRunLogResult> LogsAsync(Guid runId, Guid ownerId, Guid workspaceId, CancellationToken ct = default);
    Task ExecuteQueuedRunAsync(AgentRunRecord run, CancellationToken ct = default);
}

public interface IOpenCodeProcessService
{
    Task<ProcessRunResult> RunAsync(ProcessRunRequest request, Func<string, CancellationToken, Task> onStdoutLine, CancellationToken ct = default);
}

public sealed record ProcessRunRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory,
    IReadOnlyDictionary<string, string> Environment);

public sealed record ProcessRunResult(int ExitCode, string StandardError);

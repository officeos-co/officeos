using OffceOs.Domain.Common.Primitives;

namespace OffceOs.Domain.Features.AgentHarness;

public sealed record AgentSandboxCreateRequest(
    Guid AgentId,
    IReadOnlyDictionary<string, string> Environment,
    IReadOnlyDictionary<string, string> Metadata);

public sealed record AgentSandboxDeployment(string SandboxId, string? ServiceUrl);

public sealed record AgentSandboxCommandResult(string Output, int ExitCode);

public interface IAgentSandbox
{
    Task<AgentSandboxDeployment> CreateAsync(
        Guid agentId,
        IReadOnlyDictionary<string, string> environment,
        IReadOnlyDictionary<string, string> metadata,
        CancellationToken ct = default);

    Task<AgentResult<AgentSandboxCommandResult>> ExecuteAsync(
        string sandboxId,
        string serviceUrl,
        string command,
        TimeSpan timeout,
        CancellationToken ct = default);

    Task<AgentResult<string>> ReadFileAsync(
        string sandboxId,
        string serviceUrl,
        string path,
        CancellationToken ct = default);

    Task<AgentResult<bool>> WriteFileAsync(
        string sandboxId,
        string serviceUrl,
        string path,
        string content,
        CancellationToken ct = default);

    Task<bool> TerminateAsync(string sandboxId, CancellationToken ct = default);
}

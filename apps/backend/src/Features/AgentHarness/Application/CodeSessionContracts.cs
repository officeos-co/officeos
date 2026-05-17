using OffceOs.Features.Agents.Domain;

namespace OffceOs.Features.AgentHarness.Application;

public interface ICodeSessionService
{
    Task PrepareAsync(AgentSessionRecord session, string sandboxId, string serviceUrl, CancellationToken ct = default);
    Task<CodeSessionFinalizeResult?> FinalizeAsync(AgentSessionRecord session, string sandboxId, string serviceUrl, CancellationToken ct = default);
}

public sealed record CodeSessionFinalizeResult(
    string Branch,
    string CommitSha,
    string PullRequestUrl,
    int? PullRequestNumber);

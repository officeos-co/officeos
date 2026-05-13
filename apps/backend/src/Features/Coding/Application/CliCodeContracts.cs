namespace OffceOs.Application.Features.Coding;

public interface ICliCodeService
{
    Task<CliCodeSessionResult> CreateSessionAsync(CliCodeSessionRequest request, Guid userId, Guid workspaceId, CancellationToken ct = default);
}

public sealed record CliCodeRepositoryRequest(
    string? Root,
    string? RemoteUrl,
    string? Branch,
    string? Commit,
    bool HasChanges);

public sealed record CliCodeSessionRequest(
    string? Provider,
    string? Model,
    string? Effort,
    CliCodeRepositoryRequest? Repository);

public sealed record CliCodeSessionResult(
    Guid SessionId,
    Guid AgentId,
    string Name,
    string Provider,
    string Model,
    string Effort);

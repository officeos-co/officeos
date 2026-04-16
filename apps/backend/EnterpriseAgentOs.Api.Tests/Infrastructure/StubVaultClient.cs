namespace EnterpriseAgentOs.Api.Tests.Infrastructure;

/// <summary>
/// No-op vault client for integration tests. Avoids needing a real CouchDB.
/// </summary>
public sealed class StubVaultClient : EnterpriseAgentOs.Api.Entities.Vault.IVaultClient
{
    public Task CreateAgentVaultAsync(Guid agentId, string agentName, string provider, string? model, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteAgentVaultAsync(Guid agentId, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<string>> ListFilesAsync(Guid agentId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task<string?> GetFileAsync(Guid agentId, string fileName, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    public Task PutFileAsync(Guid agentId, string fileName, string content, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task DeleteFileAsync(Guid agentId, string fileName, CancellationToken ct = default)
        => Task.CompletedTask;
}

namespace EnterpriseAgentOs.Api.Entities.Vault;

public interface IVaultClient
{
    Task CreateAgentVaultAsync(Guid agentId, string agentName, string provider, string? model, CancellationToken ct = default);
    Task DeleteAgentVaultAsync(Guid agentId, CancellationToken ct = default);
    Task<IReadOnlyList<string>> ListFilesAsync(Guid agentId, CancellationToken ct = default);
    Task<string?> GetFileAsync(Guid agentId, string fileName, CancellationToken ct = default);
    Task PutFileAsync(Guid agentId, string fileName, string content, CancellationToken ct = default);
    Task DeleteFileAsync(Guid agentId, string fileName, CancellationToken ct = default);
}

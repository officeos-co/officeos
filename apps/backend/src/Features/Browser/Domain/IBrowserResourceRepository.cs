namespace OffceOs.Features.Browser.Domain;

public interface IBrowserResourceRepository
{
    Task<IReadOnlyList<BrowserResourceRecord>> ListAsync(BrowserResourceFilter filter, CancellationToken ct = default);
    Task<BrowserResourceRecord?> GetByAsync(BrowserResourceFilter filter, CancellationToken ct = default);
    Task<BrowserResourceRecord> SaveAsync(BrowserResourceRecord resource, CancellationToken ct = default);
    Task<bool> DeleteAsync(BrowserResourceFilter filter, CancellationToken ct = default);
    Task SetCurrentAgentAsync(Guid browserResourceId, Guid agentId, CancellationToken ct = default);
}

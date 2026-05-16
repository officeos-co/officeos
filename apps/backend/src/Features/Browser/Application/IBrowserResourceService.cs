using OffceOs.Domain.Features.Browser;

namespace OffceOs.Application.Features.Browser;

public interface IBrowserResourceService
{
    Task<IReadOnlyList<BrowserResourceRecord>> ListAsync(Guid workspaceId, CancellationToken ct = default);
    Task<BrowserResourceRecord?> GetAsync(Guid id, Guid workspaceId, CancellationToken ct = default);
    Task<BrowserResourceRecord> CreateAsync(Guid ownerId, Guid workspaceId, string? displayName, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, Guid workspaceId, CancellationToken ct = default);
}

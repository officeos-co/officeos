using OffceOs.Domain.Features.Browser;

namespace OffceOs.Application.Features.Browser;

internal sealed class BrowserResourceService : IBrowserResourceService
{
    private readonly IBrowserResourceRepository _browserResourceRepository;

    public BrowserResourceService(IBrowserResourceRepository browserResourceRepository)
    {
        _browserResourceRepository = browserResourceRepository;
    }

    public Task<IReadOnlyList<BrowserResourceRecord>> ListAsync(Guid workspaceId, CancellationToken ct = default) =>
        _browserResourceRepository.ListAsync(new BrowserResourceFilter { WorkspaceId = workspaceId }, ct);

    public Task<BrowserResourceRecord?> GetAsync(Guid id, Guid workspaceId, CancellationToken ct = default) =>
        _browserResourceRepository.GetByAsync(new BrowserResourceFilter { Id = id, WorkspaceId = workspaceId }, ct);

    public Task<BrowserResourceRecord> CreateAsync(Guid ownerId, Guid workspaceId, string? displayName, CancellationToken ct = default) =>
        _browserResourceRepository.SaveAsync(BrowserResourceRecord.Create(ownerId, workspaceId, displayName ?? "Browser"), ct);

    public Task<bool> DeleteAsync(Guid id, Guid workspaceId, CancellationToken ct = default) =>
        _browserResourceRepository.DeleteAsync(new BrowserResourceFilter { Id = id, WorkspaceId = workspaceId }, ct);
}

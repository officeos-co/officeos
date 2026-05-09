namespace OffceOs.Application.Features.Management;

internal sealed class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IDistributedCache _distributedCache;

    public WorkspaceService(IWorkspaceRepository workspaceRepository, IDistributedCache distributedCache)
    {
        _workspaceRepository = workspaceRepository;
        _distributedCache = distributedCache;
    }

    public async Task<IReadOnlyList<WorkspaceRecord>> ListAsync(Guid userId, CancellationToken ct = default)
    {
        await _workspaceRepository.EnsureDefaultAsync(userId, ct);
        return await _workspaceRepository.ListAsync(new WorkspaceFilter { UserId = userId }, ct);
    }

    public Task<WorkspaceRecord> GetCurrentAsync(Guid userId, CancellationToken ct = default)
        => _workspaceRepository.GetCurrentAsync(userId, ct);

    public async Task<WorkspaceRecord> CreateAsync(Guid userId, string? name, CancellationToken ct = default)
    {
        var created = await _workspaceRepository.SaveAsync(WorkspaceRecord.Create(userId, name), ct);
        await InvalidateUserAsync(userId, ct);
        return created;
    }

    public async Task<WorkspaceRecord> UpdateAsync(Guid userId, Guid id, string? name, CancellationToken ct = default)
    {
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = id, UserId = userId }, ct)
            ?? throw new InvalidOperationException("Workspace not found.");

        workspace.Rename(name);
        var updated = await _workspaceRepository.SaveAsync(workspace, ct);
        await InvalidateUserAsync(userId, ct);
        return updated;
    }

    public async Task<WorkspaceRecord> SwitchAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var workspace = await _workspaceRepository.GetByAsync(new WorkspaceFilter { Id = id, UserId = userId }, ct)
            ?? throw new InvalidOperationException("Workspace not found.");

        await _workspaceRepository.SetCurrentAsync(userId, id, ct);
        await InvalidateUserAsync(userId, ct);
        return workspace;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var deleted = await _workspaceRepository.DeleteAsync(id, userId, ct);
        if (deleted)
        {
            await _workspaceRepository.GetCurrentAsync(userId, ct);
            await InvalidateUserAsync(userId, ct);
        }

        return deleted;
    }

    private async Task InvalidateUserAsync(Guid userId, CancellationToken ct)
    {
        await _distributedCache.RemoveAsync($"auth:me:{userId}", ct);
    }
}

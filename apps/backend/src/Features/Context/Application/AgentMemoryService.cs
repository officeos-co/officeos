namespace EnterpriseAgentOs.Application.Features.Context;

internal sealed class AgentMemoryService : IAgentMemoryService
{
    private readonly IAgentMemoryRepository _agentMemoryRepository;
    private readonly IAgentResourceRepository _resourceRepository;
    private readonly IMemoryStoreRepository _memoryStoreRepository;

    public AgentMemoryService(
        IAgentMemoryRepository agentMemoryRepository,
        IAgentResourceRepository resourceRepository,
        IMemoryStoreRepository memoryStoreRepository)
    {
        _agentMemoryRepository = agentMemoryRepository;
        _resourceRepository = resourceRepository;
        _memoryStoreRepository = memoryStoreRepository;
    }

    public async Task StoreAsync(
        Guid agentId,
        string key,
        string content,
        CancellationToken ct = default)
    {
        var attachment = await _resourceRepository.GetActiveMemoryStoreAttachmentAsync(agentId, ct);
        if (attachment is null)
            await _agentMemoryRepository.UpsertAsync(agentId, key, content, ct);
        else
            await _memoryStoreRepository.UpsertEntryForStoreAsync(attachment.ResourceId, key, content, ct);
    }

    public async Task<IReadOnlyList<AgentMemoryRecord>> RecallAsync(
        Guid agentId,
        string query,
        int limit,
        CancellationToken ct = default)
    {
        var memories = await ListActiveMemoriesAsync(agentId, ct);
        IEnumerable<AgentMemoryRecord> filtered = memories;

        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = memories
                .Where(m => m.Key.Contains(query, StringComparison.OrdinalIgnoreCase)
                         || m.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(m => CountOccurrences(m.Key + " " + m.Content, query));
        }

        return filtered.Take(Math.Max(0, limit)).ToList();
    }

    public async Task<bool> ForgetAsync(Guid agentId, string key, CancellationToken ct = default)
    {
        var attachment = await _resourceRepository.GetActiveMemoryStoreAttachmentAsync(agentId, ct);
        return attachment is null
            ? await _agentMemoryRepository.DeleteAsync(agentId, key, ct)
            : await _memoryStoreRepository.DeleteEntryForStoreAsync(attachment.ResourceId, key, ct);
    }

    private async Task<IReadOnlyList<AgentMemoryRecord>> ListActiveMemoriesAsync(Guid agentId, CancellationToken ct)
    {
        var attachment = await _resourceRepository.GetActiveMemoryStoreAttachmentAsync(agentId, ct);
        if (attachment is null)
            return await _agentMemoryRepository.ListAsync(agentId, ct);

        var activeStoreEntries = await _memoryStoreRepository.ListEntriesForStoreAsync(attachment.ResourceId, ct);
        return activeStoreEntries.Select(e => new AgentMemoryRecord
        {
            Id = e.Id,
            AgentId = agentId,
            Key = e.Key,
            Content = e.Content,
            CreatedAt = e.CreatedAt,
            UpdatedAt = e.UpdatedAt,
        }).ToList();
    }

    private static int CountOccurrences(string text, string pattern)
    {
        var count = 0;
        var idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }

        return count;
    }
}

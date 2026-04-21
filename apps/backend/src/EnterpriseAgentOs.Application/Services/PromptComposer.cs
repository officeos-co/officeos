namespace EnterpriseAgentOs.Application.Services;

/// <summary>
/// Fetches personality files and memories for prompt composition.
/// The actual prompt assembly happens via Domain.Services.SystemPromptComposer.
/// </summary>
internal sealed class PromptComposer
{
    private readonly IAgentPersonalityRepository _agentPersonalityRepository;
    private readonly IAgentMemoryRepository _agentMemoryRepository;

    public PromptComposer(
        IAgentPersonalityRepository personality,
        IAgentMemoryRepository memory)
    {
        _agentPersonalityRepository = personality;
        _agentMemoryRepository = memory;
    }

    /// <summary>Loads all personality files and memories for prompt composition.</summary>
    public async Task<PromptContext> LoadAsync(Guid agentId, CancellationToken ct = default)
    {
        var personalityFiles = await _agentPersonalityRepository.ListAsync(agentId, ct);
        var memories = await _agentMemoryRepository.ListAsync(agentId, ct);
        return new PromptContext(personalityFiles, memories);
    }
}

/// <summary>All context needed to compose the system prompt for a turn.</summary>
internal sealed record PromptContext(
    IReadOnlyList<AgentPersonalityRecord> PersonalityFiles,
    IReadOnlyList<AgentMemoryRecord> Memories);

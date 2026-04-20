namespace EnterpriseAgentOs.Application.Services.Agents;

public sealed class PromptComposer
{
    private readonly IAgentPersonalityRepository _personality;
    private readonly IAgentMemoryRepository _memory;

    public PromptComposer(
        IAgentPersonalityRepository personality,
        IAgentMemoryRepository memory)
    {
        _personality = personality;
        _memory = memory;
    }

    public async Task<string> ComposeAsync(Guid agentId, string? userPrompt, CancellationToken ct = default)
    {
        var personalityFiles = await _personality.ListAsync(agentId, ct);
        var memories = await _memory.ListAsync(agentId, ct);

        var sections = new List<string>();

        // Domain model owns the composition order via CompositionOrder property.
        foreach (var file in personalityFiles.OrderBy(p => p.CompositionOrder))
            sections.Add(file.Content);

        if (!string.IsNullOrWhiteSpace(userPrompt))
            sections.Add(userPrompt);

        if (memories.Count > 0)
        {
            var memorySection = "## Memory\n\n" +
                string.Join("\n\n", memories.Select(m => $"### {m.Key}\n{m.Content}"));
            sections.Add(memorySection);
        }

        return string.Join("\n\n---\n\n", sections);
    }
}

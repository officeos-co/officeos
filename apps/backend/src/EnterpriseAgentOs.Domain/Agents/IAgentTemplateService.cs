namespace EnterpriseAgentOs.Domain.Agents;

public interface IAgentTemplateService
{
    Task<IReadOnlyList<AgentTemplateDto>> ListAsync(CancellationToken ct = default);
    Task<AgentTemplateDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<AgentDto> CreateAgentFromTemplateAsync(
        Guid templateId,
        string name,
        string provider,
        string? model,
        Guid ownerId,
        CancellationToken ct = default);
}

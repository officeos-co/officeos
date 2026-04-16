namespace EnterpriseAgentOs.Api.Entities.AgentTemplates;

public interface IAgentTemplateService
{
    Task<IReadOnlyList<AgentTemplateDto>> ListAsync(CancellationToken ct = default);
    Task<AgentTemplateDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Api.Entities.Agents.AgentDto> CreateAgentFromTemplateAsync(
        Guid templateId,
        string name,
        string provider,
        string? model,
        Guid ownerId,
        CancellationToken ct = default);
}

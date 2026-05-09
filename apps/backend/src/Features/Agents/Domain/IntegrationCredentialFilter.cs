namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public sealed record IntegrationCredentialFilter
{
    public Guid? Id { get; init; }
    public Guid? OwnerId { get; init; }
    public string? IntegrationName { get; init; }
}

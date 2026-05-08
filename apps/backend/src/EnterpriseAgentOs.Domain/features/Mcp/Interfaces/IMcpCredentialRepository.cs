namespace EnterpriseAgentOs.Domain.Features.Agents.Integrations;

public sealed record IntegrationCredentialFilter
{
    public Guid? Id { get; init; }
    public string? IntegrationName { get; init; }
}

public interface IIntegrationCredentialRepository
{
    Task<IntegrationCredentialRecord?> GetByAsync(IntegrationCredentialFilter filter, CancellationToken ct = default);
    Task UpsertAsync(IntegrationCredentialRecord credential, CancellationToken ct = default);
    Task DeleteAsync(string integrationName, CancellationToken ct = default);
}

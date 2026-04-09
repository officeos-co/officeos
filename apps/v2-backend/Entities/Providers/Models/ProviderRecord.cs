namespace EnterpriseAgentOs.Api.Entities.Providers.Models;

public sealed class ProviderRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Configured { get; set; }
}

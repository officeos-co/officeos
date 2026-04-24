namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class ProviderEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? EncryptedApiKey { get; set; }
    public DateTime? ConfiguredAt { get; set; }
}

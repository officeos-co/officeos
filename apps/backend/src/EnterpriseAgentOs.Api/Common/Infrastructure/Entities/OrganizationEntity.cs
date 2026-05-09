namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class OrganizationEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

namespace OffceOs.Database.Models;

public sealed class IntegrationCredentialEntity
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid? WorkspaceId { get; set; }
    public string IntegrationName { get; set; } = string.Empty;
    public string EncryptedCredentials { get; set; } = string.Empty;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
    public WorkspaceEntity? Workspace { get; set; }
}

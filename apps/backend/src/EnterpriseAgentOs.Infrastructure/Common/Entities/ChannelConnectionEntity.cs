namespace EnterpriseAgentOs.Infrastructure.Common.Entities;

public sealed class ChannelConnectionEntity
{
    public Guid Id { get; set; }
    public string ChannelType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public UserEntity? CreatedBy { get; set; }
}

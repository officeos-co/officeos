using System.ComponentModel.DataAnnotations;

namespace EnterpriseAgentOs.Api.Database.Models;

public sealed class AgentRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = string.Empty;

    public string? Model { get; set; }

    public string Status { get; set; } = "pending";

    public string? PodName { get; set; }

    public string? ServiceUrl { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }
}

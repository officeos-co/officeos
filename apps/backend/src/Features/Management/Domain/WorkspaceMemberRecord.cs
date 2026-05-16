using OffceOs.Domain.Common.ValueObjects;

namespace OffceOs.Domain.Features.Management;

public sealed class WorkspaceMemberRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid WorkspaceId { get; init; }
    public Guid UserId { get; init; }
    public WorkspaceRole Role { get; set; } = WorkspaceRole.Viewer;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public static WorkspaceMemberRecord Create(Guid workspaceId, Guid userId, WorkspaceRole role) => new()
    {
        WorkspaceId = workspaceId,
        UserId = userId,
        Role = role,
    };
}

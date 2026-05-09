namespace OffceOs.Api.Features.Management;

public sealed record WorkspacePayload(
    Guid Id,
    Guid UserId,
    string Name,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateWorkspaceInput(string Name);

public sealed record UpdateWorkspaceInput(string? Name);

public static class WorkspaceGraphQLMapper
{
    public static WorkspacePayload ToPayload(WorkspaceRecord record) =>
        new(record.Id, record.UserId, record.Name, record.CreatedAt, record.UpdatedAt);
}

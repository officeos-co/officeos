using OffceOs.Database.Models;
using OffceOs.Features.Agents.Domain;
using OffceOs.Features.ResourceLogs.Domain;
using OffceOs.Features.ResourceLogs.Infrastructure;
using OffceOs.Tests.Shared;

namespace OffceOs.Tests.ResourceLogs;

public sealed class ResourceLogRepositoryTests
{
    [Fact]
    public async Task Agent_context_does_not_rewrite_explicit_routine_resource_identity()
    {
        await using var db = TestDbFactory.Create("routine-resource-log-kind");
        var repository = new ResourceLogRepository(db);
        var workspaceId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var routineId = Guid.NewGuid();
        db.Agents.Add(new AgentEntity
        {
            Id = agentId,
            Name = "Agent",
            Provider = "openai",
            Model = "gpt-4o-mini",
            Status = AgentStatus.Idle.ToStorageString(),
            CreatedAt = DateTime.UtcNow,
            WorkspaceId = workspaceId,
        });
        await db.SaveChangesAsync();

        await repository.AppendAsync(new ResourceLogRecord
        {
            ResourceKind = ResourceLogKinds.Routine,
            ResourceId = routineId,
            ResourceName = routineId.ToString(),
            ParentResourceKind = ResourceLogKinds.Agent,
            ParentResourceId = agentId,
            AgentId = agentId,
            Type = ResourceLogType.System,
            Content = "Routine trigger fired.",
        });

        var saved = await repository.GetByAsync(new ResourceLogFilter
        {
            ResourceKind = ResourceLogKinds.Routine,
            ResourceId = routineId,
        });

        Assert.NotNull(saved);
        Assert.Equal(ResourceLogKinds.Routine, saved.ResourceKind);
        Assert.Equal(routineId, saved.ResourceId);
        Assert.Equal(ResourceLogKinds.Agent, saved.ParentResourceKind);
        Assert.Equal(agentId, saved.ParentResourceId);
        Assert.Equal(agentId, saved.AgentId);
        Assert.Equal(workspaceId, saved.WorkspaceId);
    }
}

namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class AgentMemoryMutations
{
    public async Task<EnterpriseAgentOs.Api.Entities.AgentMemory.Types.AgentMemoryGqlDto> UpsertAgentMemory(
        Guid agentId,
        string key,
        string content,
        string category,
        string? @namespace,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Database.EaosDbContext db,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);

        var existing = await db.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);

        if (existing is null)
        {
            existing = new EnterpriseAgentOs.Api.Database.Models.AgentMemoryRecord
            {
                AgentId = agentId,
                Key = key,
                Content = content,
                Category = category,
                Namespace = @namespace ?? "default",
            };
            db.AgentMemories.Add(existing);
        }
        else
        {
            existing.Content = content;
            existing.Category = category;
            existing.Namespace = @namespace ?? existing.Namespace;
        }

        await db.SaveChangesAsync(ct);
        return EnterpriseAgentOs.Api.Entities.AgentMemory.Types.AgentMemoryGraphQLMapper.ToDto(existing);
    }

    public async Task<bool> DeleteAgentMemory(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Database.EaosDbContext db,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var count = await db.AgentMemories
            .Where(m => m.Id == id)
            .ExecuteDeleteAsync(ct);
        return count > 0;
    }

}

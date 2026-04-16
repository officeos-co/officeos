using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentMemoryMutations
{
    public async Task<AgentMemoryGqlDto> UpsertAgentMemory(
        Guid agentId,
        string key,
        string content,
        string category,
        string? @namespace,
        IResolverContext context,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);

        var existing = await db.AgentMemories
            .FirstOrDefaultAsync(m => m.AgentId == agentId && m.Key == key, ct);

        if (existing is null)
        {
            existing = new AgentMemoryRecord
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
        return AgentMemoryGraphQLMapper.ToDto(existing);
    }

    public async Task<bool> DeleteAgentMemory(
        Guid id,
        IResolverContext context,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var count = await db.AgentMemories
            .Where(m => m.Id == id)
            .ExecuteDeleteAsync(ct);
        return count > 0;
    }

    public async Task<VaultFileDto> WriteVaultFile(
        Guid agentId,
        string path,
        string content,
        IResolverContext context,
        [Service] IVaultClient vault,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        await vault.PutFileAsync(agentId, path, content, ct);
        return new VaultFileDto(path, content);
    }
}

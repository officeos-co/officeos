using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentMemoryQueries
{
    public async Task<IReadOnlyList<AgentMemoryGqlDto>> GetAgentMemories(
        Guid agentId,
        string? category,
        string? @namespace,
        IResolverContext context,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var q = db.AgentMemories.AsNoTracking().Where(m => m.AgentId == agentId);
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(m => m.Category == category);
        if (!string.IsNullOrWhiteSpace(@namespace))
            q = q.Where(m => m.Namespace == @namespace);
        var rows = await q.OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync(ct);
        return rows.Select(AgentMemoryGraphQLMapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<VaultFileDto>> GetVaultFiles(
        Guid agentId,
        IResolverContext context,
        [Service] IVaultClient vault,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var fileNames = await vault.ListFilesAsync(agentId, ct);
        var list = new List<VaultFileDto>();
        foreach (var name in fileNames)
        {
            var content = await vault.GetFileAsync(agentId, name, ct);
            if (content is null) continue;
            list.Add(new VaultFileDto(name, content));
        }
        return list;
    }

    public async Task<string?> GetVaultFile(
        Guid agentId,
        string path,
        IResolverContext context,
        [Service] IVaultClient vault,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await vault.GetFileAsync(agentId, path, ct);
    }
}

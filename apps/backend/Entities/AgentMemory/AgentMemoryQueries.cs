namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class AgentMemoryQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.AgentMemory.Types.AgentMemoryGqlDto>> GetAgentMemories(
        Guid agentId,
        string? category,
        string? @namespace,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Database.EaosDbContext db,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var q = db.AgentMemories.AsNoTracking().Where(m => m.AgentId == agentId);
        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(m => m.Category == category);
        if (!string.IsNullOrWhiteSpace(@namespace))
            q = q.Where(m => m.Namespace == @namespace);
        var rows = await q.OrderByDescending(m => m.CreatedAt).Take(200).ToListAsync(ct);
        return rows.Select(EnterpriseAgentOs.Api.Entities.AgentMemory.Types.AgentMemoryGraphQLMapper.ToDto).ToList();
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.AgentMemory.Types.VaultFileDto>> GetVaultFiles(
        Guid agentId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Vault.IVaultClient vault,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var fileNames = await vault.ListFilesAsync(agentId, ct);
        var list = new List<EnterpriseAgentOs.Api.Entities.AgentMemory.Types.VaultFileDto>();
        foreach (var name in fileNames)
        {
            var content = await vault.GetFileAsync(agentId, name, ct);
            if (content is null) continue;
            list.Add(new EnterpriseAgentOs.Api.Entities.AgentMemory.Types.VaultFileDto(name, content));
        }
        return list;
    }

    public async Task<string?> GetVaultFile(
        Guid agentId,
        string path,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Vault.IVaultClient vault,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await vault.GetFileAsync(agentId, path, ct);
    }
}

using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Entities.AgentSkills.GraphQL;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentSkillsMutations
{
    public async Task<bool> AssignSkillToAgent(
        Guid agentId,
        string skillName,
        IResolverContext context,
        [Service] IAgentSkillRepository agentSkills,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        await agentSkills.AssignAsync(agentId, new[] { skillName }, ct);
        return true;
    }

    public async Task<bool> UnassignSkillFromAgent(
        Guid agentId,
        string skillName,
        IResolverContext context,
        [Service] IAgentSkillRepository agentSkills,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await agentSkills.RemoveAsync(agentId, skillName, ct);
    }

    public async Task<AgentToolPermissionDto> SetAgentToolPermission(
        Guid agentId,
        string skillName,
        string toolName,
        ToolPermission permission,
        IResolverContext context,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var skill = skillName.Trim().ToLowerInvariant();
        var tool = toolName.Trim();

        var existing = await db.AgentToolPermissions
            .FirstOrDefaultAsync(p =>
                p.AgentId == agentId && p.SkillName == skill && p.ToolName == tool, ct);

        if (existing is null)
        {
            existing = new AgentToolPermissionRecord
            {
                AgentId = agentId,
                SkillName = skill,
                ToolName = tool,
                Permission = permission,
            };
            db.AgentToolPermissions.Add(existing);
        }
        else
        {
            existing.Permission = permission;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return new AgentToolPermissionDto(existing.SkillName, existing.ToolName, existing.Permission);
    }
}

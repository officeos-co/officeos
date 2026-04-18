namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class AgentSkillsMutations
{
    public async Task<bool> AssignSkillToAgent(
        Guid agentId,
        string skillName,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.AgentSkills.IAgentSkillRepository agentSkills,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        await agentSkills.AssignAsync(agentId, new[] { skillName }, ct);
        return true;
    }

    public async Task<bool> UnassignSkillFromAgent(
        Guid agentId,
        string skillName,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.AgentSkills.IAgentSkillRepository agentSkills,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await agentSkills.RemoveAsync(agentId, skillName, ct);
    }

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.AgentToolPermissionDto> SetAgentToolPermission(
        Guid agentId,
        string skillName,
        string toolName,
        EnterpriseAgentOs.Domain.Models.ToolPermission permission,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var skill = skillName.Trim().ToLowerInvariant();
        var tool = toolName.Trim();

        var existing = await db.AgentToolPermissions
            .FirstOrDefaultAsync(p =>
                p.AgentId == agentId && p.SkillName == skill && p.ToolName == tool, ct);

        if (existing is null)
        {
            existing = new EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord
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
        return new EnterpriseAgentOs.Api.GraphQL.Types.AgentToolPermissionDto(existing.SkillName, existing.ToolName, existing.Permission);
    }
}

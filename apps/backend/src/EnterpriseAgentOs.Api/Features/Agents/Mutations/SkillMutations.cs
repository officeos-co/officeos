namespace EnterpriseAgentOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class SkillMutations
{
    private static void InvalidateSkillCaches(IMemoryCache cache, IResolverContext context)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        cache.Remove($"skills:dashboard:list:{user.Id}");
    }

    [GraphQLDescription("Installs a skill from the catalog by name. Makes it available for assignment to agents.")]
    public async Task<bool> InstallSkill(
        string name,
        IResolverContext context,
        [Service] ISkillService skills,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var dto = await skills.InstallAsync(name, ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Skill '{name}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        InvalidateSkillCaches(cache, context);
        return true;
    }

    [GraphQLDescription("Uninstalls a skill by name. Removes it from all agents.")]
    public async Task<bool> UninstallSkill(
        string name,
        IResolverContext context,
        [Service] ISkillService skills,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var dto = await skills.UninstallAsync(name, ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Skill '{name}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        InvalidateSkillCaches(cache, context);
        return true;
    }

    [GraphQLDescription("Sets credential key-value pairs for a skill. Credentials are encrypted at rest.")]
    public async Task<bool> SetSkillCredentials(
        string name,
        IReadOnlyList<SkillCredentialEntry> credentials,
        IResolverContext context,
        [Service] ISkillService skills,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var dict = credentials.ToDictionary(c => c.Key, c => c.Value);
        var dto = await skills.PutCredentialsAsync(name, dict, ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Skill '{name}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        InvalidateSkillCaches(cache, context);
        return true;
    }

    [GraphQLDescription("Sets where a skill executes: cloud (managed) or runner (self-hosted).")]
    public async Task<bool> SetSkillRunTarget(
        string name,
        string runTarget,
        IResolverContext context,
        [Service] ISkillService skills,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        var dto = await skills.SetRunTargetAsync(name, runTarget, ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Skill '{name}' not found or invalid run target.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return true;
    }

    [GraphQLDescription("Adds a like from the authenticated user to a skill.")]
    public async Task<SkillDashboardDto> LikeSkill(
        Guid skillId,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var skill = await catalog.GetByIdAsync(skillId, ct) ?? throw NotFound(skillId);
        await catalog.AddLikeAsync(skillId, user.Id, ct);
        InvalidateSkillCaches(cache, context);
        return SkillDashboardMapper.ToDto(skill);
    }

    [GraphQLDescription("Removes the authenticated user's like from a skill.")]
    public async Task<SkillDashboardDto> UnlikeSkill(
        Guid skillId,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var skill = await catalog.GetByIdAsync(skillId, ct) ?? throw NotFound(skillId);
        await catalog.RemoveLikeAsync(skillId, user.Id, ct);
        InvalidateSkillCaches(cache, context);
        return SkillDashboardMapper.ToDto(skill);
    }

    [GraphQLDescription("Posts a comment on a skill. Body must not be empty.")]
    public async Task<SkillCommentDto> CommentOnSkill(
        Guid skillId,
        string body,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Comment body cannot be empty.").SetCode("INVALID_INPUT").Build());
        }
        _ = await catalog.GetByIdAsync(skillId, ct) ?? throw NotFound(skillId);
        var record = await catalog.AddCommentAsync(skillId, user.Id, body, ct);
        InvalidateSkillCaches(cache, context);
        return SkillDashboardMapper.ToDto(record);
    }

    [GraphQLDescription("Deletes a skill comment. Only the comment author can delete.")]
    public async Task<bool> DeleteSkillComment(
        Guid commentId,
        IResolverContext context,
        [Service] ISkillCatalogRepository catalog,
        [Service] IMemoryCache cache,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        try
        {
            var deleted = await catalog.DeleteCommentAsync(commentId, user.Id, ct);
            InvalidateSkillCaches(cache, context);
            return deleted;
        }
        catch (InvalidOperationException)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Only the author may delete a comment.").SetCode("FORBIDDEN").Build());
        }
    }

    private static GraphQLException NotFound(Guid id) =>
        new(ErrorBuilder.New().SetMessage($"Skill '{id}' not found.").SetCode("NOT_FOUND").Build());

    // ── Agent-skill bindings ─────────────────────────────────────────────────

    [GraphQLDescription("Assigns an installed skill to an agent so it can use the skill's tools.")]
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

    [GraphQLDescription("Removes a skill assignment from an agent.")]
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

    [GraphQLDescription("Sets an allow/deny permission override for a specific tool on a specific skill for an agent.")]
    public async Task<AgentToolPermissionRecord> SetAgentToolPermission(
        Guid agentId,
        string skillName,
        string toolName,
        ToolPermission permission,
        IResolverContext context,
        [Service] IAgentSkillRepository agentSkills,
        CancellationToken ct)
    {
        _ = DashboardAuthContextExtensions.GetUser(context);
        return await agentSkills.UpsertToolPermissionAsync(agentId, skillName, toolName, permission, ct);
    }
}

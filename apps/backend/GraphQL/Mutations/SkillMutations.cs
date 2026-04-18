namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class SkillMutations
{
    public async Task<bool> InstallSkill(
        string name,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Skills.ISkillService skills,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var dto = await skills.InstallAsync(name, ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Skill '{name}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return true;
    }

    public async Task<bool> UninstallSkill(
        string name,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Skills.ISkillService skills,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var dto = await skills.UninstallAsync(name, ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Skill '{name}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        return true;
    }

    public async Task<bool> SetSkillCredentials(
        string name,
        IReadOnlyList<EnterpriseAgentOs.Api.GraphQL.Types.SkillCredentialEntry> credentials,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Skills.ISkillService skills,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
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
        return true;
    }

    public async Task<bool> SetSkillRunTarget(
        string name,
        string runTarget,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Skills.ISkillService skills,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
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

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.SkillDashboardDto> LikeSkill(
        Guid skillId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var skill = await db.Skills.FirstOrDefaultAsync(s => s.Id == skillId, ct)
            ?? throw NotFound(skillId);

        var existing = await db.SkillLikes
            .FirstOrDefaultAsync(l => l.SkillId == skillId && l.UserId == user.Id, ct);
        if (existing is null)
        {
            db.SkillLikes.Add(new EnterpriseAgentOs.Domain.Models.SkillLikeRecord { SkillId = skillId, UserId = user.Id });
            await db.SaveChangesAsync(ct);
        }
        return EnterpriseAgentOs.Api.GraphQL.Types.SkillDashboardMapper.ToDto(skill);
    }

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.SkillDashboardDto> UnlikeSkill(
        Guid skillId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var skill = await db.Skills.FirstOrDefaultAsync(s => s.Id == skillId, ct)
            ?? throw NotFound(skillId);

        var existing = await db.SkillLikes
            .FirstOrDefaultAsync(l => l.SkillId == skillId && l.UserId == user.Id, ct);
        if (existing is not null)
        {
            db.SkillLikes.Remove(existing);
            await db.SaveChangesAsync(ct);
        }
        return EnterpriseAgentOs.Api.GraphQL.Types.SkillDashboardMapper.ToDto(skill);
    }

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.SkillCommentDto> CommentOnSkill(
        Guid skillId,
        string body,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Comment body cannot be empty.").SetCode("INVALID_INPUT").Build());
        }
        var exists = await db.Skills.AnyAsync(s => s.Id == skillId, ct);
        if (!exists) throw NotFound(skillId);

        var record = new EnterpriseAgentOs.Domain.Models.SkillCommentRecord
        {
            SkillId = skillId,
            UserId = user.Id,
            Body = body.Trim(),
        };
        db.SkillComments.Add(record);
        await db.SaveChangesAsync(ct);

        record.User = user;
        return EnterpriseAgentOs.Api.GraphQL.Types.SkillDashboardMapper.ToDto(record);
    }

    public async Task<bool> DeleteSkillComment(
        Guid commentId,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var comment = await db.SkillComments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment is null) return false;
        if (comment.UserId != user.Id)
        {
            throw new GraphQLException(
                ErrorBuilder.New().SetMessage("Only the author may delete a comment.").SetCode("FORBIDDEN").Build());
        }
        db.SkillComments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<EnterpriseAgentOs.Api.GraphQL.Types.SkillDashboardDto> SetSkillSourceCodeUrl(
        Guid skillId,
        string? url,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var skill = await db.Skills.FirstOrDefaultAsync(s => s.Id == skillId, ct)
            ?? throw NotFound(skillId);
        skill.SourceCodeUrl = string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        skill.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return EnterpriseAgentOs.Api.GraphQL.Types.SkillDashboardMapper.ToDto(skill);
    }

    private static GraphQLException NotFound(Guid id) =>
        new(ErrorBuilder.New().SetMessage($"Skill '{id}' not found.").SetCode("NOT_FOUND").Build());
}

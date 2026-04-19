namespace EnterpriseAgentOs.Domain.Interfaces.Skills;

public interface ISkillCatalogRepository
{
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillRecord>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillRecord>> ListActiveAsync(CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.SkillRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.SkillRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.SkillRecord> UpsertAsync(EnterpriseAgentOs.Domain.Models.SkillRecord record, CancellationToken ct = default);
    Task<bool> DeleteByNameAsync(string name, CancellationToken ct = default);

    // Batch social queries — avoid N+1 in GraphQL resolvers
    Task<Dictionary<Guid, int>> BatchLikesCountAsync(IReadOnlyList<Guid> skillIds, CancellationToken ct = default);
    Task<HashSet<Guid>> BatchLikedByUserAsync(IReadOnlyList<Guid> skillIds, Guid userId, CancellationToken ct = default);
    Task<Dictionary<Guid, int>> BatchCommentCountAsync(IReadOnlyList<Guid> skillIds, CancellationToken ct = default);
    Task<HashSet<string>> BatchInstalledNamesAsync(CancellationToken ct = default);
    Task<HashSet<string>> BatchConfiguredNamesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillCommentRecord>> ListCommentsBySkillAsync(Guid skillId, CancellationToken ct = default);

    // Social mutations
    Task<bool> AddLikeAsync(Guid skillId, Guid userId, CancellationToken ct = default);
    Task<bool> RemoveLikeAsync(Guid skillId, Guid userId, CancellationToken ct = default);
    Task<EnterpriseAgentOs.Domain.Models.SkillCommentRecord> AddCommentAsync(Guid skillId, Guid userId, string body, CancellationToken ct = default);
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken ct = default);
}

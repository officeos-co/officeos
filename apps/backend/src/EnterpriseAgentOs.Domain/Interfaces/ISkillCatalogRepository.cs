namespace EnterpriseAgentOs.Domain.Interfaces;

public interface ISkillCatalogRepository
{
    Task<IReadOnlyList<SkillRecord>> ListAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SkillRecord>> ListActiveAsync(CancellationToken ct = default);
    Task<SkillRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<SkillRecord?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<SkillRecord> UpsertAsync(SkillRecord record, CancellationToken ct = default);
    Task<bool> DeleteByNameAsync(string name, CancellationToken ct = default);

    // Batch social queries — avoid N+1 in GraphQL resolvers
    Task<Dictionary<Guid, int>> BatchLikesCountAsync(IReadOnlyList<Guid> skillIds, CancellationToken ct = default);
    Task<HashSet<Guid>> BatchLikedByUserAsync(IReadOnlyList<Guid> skillIds, Guid userId, CancellationToken ct = default);
    Task<Dictionary<Guid, int>> BatchCommentCountAsync(IReadOnlyList<Guid> skillIds, CancellationToken ct = default);
    Task<HashSet<string>> BatchInstalledNamesAsync(CancellationToken ct = default);
    Task<HashSet<string>> BatchConfiguredNamesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SkillCommentRecord>> ListCommentsBySkillAsync(Guid skillId, CancellationToken ct = default);

    // Social mutations
    Task<bool> AddLikeAsync(Guid skillId, Guid userId, CancellationToken ct = default);
    Task<bool> RemoveLikeAsync(Guid skillId, Guid userId, CancellationToken ct = default);
    Task<SkillCommentRecord> AddCommentAsync(Guid skillId, Guid userId, string body, CancellationToken ct = default);
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken ct = default);
}

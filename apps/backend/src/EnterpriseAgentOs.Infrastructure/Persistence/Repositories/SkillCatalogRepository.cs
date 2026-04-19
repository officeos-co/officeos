namespace EnterpriseAgentOs.Infrastructure.Persistence.Repositories;

public sealed class SkillCatalogRepository : EnterpriseAgentOs.Domain.Interfaces.Skills.ISkillCatalogRepository
{
    private readonly EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext _db;

    public SkillCatalogRepository(EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _db.Skills.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillRecord>> ListActiveAsync(CancellationToken ct = default)
    {
        return await _db.Skills.AsNoTracking()
            .Where(s => s.Status == "active")
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task<EnterpriseAgentOs.Domain.Models.SkillRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Skills.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<EnterpriseAgentOs.Domain.Models.SkillRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim().ToLowerInvariant();
        return await _db.Skills.FirstOrDefaultAsync(s => s.Name == n, ct);
    }

    public async Task<EnterpriseAgentOs.Domain.Models.SkillRecord> UpsertAsync(EnterpriseAgentOs.Domain.Models.SkillRecord record, CancellationToken ct = default)
    {
        var existing = await _db.Skills.FirstOrDefaultAsync(s => s.Name == record.Name, ct);
        if (existing is null)
        {
            _db.Skills.Add(record);
        }
        else
        {
            existing.Title = record.Title;
            existing.Description = record.Description;
            existing.Doc = record.Doc;
            existing.Logo = record.Logo;
            existing.License = record.License;
            existing.Repository = record.Repository;
            existing.RequiresApproval = record.RequiresApproval;
            existing.Readme = record.Readme;
            existing.Changelog = record.Changelog;
            existing.Category = record.Category;
            existing.AuthorName = record.AuthorName;
            existing.AuthorUrl = record.AuthorUrl;
            existing.Categories = record.Categories;
            existing.Keywords = record.Keywords;
            existing.ActionsJson = record.ActionsJson;
            existing.CredentialFieldsJson = record.CredentialFieldsJson;
            existing.ContributorsJson = record.ContributorsJson;
            existing.BundleS3Key = record.BundleS3Key;
            existing.Version = record.Version;
            existing.Status = record.Status;
            existing.BuildError = record.BuildError;
            existing.IsSystem = record.IsSystem;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
        return existing ?? record;
    }

    public async Task<bool> DeleteByNameAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim().ToLowerInvariant();
        var row = await _db.Skills.FirstOrDefaultAsync(s => s.Name == n, ct);
        if (row is null) return false;
        _db.Skills.Remove(row);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Dictionary<Guid, int>> BatchLikesCountAsync(IReadOnlyList<Guid> skillIds, CancellationToken ct = default)
    {
        return await _db.SkillLikes
            .Where(l => skillIds.Contains(l.SkillId))
            .GroupBy(l => l.SkillId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public async Task<HashSet<Guid>> BatchLikedByUserAsync(IReadOnlyList<Guid> skillIds, Guid userId, CancellationToken ct = default)
    {
        var ids = await _db.SkillLikes
            .Where(l => skillIds.Contains(l.SkillId) && l.UserId == userId)
            .Select(l => l.SkillId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<Dictionary<Guid, int>> BatchCommentCountAsync(IReadOnlyList<Guid> skillIds, CancellationToken ct = default)
    {
        return await _db.SkillComments
            .Where(c => skillIds.Contains(c.SkillId))
            .GroupBy(c => c.SkillId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public async Task<HashSet<string>> BatchInstalledNamesAsync(CancellationToken ct = default)
    {
        var names = await _db.SkillCredentials
            .Where(r => r.Enabled)
            .Select(r => r.SkillName)
            .ToListAsync(ct);
        return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<HashSet<string>> BatchConfiguredNamesAsync(CancellationToken ct = default)
    {
        var names = await _db.SkillCredentials
            .Where(r => r.Enabled && r.EncryptedCredentials != null)
            .Select(r => r.SkillName)
            .ToListAsync(ct);
        return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Domain.Models.SkillCommentRecord>> ListCommentsBySkillAsync(Guid skillId, CancellationToken ct = default)
    {
        return await _db.SkillComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.SkillId == skillId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> AddLikeAsync(Guid skillId, Guid userId, CancellationToken ct = default)
    {
        var existing = await _db.SkillLikes
            .FirstOrDefaultAsync(l => l.SkillId == skillId && l.UserId == userId, ct);
        if (existing is not null) return false;
        _db.SkillLikes.Add(new EnterpriseAgentOs.Domain.Models.SkillLikeRecord { SkillId = skillId, UserId = userId });
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveLikeAsync(Guid skillId, Guid userId, CancellationToken ct = default)
    {
        var existing = await _db.SkillLikes
            .FirstOrDefaultAsync(l => l.SkillId == skillId && l.UserId == userId, ct);
        if (existing is null) return false;
        _db.SkillLikes.Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<EnterpriseAgentOs.Domain.Models.SkillCommentRecord> AddCommentAsync(Guid skillId, Guid userId, string body, CancellationToken ct = default)
    {
        var record = new EnterpriseAgentOs.Domain.Models.SkillCommentRecord
        {
            SkillId = skillId,
            UserId = userId,
            Body = body.Trim(),
        };
        _db.SkillComments.Add(record);
        await _db.SaveChangesAsync(ct);
        // Load user for DTO mapping
        await _db.Entry(record).Reference(c => c.User).LoadAsync(ct);
        return record;
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken ct = default)
    {
        var comment = await _db.SkillComments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment is null) return false;
        if (comment.UserId != userId)
            throw new InvalidOperationException("Only the author may delete a comment.");
        _db.SkillComments.Remove(comment);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}

namespace EnterpriseAgentOs.Infrastructure.Skills;

internal sealed class SkillCatalogRepository : ISkillCatalogRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public SkillCatalogRepository(EaosDbContext db)
    {
        _eaosDbContext = db;
    }

    public async Task<IReadOnlyList<SkillRecord>> ListAsync(CancellationToken ct = default)
    {
        return await _eaosDbContext.Skills.AsNoTracking().OrderBy(s => s.Name).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<SkillRecord>> ListActiveAsync(CancellationToken ct = default)
    {
        return await _eaosDbContext.Skills.AsNoTracking()
            .Where(s => s.Status == "active")
            .OrderBy(s => s.Name)
            .ToListAsync(ct);
    }

    public async Task<SkillRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _eaosDbContext.Skills.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<SkillRecord?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim().ToLowerInvariant();
        return await _eaosDbContext.Skills.FirstOrDefaultAsync(s => s.Name == n, ct);
    }

    public async Task<SkillRecord> UpsertAsync(SkillRecord record, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.Skills.FirstOrDefaultAsync(s => s.Name == record.Name, ct);
        if (existing is null)
        {
            _eaosDbContext.Skills.Add(record);
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
        await _eaosDbContext.SaveChangesAsync(ct);
        return existing ?? record;
    }

    public async Task<bool> DeleteByNameAsync(string name, CancellationToken ct = default)
    {
        var n = name.Trim().ToLowerInvariant();
        var row = await _eaosDbContext.Skills.FirstOrDefaultAsync(s => s.Name == n, ct);
        if (row is null) return false;
        _eaosDbContext.Skills.Remove(row);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Dictionary<Guid, int>> BatchLikesCountAsync(IReadOnlyList<Guid> skillIds, CancellationToken ct = default)
    {
        return await _eaosDbContext.SkillLikes
            .Where(l => skillIds.Contains(l.SkillId))
            .GroupBy(l => l.SkillId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public async Task<HashSet<Guid>> BatchLikedByUserAsync(IReadOnlyList<Guid> skillIds, Guid userId, CancellationToken ct = default)
    {
        var ids = await _eaosDbContext.SkillLikes
            .Where(l => skillIds.Contains(l.SkillId) && l.UserId == userId)
            .Select(l => l.SkillId)
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    public async Task<Dictionary<Guid, int>> BatchCommentCountAsync(IReadOnlyList<Guid> skillIds, CancellationToken ct = default)
    {
        return await _eaosDbContext.SkillComments
            .Where(c => skillIds.Contains(c.SkillId))
            .GroupBy(c => c.SkillId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);
    }

    public async Task<HashSet<string>> BatchInstalledNamesAsync(CancellationToken ct = default)
    {
        var names = await _eaosDbContext.SkillCredentials
            .Where(r => r.Enabled)
            .Select(r => r.SkillName)
            .ToListAsync(ct);
        return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public async Task<HashSet<string>> BatchConfiguredNamesAsync(CancellationToken ct = default)
    {
        // Single query: all installed skill names + whether they have manual credentials
        var installed = await _eaosDbContext.SkillCredentials
            .Where(r => r.Enabled)
            .Select(r => new { r.SkillName, HasCreds = r.EncryptedCredentials != null })
            .ToListAsync(ct);

        var result = installed
            .Where(r => r.HasCreds)
            .Select(r => r.SkillName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Load connected OAuth tokens with their granted scopes
        var oauthTokens = await _eaosDbContext.OAuthTokens
            .Where(t => t.EncryptedAccessToken != null)
            .Include(t => t.GrantedScopes)
            .ToListAsync(ct);

        var providerTokens = oauthTokens.ToDictionary(t => t.Provider, StringComparer.OrdinalIgnoreCase);

        if (providerTokens.Count == 0)
            return result;

        // For installed skills without manual creds, check if they use a connected OAuth provider
        var unconfiguredNames = installed
            .Where(r => !r.HasCreds)
            .Select(r => r.SkillName)
            .ToList();

        if (unconfiguredNames.Count == 0)
            return result;

        // Single query: fetch credential field JSON for unconfigured installed skills
        var skillFields = await _eaosDbContext.Skills
            .Where(s => unconfiguredNames.Contains(s.Name) && s.CredentialFieldsJson != null)
            .Select(s => new { s.Name, s.CredentialFieldsJson })
            .ToListAsync(ct);

        var jsonOpts = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        foreach (var sf in skillFields)
        {
            var fields = JsonSerializer.Deserialize<List<RuntimeCredentialField>>(sf.CredentialFieldsJson!, jsonOpts);
            var oauthField = fields?.FirstOrDefault(f => f.Kind == "oauth2" && f.Oauth2 is not null);
            if (oauthField is null) continue;

            var provider = oauthField.Oauth2!.Provider;
            if (!providerTokens.TryGetValue(provider, out var token)) continue;

            // Check that the token's granted scopes cover all scopes required by this skill
            if (token.CoversScopes(oauthField.Oauth2.Scopes ?? []))
                result.Add(sf.Name);
        }

        return result;
    }

    public async Task<IReadOnlyList<SkillCommentRecord>> ListCommentsBySkillAsync(Guid skillId, CancellationToken ct = default)
    {
        return await _eaosDbContext.SkillComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.SkillId == skillId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<bool> AddLikeAsync(Guid skillId, Guid userId, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.SkillLikes
            .FirstOrDefaultAsync(l => l.SkillId == skillId && l.UserId == userId, ct);
        if (existing is not null) return false;
        _eaosDbContext.SkillLikes.Add(new SkillLikeRecord { SkillId = skillId, UserId = userId });
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveLikeAsync(Guid skillId, Guid userId, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.SkillLikes
            .FirstOrDefaultAsync(l => l.SkillId == skillId && l.UserId == userId, ct);
        if (existing is null) return false;
        _eaosDbContext.SkillLikes.Remove(existing);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<SkillCommentRecord> AddCommentAsync(Guid skillId, Guid userId, string body, CancellationToken ct = default)
    {
        var record = new SkillCommentRecord
        {
            SkillId = skillId,
            UserId = userId,
            Body = body.Trim(),
        };
        _eaosDbContext.SkillComments.Add(record);
        await _eaosDbContext.SaveChangesAsync(ct);
        // Load user for DTO mapping
        await _eaosDbContext.Entry(record).Reference(c => c.User).LoadAsync(ct);
        return record;
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId, CancellationToken ct = default)
    {
        var comment = await _eaosDbContext.SkillComments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
        if (comment is null) return false;
        if (comment.UserId != userId)
            throw new InvalidOperationException("Only the author may delete a comment.");
        _eaosDbContext.SkillComments.Remove(comment);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }
}

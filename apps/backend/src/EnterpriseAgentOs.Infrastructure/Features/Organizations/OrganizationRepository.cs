namespace EnterpriseAgentOs.Infrastructure.Features.Organizations;

internal sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly EaosDbContext _eaosDbContext;

    public OrganizationRepository(EaosDbContext db) => _eaosDbContext = db;

    public async Task<OrganizationRecord> GetOrCreateDefaultAsync(
        Guid ownerUserId,
        string ownerEmail,
        string? ownerName,
        CancellationToken ct = default)
    {
        var owned = await _eaosDbContext.Organizations
            .FirstOrDefaultAsync(o => o.OwnerUserId == ownerUserId, ct);
        if (owned is not null) return ToOrganizationRecord(owned);

        var orgEntity = new OrganizationEntity
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(ownerName) ? "My Organization" : $"{ownerName}'s Organization",
            OwnerUserId = ownerUserId,
            CreatedAt = DateTime.UtcNow,
        };
        _eaosDbContext.Organizations.Add(orgEntity);

        _eaosDbContext.OrgMembers.Add(new OrgMemberEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgEntity.Id,
            UserId = ownerUserId,
            Email = ownerEmail,
            Role = "Owner",
            Status = "active",
            CreatedAt = DateTime.UtcNow,
        });
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToOrganizationRecord(orgEntity);
    }

    public async Task<OrganizationRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
        return entity is null ? null : ToOrganizationRecord(entity);
    }

    public async Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var entities = await _eaosDbContext.OrgMembers
            .Where(m => m.OrganizationId == organizationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
        return entities.Select(ToOrgMemberRecord).ToList();
    }

    public async Task<OrgMemberRecord> AddMemberAsync(
        Guid organizationId, string email, string role, string status, Guid? userId, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.OrgMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.Email == email, ct);
        if (existing is not null) return ToOrgMemberRecord(existing);

        var entity = new OrgMemberEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Email = email,
            Role = role,
            Status = status,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
        };
        _eaosDbContext.OrgMembers.Add(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToOrgMemberRecord(entity);
    }

    public async Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.OrgMembers.FirstOrDefaultAsync(x => x.Id == memberId, ct);
        if (entity is null) return false;
        _eaosDbContext.OrgMembers.Remove(entity);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<OrganizationRecord> RenameAsync(
        Guid organizationId, string name, CancellationToken ct = default)
    {
        var entity = await _eaosDbContext.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            ?? throw new InvalidOperationException($"organization {organizationId} not found");
        entity.Name = name;
        await _eaosDbContext.SaveChangesAsync(ct);
        return ToOrganizationRecord(entity);
    }

    // ── Mapping ──────────────────────────────────────────────────────

    private static OrganizationRecord ToOrganizationRecord(OrganizationEntity e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        OwnerUserId = e.OwnerUserId,
        CreatedAt = e.CreatedAt,
    };

    private static OrganizationEntity ToOrganizationEntity(OrganizationRecord r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        OwnerUserId = r.OwnerUserId,
        CreatedAt = r.CreatedAt,
    };

    private static OrgMemberRecord ToOrgMemberRecord(OrgMemberEntity e) => new()
    {
        Id = e.Id,
        OrganizationId = e.OrganizationId,
        UserId = e.UserId,
        Email = e.Email,
        Role = e.Role,
        Status = e.Status,
        CreatedAt = e.CreatedAt,
    };

    private static OrgMemberEntity ToOrgMemberEntity(OrgMemberRecord r) => new()
    {
        Id = r.Id,
        OrganizationId = r.OrganizationId,
        UserId = r.UserId,
        Email = r.Email,
        Role = r.Role,
        Status = r.Status,
        CreatedAt = r.CreatedAt,
    };
}

namespace EnterpriseAgentOs.Infrastructure.Organizations;

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
        // Single-tenant-compatible: each user gets one default org. Model supports many.
        var owned = await _eaosDbContext.Organizations
            .FirstOrDefaultAsync(o => o.OwnerUserId == ownerUserId, ct);
        if (owned is not null) return owned;

        var org = new OrganizationRecord
        {
            Name = string.IsNullOrWhiteSpace(ownerName) ? "My Organization" : $"{ownerName}'s Organization",
            OwnerUserId = ownerUserId,
        };
        _eaosDbContext.Organizations.Add(org);

        _eaosDbContext.OrgMembers.Add(new OrgMemberRecord
        {
            OrganizationId = org.Id,
            UserId = ownerUserId,
            Email = ownerEmail,
            Role = "Owner",
            Status = "active",
        });
        await _eaosDbContext.SaveChangesAsync(ct);
        return org;
    }

    public Task<OrganizationRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _eaosDbContext.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<OrgMemberRecord>> ListMembersAsync(
        Guid organizationId, CancellationToken ct = default)
        => await _eaosDbContext.OrgMembers
            .Where(m => m.OrganizationId == organizationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task<OrgMemberRecord> AddMemberAsync(
        Guid organizationId, string email, string role, string status, Guid? userId, CancellationToken ct = default)
    {
        var existing = await _eaosDbContext.OrgMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.Email == email, ct);
        if (existing is not null) return existing;

        var member = new OrgMemberRecord
        {
            OrganizationId = organizationId,
            Email = email,
            Role = role,
            Status = status,
            UserId = userId,
        };
        _eaosDbContext.OrgMembers.Add(member);
        await _eaosDbContext.SaveChangesAsync(ct);
        return member;
    }

    public async Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var m = await _eaosDbContext.OrgMembers.FirstOrDefaultAsync(x => x.Id == memberId, ct);
        if (m is null) return false;
        _eaosDbContext.OrgMembers.Remove(m);
        await _eaosDbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<OrganizationRecord> RenameAsync(
        Guid organizationId, string name, CancellationToken ct = default)
    {
        var org = await _eaosDbContext.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            ?? throw new InvalidOperationException($"organization {organizationId} not found");
        org.Name = name;
        await _eaosDbContext.SaveChangesAsync(ct);
        return org;
    }
}

namespace EnterpriseAgentOs.Api.Entities.Organizations;

public sealed class OrganizationRepository : IOrganizationRepository
{
    private readonly EnterpriseAgentOs.Api.Database.EaosDbContext _db;

    public OrganizationRepository(EnterpriseAgentOs.Api.Database.EaosDbContext db) => _db = db;

    public async Task<EnterpriseAgentOs.Api.Database.Models.OrganizationRecord> GetOrCreateDefaultAsync(
        Guid ownerUserId,
        string ownerEmail,
        string? ownerName,
        CancellationToken ct = default)
    {
        // Single-tenant-compatible: each user gets one default org. Model supports many.
        var owned = await _db.Organizations
            .FirstOrDefaultAsync(o => o.OwnerUserId == ownerUserId, ct);
        if (owned is not null) return owned;

        var org = new EnterpriseAgentOs.Api.Database.Models.OrganizationRecord
        {
            Name = string.IsNullOrWhiteSpace(ownerName) ? "My Organization" : $"{ownerName}'s Organization",
            OwnerUserId = ownerUserId,
        };
        _db.Organizations.Add(org);

        _db.OrgMembers.Add(new EnterpriseAgentOs.Api.Database.Models.OrgMemberRecord
        {
            OrganizationId = org.Id,
            UserId = ownerUserId,
            Email = ownerEmail,
            Role = "Owner",
            Status = "active",
        });
        await _db.SaveChangesAsync(ct);
        return org;
    }

    public Task<EnterpriseAgentOs.Api.Database.Models.OrganizationRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Database.Models.OrgMemberRecord>> ListMembersAsync(
        Guid organizationId, CancellationToken ct = default)
        => await _db.OrgMembers
            .Where(m => m.OrganizationId == organizationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);

    public async Task<EnterpriseAgentOs.Api.Database.Models.OrgMemberRecord> AddMemberAsync(
        Guid organizationId, string email, string role, string status, Guid? userId, CancellationToken ct = default)
    {
        var existing = await _db.OrgMembers
            .FirstOrDefaultAsync(m => m.OrganizationId == organizationId && m.Email == email, ct);
        if (existing is not null) return existing;

        var member = new EnterpriseAgentOs.Api.Database.Models.OrgMemberRecord
        {
            OrganizationId = organizationId,
            Email = email,
            Role = role,
            Status = status,
            UserId = userId,
        };
        _db.OrgMembers.Add(member);
        await _db.SaveChangesAsync(ct);
        return member;
    }

    public async Task<bool> RemoveMemberAsync(Guid memberId, CancellationToken ct = default)
    {
        var m = await _db.OrgMembers.FirstOrDefaultAsync(x => x.Id == memberId, ct);
        if (m is null) return false;
        _db.OrgMembers.Remove(m);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<EnterpriseAgentOs.Api.Database.Models.OrganizationRecord> RenameAsync(
        Guid organizationId, string name, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == organizationId, ct)
            ?? throw new InvalidOperationException($"organization {organizationId} not found");
        org.Name = name;
        await _db.SaveChangesAsync(ct);
        return org;
    }
}

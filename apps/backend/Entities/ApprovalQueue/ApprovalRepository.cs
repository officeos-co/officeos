using EnterpriseAgentOs.Api.Database;
using EnterpriseAgentOs.Api.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseAgentOs.Api.Entities.ApprovalQueue;

public sealed class ApprovalRepository : IApprovalRepository
{
    private readonly EaosDbContext _db;

    public ApprovalRepository(EaosDbContext db)
    {
        _db = db;
    }

    public async Task<ApprovalRequestRecord> CreateAsync(ApprovalRequestRecord record, CancellationToken ct = default)
    {
        _db.ApprovalRequests.Add(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }

    public async Task<ApprovalRequestRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.ApprovalRequests.FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<List<ApprovalRequestRecord>> GetPendingAsync(int limit, int offset, CancellationToken ct = default)
    {
        return await _db.ApprovalRequests
            .Where(a => a.Status == "pending")
            .OrderByDescending(a => a.RequestedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(ct);
    }

    public async Task<List<ApprovalRequestRecord>> GetByAgentAsync(Guid agentId, CancellationToken ct = default)
    {
        return await _db.ApprovalRequests
            .Where(a => a.AgentId == agentId)
            .OrderByDescending(a => a.RequestedAt)
            .ToListAsync(ct);
    }

    public async Task<ApprovalRequestRecord> UpdateAsync(ApprovalRequestRecord record, CancellationToken ct = default)
    {
        _db.ApprovalRequests.Update(record);
        await _db.SaveChangesAsync(ct);
        return record;
    }
}

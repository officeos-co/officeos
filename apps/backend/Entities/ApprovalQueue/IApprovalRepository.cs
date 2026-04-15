using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.ApprovalQueue;

public interface IApprovalRepository
{
    Task<ApprovalRequestRecord> CreateAsync(ApprovalRequestRecord record, CancellationToken ct = default);
    Task<ApprovalRequestRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ApprovalRequestRecord>> GetPendingAsync(int limit, int offset, CancellationToken ct = default);
    Task<List<ApprovalRequestRecord>> GetByAgentAsync(Guid agentId, CancellationToken ct = default);
    Task<ApprovalRequestRecord> UpdateAsync(ApprovalRequestRecord record, CancellationToken ct = default);
}

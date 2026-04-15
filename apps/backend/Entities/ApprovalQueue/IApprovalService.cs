using EnterpriseAgentOs.Api.Database.Models;

namespace EnterpriseAgentOs.Api.Entities.ApprovalQueue;

public interface IApprovalService
{
    Task<ApprovalRequestRecord> CreatePendingAsync(Guid agentId, string skillName, string action, string paramsJson, CancellationToken ct = default);
    Task<ApprovalRequestRecord?> ApproveAndExecuteAsync(Guid approvalId, Guid decidedByUserId, CancellationToken ct = default);
    Task<ApprovalRequestRecord?> RejectAsync(Guid approvalId, Guid decidedByUserId, CancellationToken ct = default);
    Task<List<ApprovalRequestRecord>> GetPendingAsync(int limit = 50, int offset = 0, CancellationToken ct = default);
    Task<ApprovalRequestRecord?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ApprovalRequestRecord>> GetByAgentAsync(Guid agentId, CancellationToken ct = default);
}

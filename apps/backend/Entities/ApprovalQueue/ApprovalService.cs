using System.Text.Json;
using EnterpriseAgentOs.Api.Database.Models;
using EnterpriseAgentOs.Api.Entities.Skills;

namespace EnterpriseAgentOs.Api.Entities.ApprovalQueue;

public sealed class ApprovalService : IApprovalService
{
    private readonly IApprovalRepository _repository;
    private readonly ISkillService _skillService;
    private readonly SkillRuntimeClient _runtime;
    private readonly ILogger<ApprovalService> _logger;

    public ApprovalService(
        IApprovalRepository repository,
        ISkillService skillService,
        SkillRuntimeClient runtime,
        ILogger<ApprovalService> logger)
    {
        _repository = repository;
        _skillService = skillService;
        _runtime = runtime;
        _logger = logger;
    }

    public async Task<ApprovalRequestRecord> CreatePendingAsync(
        Guid agentId,
        string skillName,
        string action,
        string paramsJson,
        CancellationToken ct = default)
    {
        var record = new ApprovalRequestRecord
        {
            AgentId = agentId,
            SkillName = skillName,
            Action = action,
            ParamsJson = paramsJson,
            Status = "pending",
            RequestedAt = DateTime.UtcNow,
        };

        return await _repository.CreateAsync(record, ct);
    }

    public async Task<ApprovalRequestRecord?> ApproveAndExecuteAsync(
        Guid approvalId,
        Guid decidedByUserId,
        CancellationToken ct = default)
    {
        var record = await _repository.GetByIdAsync(approvalId, ct);
        if (record is null || record.Status != "pending") return record;

        // Get credentials and execute the skill
        var creds = await _skillService.GetDecryptedCredentialsAsync(record.SkillName, ct);
        if (creds is null)
        {
            _logger.LogWarning("ApproveAndExecute: credentials missing for skill {SkillName}", record.SkillName);
            creds = new Dictionary<string, string>();
        }

        string resultJson;
        try
        {
            var @params = JsonSerializer.Deserialize<Dictionary<string, object>>(record.ParamsJson)
                ?? new Dictionary<string, object>();

            var result = await _runtime.ExecuteAsync(record.SkillName, record.Action, @params, creds, ct: ct);
            resultJson = JsonSerializer.Serialize(new { success = result.Success, result = result.Result, error = result.Error });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApproveAndExecute failed for {SkillName}.{Action}", record.SkillName, record.Action);
            resultJson = JsonSerializer.Serialize(new { success = false, error = ex.Message });
        }

        record.Status = "approved";
        record.DecidedAt = DateTime.UtcNow;
        record.DecidedByUserId = decidedByUserId;
        record.ResultJson = resultJson;

        return await _repository.UpdateAsync(record, ct);
    }

    public async Task<ApprovalRequestRecord?> RejectAsync(
        Guid approvalId,
        Guid decidedByUserId,
        CancellationToken ct = default)
    {
        var record = await _repository.GetByIdAsync(approvalId, ct);
        if (record is null || record.Status != "pending") return record;

        record.Status = "rejected";
        record.DecidedAt = DateTime.UtcNow;
        record.DecidedByUserId = decidedByUserId;

        return await _repository.UpdateAsync(record, ct);
    }

    public Task<List<ApprovalRequestRecord>> GetPendingAsync(int limit = 50, int offset = 0, CancellationToken ct = default)
        => _repository.GetPendingAsync(limit, offset, ct);

    public Task<ApprovalRequestRecord?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _repository.GetByIdAsync(id, ct);

    public Task<List<ApprovalRequestRecord>> GetByAgentAsync(Guid agentId, CancellationToken ct = default)
        => _repository.GetByAgentAsync(agentId, ct);
}

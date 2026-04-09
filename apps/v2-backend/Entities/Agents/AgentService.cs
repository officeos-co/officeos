using EnterpriseAgentOs.Api.Entities.Agents;

namespace EnterpriseAgentOs.Api.Entities.Agents;

public sealed class AgentService : IAgentService
{
    private readonly IAgentRepository _repository;

    public AgentService(IAgentRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<AgentDto>> ListAsync(CancellationToken ct = default)
    {
        var records = await _repository.ListAsync(ct);
        return records.Select(ToDto).ToList();
    }

    public async Task<AgentDto> CreateAsync(CreateAgentRequest request, CancellationToken ct = default)
    {
        var record = new AgentRecord
        {
            Name = request.Name.Trim(),
            Model = string.IsNullOrWhiteSpace(request.Model) ? null : request.Model.Trim(),
            Status = "pending",
        };
        await _repository.AddAsync(record, ct);
        return ToDto(record);
    }

    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        return _repository.SoftDeleteAsync(id, ct);
    }

    private static AgentDto ToDto(AgentRecord record) =>
        new(record.Id, record.Name, record.Model, record.Status, record.CreatedAt);
}

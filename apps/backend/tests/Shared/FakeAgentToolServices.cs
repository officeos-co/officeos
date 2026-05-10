using System.Text.Json;
using OffceOs.Application.Features.Agents;
using OffceOs.Application.Features.Context;
using OffceOs.Domain.Common.Primitives;
using OffceOs.Domain.Features.Agents;
using OffceOs.Domain.Features.Context;
using OffceOs.Domain.Features.Integrations;
using OffceOs.Domain.Features.Management;

namespace OffceOs.Tests.Shared;

public sealed class FakeOrganizationPolicyService : IOrganizationPolicyService
{
    private readonly OrganizationPolicyProfileRecord? _policy;

    public FakeOrganizationPolicyService(OrganizationPolicyProfileRecord? policy = null) => _policy = policy;

    public Task<OrganizationPolicyProfileRecord?> GetEffectiveForWorkspaceAsync(Guid? workspaceId, CancellationToken ct = default) =>
        Task.FromResult(_policy);

    public Task<OrganizationPolicyProfileRecord> GetOrCreateAsync(Guid actorUserId, Guid organizationId, CancellationToken ct = default) =>
        Task.FromResult(_policy ?? OrganizationPolicyProfileRecord.Default(organizationId));

    public Task<OrganizationPolicyProfileRecord> UpdateAsync(Guid actorUserId, OrganizationPolicyProfileRecord profile, CancellationToken ct = default) =>
        Task.FromResult(profile);
}

public sealed class FakeAgentMemoryService : IAgentMemoryService
{
    public Task StoreAsync(Guid agentId, string key, string content, CancellationToken ct = default) => Task.CompletedTask;

    public Task<IReadOnlyList<AgentMemoryRecord>> RecallAsync(Guid agentId, string query, int limit, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentMemoryRecord>>([]);

    public Task<bool> ForgetAsync(Guid agentId, string key, CancellationToken ct = default) => Task.FromResult(false);
}

public sealed class FakeAgentCronJobRepository : IAgentCronJobRepository
{
    public Task<IReadOnlyList<AgentCronJobRecord>> ListAsync(Guid agentId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentCronJobRecord>>([]);

    public Task<IReadOnlyList<AgentCronJobWithAgentRecord>> ListForOwnerAsync(Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentCronJobWithAgentRecord>>([]);

    public Task<IReadOnlyList<AgentCronJobRecord>> ListAllEnabledAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentCronJobRecord>>([]);

    public Task<AgentCronJobRecord?> GetByAsync(AgentCronJobFilter filter, CancellationToken ct = default) =>
        Task.FromResult<AgentCronJobRecord?>(null);

    public Task<AgentCronJobWithAgentRecord?> GetForOwnerAsync(Guid id, Guid? ownerId, Guid? workspaceId = null, CancellationToken ct = default) =>
        Task.FromResult<AgentCronJobWithAgentRecord?>(null);

    public Task<AgentCronJobRecord> CreateAsync(Guid agentId, string name, string expression, string prompt, CancellationToken ct = default) =>
        Task.FromResult(AgentCronJobRecord.Create(agentId, name, expression, prompt));

    public Task UpdateAsync(AgentCronJobRecord record, CancellationToken ct = default) => Task.CompletedTask;
    public Task SetEnabledAsync(Guid id, bool enabled, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> DeleteAsync(Guid id, CancellationToken ct = default) => Task.FromResult(false);
}

public sealed class FakeAgentRunRepository : IAgentRunRepository
{
    public Task<AgentRunRecord> CreateAsync(AgentRunRecord run, CancellationToken ct = default) => Task.FromResult(run);
    public Task<AgentRunRecord?> GetByAsync(AgentRunFilter filter, CancellationToken ct = default) => Task.FromResult<AgentRunRecord?>(null);

    public Task<IReadOnlyList<AgentRunRecord>> ListForAgentAsync(Guid agentId, Guid? parentRunId = null, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentRunRecord>>([]);

    public Task UpdateAsync(AgentRunRecord run, CancellationToken ct = default) => Task.CompletedTask;
}

internal sealed class NoBrowserToolContextFactory : IBrowserToolContextFactory
{
    public Task<BrowserToolContext?> CreateForTurnAsync(CancellationToken ct = default) =>
        Task.FromResult<BrowserToolContext?>(null);

    public Task<BrowserToolContext> CreateCatalogAsync(CancellationToken ct = default) =>
        throw new NotSupportedException();
}

public sealed class EmptyAgentDefinitionRepository : IAgentDefinitionRepository
{
    public Task<AgentDefinitionRecord?> GetByAsync(AgentDefinitionFilter filter, CancellationToken ct = default) =>
        Task.FromResult<AgentDefinitionRecord?>(null);

    public Task<IReadOnlyList<AgentDefinitionRecord>> ListAsync(AgentDefinitionFilter filter, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<AgentDefinitionRecord>>([]);

    public Task AddAsync(AgentDefinitionRecord definition, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task<int> GetNextVersionAsync(Guid agentId, CancellationToken ct = default) =>
        Task.FromResult(1);
}

public sealed class FakeIntegrationExecutionService : IIntegrationExecutionService
{
    public Task<JsonElement> ExecuteAsync(IntegrationExecuteRequest request, CancellationToken ct = default) =>
        Task.FromResult(JsonSerializer.SerializeToElement(new { ok = true }));
}

public sealed class CapturingIntegrationExecutionService : IIntegrationExecutionService
{
    public IntegrationExecuteRequest? Request { get; private set; }

    public Task<JsonElement> ExecuteAsync(IntegrationExecuteRequest request, CancellationToken ct = default)
    {
        Request = request;
        return Task.FromResult(JsonSerializer.SerializeToElement(new
        {
            status = "success",
            result = Array.Empty<object>(),
            connector_metadata = (object?)null,
            execution_metadata = new
            {
                connector_instance_id = $"source_id:{request.SourceId}",
                execution_time_ms = 1,
            },
        }));
    }
}

namespace OffceOs.Api.Features.AgentUsage;

[ExtendObjectType(typeof(GraphQLQueries))]
public class AgentUsageQueries
{
    [GraphQLDescription("Returns Codeburn-style usage dashboard analytics for the current user.")]
    public async Task<AgentUsageDashboardResult> GetAgentUsageDashboard(
        AgentUsageInput input,
        [Service] UserContext user,
        [Service] IAgentUsageAnalyticsService agentUsageAnalyticsService,
        CancellationToken ct)
    {
        return await agentUsageAnalyticsService.GetDashboardAsync(user.Id, ToRequest(input), ct);
    }

    [GraphQLDescription("Returns per-model usage, token composition, and activity breakdowns for the current user.")]
    public async Task<AgentUsageModelsResult> GetAgentUsageModels(
        AgentUsageInput input,
        [Service] UserContext user,
        [Service] IAgentUsageAnalyticsService agentUsageAnalyticsService,
        CancellationToken ct)
    {
        return await agentUsageAnalyticsService.GetModelsAsync(user.Id, ToRequest(input), ct);
    }

    [GraphQLDescription("Compares two models over usage, efficiency, and working-style metrics.")]
    public async Task<AgentUsageCompareResult> GetAgentUsageCompare(
        AgentUsageCompareInput input,
        [Service] UserContext user,
        [Service] IAgentUsageAnalyticsService agentUsageAnalyticsService,
        CancellationToken ct)
    {
        return await agentUsageAnalyticsService.CompareModelsAsync(
            user.Id,
            new AgentUsageCompareRequest(
                input.From,
                input.To,
                input.ModelA,
                input.ModelB,
                input.WorkspaceId,
                input.AgentId,
                input.Provider),
            ct);
    }

    [GraphQLDescription("Returns deterministic optimization findings and estimated savings for the current user.")]
    public async Task<AgentUsageOptimizeResult> GetAgentUsageOptimize(
        AgentUsageInput input,
        [Service] UserContext user,
        [Service] IAgentUsageAnalyticsService agentUsageAnalyticsService,
        CancellationToken ct)
    {
        return await agentUsageAnalyticsService.GetOptimizeAsync(user.Id, ToRequest(input), ct);
    }

    [GraphQLDescription("Returns usage data in an export-friendly JSON shape.")]
    public async Task<AgentUsageExport> ExportAgentUsage(
        AgentUsageInput input,
        [Service] UserContext user,
        [Service] IAgentUsageAnalyticsService agentUsageAnalyticsService,
        CancellationToken ct)
    {
        return await agentUsageAnalyticsService.ExportAsync(user.Id, ToRequest(input), ct);
    }

    private static AgentUsageAnalyticsRequest ToRequest(AgentUsageInput input) => new(
        input.From,
        input.To,
        input.WorkspaceId,
        input.AgentId,
        input.Provider,
        input.Model);
}

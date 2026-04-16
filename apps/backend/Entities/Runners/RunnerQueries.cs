namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLQueries))]
public class RunnerQueries
{
    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.Runners.Types.RunnerDto>> GetRunners(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Runners.IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var rows = await runners.ListByOwnerAsync(user.Id, ct);
        return rows.Select(r => EnterpriseAgentOs.Api.Entities.Runners.Types.RunnerGraphQLMapper.ToDto(r)).ToList();
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Runners.Types.RunnerDto?> GetRunner(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Runners.IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var r = await runners.GetByIdAsync(id, ct);
        if (r is null || r.OwnerId != user.Id) return null;
        return EnterpriseAgentOs.Api.Entities.Runners.Types.RunnerGraphQLMapper.ToDto(r);
    }

    public async Task<IReadOnlyList<EnterpriseAgentOs.Api.Entities.Runners.Types.RunnerJobDto>> GetRunnerJobs(
        Guid runnerId,
        string? status,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Runners.IRunnerRepository runners,
        [Service] EnterpriseAgentOs.Api.Entities.Runners.IRunnerJobRepository jobs,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var r = await runners.GetByIdAsync(runnerId, ct);
        if (r is null || r.OwnerId != user.Id)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Runner '{runnerId}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }

        var rows = await jobs.GetRecentByRunnerAsync(runnerId, 100, ct);
        if (!string.IsNullOrWhiteSpace(status))
        {
            rows = rows.Where(j => string.Equals(j.Status, status, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return rows.Select(j => EnterpriseAgentOs.Api.Entities.Runners.Types.RunnerGraphQLMapper.ToDto(j)).ToList();
    }
}

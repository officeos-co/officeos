using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Queries;

[ExtendObjectType(typeof(GraphQLQueries))]
public class RunnerQueries
{
    public async Task<IReadOnlyList<RunnerDto>> GetRunners(
        IResolverContext context,
        [Service] IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var rows = await runners.ListByOwnerAsync(user.Id, ct);
        return rows.Select(r => r.ToDto()).ToList();
    }

    public async Task<RunnerDto?> GetRunner(
        Guid id,
        IResolverContext context,
        [Service] IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var r = await runners.GetByIdAsync(id, ct);
        if (r is null || r.OwnerId != user.Id) return null;
        return r.ToDto();
    }

    public async Task<IReadOnlyList<RunnerJobDto>> GetRunnerJobs(
        Guid runnerId,
        string? status,
        IResolverContext context,
        [Service] IRunnerRepository runners,
        [Service] IRunnerJobRepository jobs,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
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
        return rows.Select(j => j.ToDto()).ToList();
    }
}

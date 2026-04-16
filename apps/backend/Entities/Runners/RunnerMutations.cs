namespace EnterpriseAgentOs.Api.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class RunnerMutations
{
    public async Task<EnterpriseAgentOs.Api.Entities.Runners.Types.CreateRunnerResult> CreateRunner(
        string name,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Runners.IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Name is required.")
                    .SetCode("BAD_INPUT")
                    .Build());
        }

        var registrationToken = $"sr_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}";
        var hash = EnterpriseAgentOs.Api.Middleware.SessionAuthMiddleware.HashToken(registrationToken);
        var runner = await runners.CreateAsync(user.Id, name.Trim(), hash, ct);
        return new EnterpriseAgentOs.Api.Entities.Runners.Types.CreateRunnerResult(EnterpriseAgentOs.Api.Entities.Runners.Types.RunnerGraphQLMapper.ToDto(runner), registrationToken);
    }

    public async Task<EnterpriseAgentOs.Api.Entities.Runners.Types.CreateRunnerResult> RegenerateRunnerToken(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Runners.IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var runner = await runners.GetByIdAsync(id, ct);
        if (runner is null || runner.OwnerId != user.Id)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Runner '{id}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }

        var registrationToken = $"sr_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}";
        runner.RegistrationTokenHash = EnterpriseAgentOs.Api.Middleware.SessionAuthMiddleware.HashToken(registrationToken);
        runner.AuthTokenHash = null;
        runner.Status = "pending";
        await runners.UpdateAsync(runner, ct);
        return new EnterpriseAgentOs.Api.Entities.Runners.Types.CreateRunnerResult(EnterpriseAgentOs.Api.Entities.Runners.Types.RunnerGraphQLMapper.ToDto(runner), registrationToken);
    }

    public async Task<bool> DeleteRunner(
        Guid id,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Api.Entities.Runners.IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var runner = await runners.GetByIdAsync(id, ct);
        if (runner is null || runner.OwnerId != user.Id) return false;
        await runners.DeleteAsync(id, ct);
        return true;
    }
}

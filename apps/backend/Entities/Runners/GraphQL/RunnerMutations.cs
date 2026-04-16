using System.Security.Cryptography;
using HotChocolate.Resolvers;

namespace EnterpriseAgentOs.Api.Entities.Runners.GraphQL;

[ExtendObjectType(typeof(GraphQLMutations))]
public class RunnerMutations
{
    public async Task<CreateRunnerResult> CreateRunner(
        string name,
        IResolverContext context,
        [Service] IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage("Name is required.")
                    .SetCode("BAD_INPUT")
                    .Build());
        }

        var registrationToken = $"sr_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))}";
        var hash = SessionAuthMiddleware.HashToken(registrationToken);
        var runner = await runners.CreateAsync(user.Id, name.Trim(), hash, ct);
        return new CreateRunnerResult(runner.ToDto(), registrationToken);
    }

    public async Task<CreateRunnerResult> RegenerateRunnerToken(
        Guid id,
        IResolverContext context,
        [Service] IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
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
        runner.RegistrationTokenHash = SessionAuthMiddleware.HashToken(registrationToken);
        runner.AuthTokenHash = null;
        runner.Status = "pending";
        await runners.UpdateAsync(runner, ct);
        return new CreateRunnerResult(runner.ToDto(), registrationToken);
    }

    public async Task<bool> DeleteRunner(
        Guid id,
        IResolverContext context,
        [Service] IRunnerRepository runners,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        var runner = await runners.GetByIdAsync(id, ct);
        if (runner is null || runner.OwnerId != user.Id) return false;
        await runners.DeleteAsync(id, ct);
        return true;
    }
}

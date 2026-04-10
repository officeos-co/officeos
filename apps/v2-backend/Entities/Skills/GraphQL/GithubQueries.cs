using EnterpriseAgentOs.Api.Entities.Skills.GraphQL.Types;
using EnterpriseAgentOs.Api.Entities.Skills.Implementations;

namespace EnterpriseAgentOs.Api.Entities.Skills.GraphQL;

[ExtendObjectType("Query")]
public class GithubQueries
{
    [GraphQLDescription("List repositories accessible to the authenticated user")]
    public async Task<List<GithubRepo>> GithubRepos(
        [Service] GithubSkill github,
        [Service] ISkillService skills,
        [GraphQLDescription("all, public, or private")] string visibility = "all",
        [GraphQLDescription("Results per page (1-100)")] int perPage = 30,
        CancellationToken ct = default)
    {
        var creds = await skills.GetDecryptedCredentialsAsync("github", ct)
            ?? throw new GraphQLException("GitHub skill not configured. Install and configure it on the Skills page.");
        return await github.ListReposAsync(new(visibility, perPage), creds, ct);
    }

    [GraphQLDescription("List issues in a repository")]
    public async Task<List<GithubIssue>> GithubIssues(
        [Service] GithubSkill github,
        [Service] ISkillService skills,
        [GraphQLDescription("Repository owner")] string owner,
        [GraphQLDescription("Repository name")] string repo,
        [GraphQLDescription("open, closed, or all")] string state = "open",
        CancellationToken ct = default)
    {
        var creds = await skills.GetDecryptedCredentialsAsync("github", ct)
            ?? throw new GraphQLException("GitHub skill not configured. Install and configure it on the Skills page.");
        return await github.ListIssuesAsync(new(owner, repo, state), creds, ct);
    }

    [GraphQLDescription("List pull requests in a repository")]
    public async Task<List<GithubPr>> GithubPrs(
        [Service] GithubSkill github,
        [Service] ISkillService skills,
        [GraphQLDescription("Repository owner")] string owner,
        [GraphQLDescription("Repository name")] string repo,
        [GraphQLDescription("open, closed, or all")] string state = "open",
        CancellationToken ct = default)
    {
        var creds = await skills.GetDecryptedCredentialsAsync("github", ct)
            ?? throw new GraphQLException("GitHub skill not configured. Install and configure it on the Skills page.");
        return await github.ListPrsAsync(new(owner, repo, state), creds, ct);
    }
}

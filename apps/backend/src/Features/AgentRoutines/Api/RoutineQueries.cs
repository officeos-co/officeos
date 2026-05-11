namespace OffceOs.Api.Features.AgentRoutines;

[ExtendObjectType(typeof(GraphQLQueries))]
public class RoutineQueries
{
    private static readonly IReadOnlyList<GitHubRoutineEventPayload> GitHubEvents =
    [
        new("push", "Push", "Branch and tag updates."),
        new("pull_request", "Pull request", "Pull request opened, updated, synchronized, or closed."),
        new("issues", "Issues", "Issue opened, edited, labeled, assigned, or closed."),
        new("issue_comment", "Issue comments", "Comments on issues and pull requests."),
        new("pull_request_review", "Pull request reviews", "Review submitted, edited, or dismissed."),
        new("pull_request_review_comment", "Review comments", "Comments on pull request diffs."),
        new("release", "Releases", "Release published, edited, or deleted."),
        new("workflow_run", "Workflow runs", "GitHub Actions workflow run activity."),
    ];

    [GraphQLDescription("Lists all routines for agents in the authenticated user's current workspace.")]
    public async Task<IReadOnlyList<AgentRoutinePayload>> GetAgentRoutines(
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var rows = await routines.ListForOwnerAsync(user.Id, workspace.Id, ct);
        return rows.Select(AgentRoutineMapper.ToPayload).ToList();
    }

    [GraphQLDescription("Returns one routine in the authenticated user's current workspace.")]
    public async Task<AgentRoutinePayload?> GetAgentRoutine(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var row = await routines.GetForOwnerAsync(id, user.Id, workspace.Id, ct);
        return row is null ? null : AgentRoutineMapper.ToPayload(row);
    }

    [GraphQLDescription("Lists all routines for a specific agent in the authenticated user's current workspace.")]
    public async Task<IReadOnlyList<AgentRoutineRecord>> GetRoutinesForAgent(
        Guid agentId,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentRoutineService routines,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return await routines.ListForAgentAsync(agentId, user.Id, workspace.Id, ct);
    }

    [GraphQLDescription("Returns GitHub repositories and supported webhook events for routine trigger creation.")]
    public async Task<GitHubRoutineOptionsPayload> GetGitHubRoutineOptions(
        [Service] UserContext user,
        [Service] GitHubIntegrationClient github,
        CancellationToken ct)
    {
        var connected = await github.HasTokenAsync(user.Id, ct);
        var repositories = connected
            ? (await github.ListRepositoriesAsync(user.Id, ct)).Select(AgentRoutineMapper.ToPayload).ToList()
            : [];

        return new GitHubRoutineOptionsPayload(connected, repositories, GitHubEvents);
    }
}

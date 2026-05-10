namespace OffceOs.Api.Features.Agents;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentDashboardMutations
{
    private static string AgentListQueryCacheKey(Guid userId, Guid workspaceId) => AgentCacheKeys.DashboardList(userId, workspaceId);
    private static string AgentQueryCacheKey(Guid id, Guid userId, Guid workspaceId) => AgentCacheKeys.DashboardDetail(id, userId, workspaceId);

    [GraphQLDescription("Creates a new agent with the given config. Optionally assigns skills, tool permissions, and channels.")]
    public async Task<AgentRecord> CreateAgent(
        CreateAgentInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentDashboardService agents,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        try
        {
            var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
            var agent = await agents.CreateAsync(
                new CreateDashboardAgentRequest(
                    input.Name,
                    input.Provider,
                    input.Model,
                    input.Prompt,
                    input.IntegrationSlugs,
                    input.ChannelConnectionIds,
                    input.ToolNames,
                    input.ToolPermissions?.Select(tp => new AgentToolPermissionInit(tp.Tool, tp.Mode)).ToList(),
                    input.Resources?.Select(resource => new AgentResourceAttachmentRequest(
                        resource.ResourceType,
                        resource.ResourceId,
                        resource.AccessMode,
                        resource.Instructions)).ToList(),
                    input.BootstrapMessage),
                user.Id,
                workspace.Id,
                ct);

            await cache.RemoveAsync(AgentListQueryCacheKey(user.Id, workspace.Id), ct);
            return agent;
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode(ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ? "NOT_FOUND" : "VALIDATION")
                    .Build());
        }
    }

    [GraphQLDescription("Patches mutable fields on an existing agent (name, provider, model, prompt). Null fields are left unchanged.")]
    public async Task<AgentRecord> UpdateAgent(
        Guid id,
        UpdateAgentInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentDashboardService agents,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var dto = await agents.PatchAsync(
            id,
            user.Id,
            workspace.Id,
            new PatchAgentRequest(input.Provider, input.Model, input.Name, input.Prompt),
            ct);
        if (dto is null)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage($"Agent '{id}' not found.")
                    .SetCode("NOT_FOUND")
                    .Build());
        }
        await cache.RemoveAsync(AgentListQueryCacheKey(user.Id, workspace.Id), ct);
        await cache.RemoveAsync(AgentQueryCacheKey(id, user.Id, workspace.Id), ct);
        return dto;
    }

    [GraphQLDescription("Soft-deletes an agent and removes its Kubernetes pod.")]
    public async Task<bool> DeleteAgent(
        Guid id,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentDashboardService agents,
        [Service] IDistributedCache cache,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        var result = await agents.DeleteAsync(id, user.Id, workspace.Id, ct);
        await cache.RemoveAsync(AgentListQueryCacheKey(user.Id, workspace.Id), ct);
        await cache.RemoveAsync(AgentQueryCacheKey(id, user.Id, workspace.Id), ct);
        return result;
    }

    [GraphQLDescription("Sets one explicit tool permission override for an agent.")]
    public async Task<ToolPermissionPayload> SetAgentToolPermission(
        SetAgentToolPermissionInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentDashboardService agents,
        CancellationToken ct)
    {
        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        await agents.SetToolPermissionAsync(user.Id, workspace.Id, input.AgentId, input.Skill, input.Tool, input.Mode, ct);
        return new ToolPermissionPayload(input.Skill, input.Tool, input.Mode);
    }

    [GraphQLDescription("Replaces explicit tool permission overrides for an agent.")]
    public async Task<IReadOnlyList<ToolPermissionPayload>> SetAgentToolPermissions(
        SetAgentToolPermissionsInput input,
        [Service] UserContext user,
        [Service] IWorkspaceService workspaces,
        [Service] IAgentDashboardService agents,
        CancellationToken ct)
    {
        var rows = input.Entries.Select(e => new AgentToolPermissionRecord
        {
            AgentId = input.AgentId,
            SkillName = e.Skill,
            ToolName = e.Tool,
            Permission = e.Mode,
        }).ToList();

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        await agents.SetToolPermissionsAsync(user.Id, workspace.Id, input.AgentId, rows, ct);
        return rows.Select(p => new ToolPermissionPayload(p.SkillName, p.ToolName, p.Permission)).ToList();
    }
}

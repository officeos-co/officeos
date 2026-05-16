namespace OffceOs.Api.Features.Agents;

[ApiController]
[Route("api/v1")]
public sealed class AgentResourceController : ControllerBase
{
    [HttpGet("resources/agents")]
    [HttpGet("resources/agent")]
    public async Task<IActionResult> ListAgents(
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRepository agents,
        [FromServices] IResourceLogService logs,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var agentRecords = await agents.ListAsync(new AgentFilter { WorkspaceId = scope.Value.WorkspaceId }, ct);
        var workLogs = await logs.ListAsync(new ResourceLogQueryRequest(
            WorkspaceId: scope.Value.WorkspaceId,
            Type: ResourceLogType.MessageIn,
            WorkStatus: string.Empty,
            Limit: 1000), ct);
        return Ok(agentRecords.Select(agent => ToAgentResource(agent, AgentHealthProjection.From(agent, workLogs.Items))));
    }

    [HttpGet("resources/agents/{name}")]
    [HttpGet("resources/agent/{name}")]
    public async Task<IActionResult> DescribeAgent(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRepository agents,
        [FromServices] IResourceLogService logs,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var agent = await FindAgentAsync(agents, name, scope.Value.WorkspaceId, ct);
        if (agent is null)
        {
            return NotFound(new { error = $"agents/{name} was not found." });
        }

        var workLogs = await logs.ListAsync(new ResourceLogQueryRequest(
            WorkspaceId: scope.Value.WorkspaceId,
            AgentId: agent.Id,
            Type: ResourceLogType.MessageIn,
            Limit: 1000), ct);
        return Ok(ToAgentDetailsResource(agent, AgentHealthProjection.From(agent, workLogs.Items)));
    }

    [HttpDelete("resources/agents/{name}")]
    [HttpDelete("resources/agent/{name}")]
    public async Task<IActionResult> DeleteAgent(
        string name,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentLifecycleService agents,
        [FromServices] IAgentRepository agentRepository,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var agent = await FindAgentAsync(agentRepository, name, scope.Value.WorkspaceId, ct);
        return agent is not null &&
            await agents.DeleteAsync(agent.Id, scope.Value.UserId, scope.Value.WorkspaceId, ct)
            ? Ok(new { deleted = true })
            : NotFound(new { error = $"agents/{name} was not found." });
    }

    [HttpPost("resources/agents/{name}/messages")]
    [HttpPost("resources/agent/{name}/messages")]
    public async Task<IActionResult> SendAgentMessage(
        string name,
        [FromBody] AgentMessageInput input,
        [FromServices] IWorkspaceService workspaces,
        [FromServices] IAgentRepository agentRepository,
        [FromServices] IAgentService agents,
        CancellationToken ct)
    {
        var scope = await RequireScopeAsync(workspaces, ct);
        if (scope is null) return Unauthorized(new { error = "Unauthenticated." });

        var agent = await FindAgentAsync(agentRepository, name, scope.Value.WorkspaceId, ct);
        if (agent is null)
            return NotFound(new { error = $"agents/{name} was not found." });

        if (string.IsNullOrWhiteSpace(input.Message))
            return BadRequest(new { error = "Message is required." });

        var work = await agents.SendMessageAsync(
            agent.Id,
            input.Message.Trim(),
            scope.Value.UserId,
            ct,
            string.IsNullOrWhiteSpace(input.Purpose) ? AgentWorkPurposeKinds.Manual : input.Purpose,
            agent.ActiveDefinitionId);

        return Ok(new
        {
            kind = "AgentWork",
            agentId = agent.Id,
            agentName = agent.Name,
            workLogId = work.Id,
            correlationId = work.CorrelationId,
            status = work.WorkStatus,
            purpose = work.WorkPurpose,
            createdAt = work.Time,
        });
    }

    private async Task<(Guid UserId, Guid WorkspaceId)?> RequireScopeAsync(IWorkspaceService workspaces, CancellationToken ct)
    {
        if (HttpContext.Items["User"] is not UserRecord user)
            return null;

        var workspace = await workspaces.GetCurrentAsync(user.Id, ct);
        return (user.Id, workspace.Id);
    }

    private static object ToAgentResource(AgentRecord agent, AgentHealthResult health) => new
    {
        kind = "Agent",
        name = agent.Name,
        id = agent.Id,
        provider = agent.Provider,
        model = agent.Model,
        status = health.Status,
        rawStatus = agent.Status.ToString(),
        health,
        createdAt = agent.CreatedAt,
    };

    private static object ToAgentDetailsResource(AgentRecord agent, AgentHealthResult health) => new
    {
        kind = "Agent",
        name = agent.Name,
        id = agent.Id,
        provider = agent.Provider,
        model = agent.Model,
        status = health.Status,
        rawStatus = agent.Status.ToString(),
        health,
        prompt = agent.Prompt,
        systemPrompt = SystemPromptComposer.Compose(agent),
        podName = agent.PodName,
        serviceUrl = agent.ServiceUrl,
        activeDefinitionId = agent.ActiveDefinitionId,
        workspaceId = agent.WorkspaceId,
        createdAt = agent.CreatedAt,
        personalityFiles = agent.PersonalityFiles.OrderBy(file => file.CompositionOrder).Select(file => new
        {
            file.FileName,
            file.Content,
            file.CreatedAt,
            file.UpdatedAt,
        }),
        memories = agent.Memories.Select(memory => new
        {
            memory.Key,
            memory.Content,
            memory.CreatedAt,
            memory.UpdatedAt,
        }),
        channelBindings = agent.ChannelBindings.Select(binding => new
        {
            binding.Id,
            binding.ChannelConnectionId,
            binding.Enabled,
            binding.Config,
            binding.CreatedAt,
        }),
        activeSession = agent.ActiveSession is null
            ? null
            : new
            {
                agent.ActiveSession.Id,
                status = agent.ActiveSession.Status.ToString(),
                agent.ActiveSession.MessageCount,
                agent.ActiveSession.LastActivityAt,
                agent.ActiveSession.CreatedAt,
                agent.ActiveSession.EndedAt,
            },
    };

    private static async Task<AgentRecord?> FindAgentAsync(
        IAgentRepository agents,
        string name,
        Guid workspaceId,
        CancellationToken ct)
    {
        if (Guid.TryParse(name, out var id))
            return await agents.GetByAsync(new AgentFilter { Id = id, WorkspaceId = workspaceId }, ct);

        var matches = await agents.ListAsync(new AgentFilter { WorkspaceId = workspaceId }, ct);
        var match = matches.FirstOrDefault(agent => agent.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        return match is null
            ? null
            : await agents.GetByAsync(new AgentFilter { Id = match.Id, WorkspaceId = workspaceId }, ct);
    }
}

public sealed record AgentMessageInput(string Message, string? Purpose = null);

namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(GraphQLMutations))]
public class AgentMutations
{
    public async Task<AgentDto> CreateAgent(
        Types.CreateAgentInput input,
        IResolverContext context,
        [Service] IAgentService agents,
        [Service] IAgentSkillRepository agentSkills,
        [Service] IChannelRepository channels,
        [Service] EaosDbContext db,
        CancellationToken ct)
    {
        var user = Middleware.DashboardAuthContextExtensions.GetUser(context);
        AgentDto dto;
        try
        {
            dto = await agents.CreateAsync(
                new CreateAgentRequest(input.Name, input.Provider, input.Model, input.Prompt),
                ownerId: user.Id,
                ct);
        }
        catch (InvalidOperationException ex)
        {
            throw new GraphQLException(
                ErrorBuilder.New()
                    .SetMessage(ex.Message)
                    .SetCode("VALIDATION")
                    .Build());
        }

        // Installed skills — accept either IntegrationSlugs (legacy) or ToolNames.
        var toolNames = input.ToolNames is { Count: > 0 }
            ? input.ToolNames
            : input.IntegrationSlugs;
        if (toolNames is { Count: > 0 })
        {
            await agentSkills.AssignAsync(dto.Id, toolNames, ct);
        }

        // Persist per-tool allow/deny overrides. Keys from the dashboard
        // arrive as "skill:tool"; split here. Rows with no ":" are treated
        // as skill-level defaults with an empty tool name.
        if (input.ToolPermissions is { Count: > 0 })
        {
            foreach (var tp in input.ToolPermissions)
            {
                var (skill, tool) = SplitToolKey(tp.Tool);
                db.AgentToolPermissions.Add(new AgentToolPermissionRecord
                {
                    AgentId = dto.Id,
                    SkillName = skill,
                    ToolName = tool,
                    Permission = tp.Mode,
                });
            }
            await db.SaveChangesAsync(ct);
        }

        if (input.ChannelSlugs is { Count: > 0 })
        {
            var connections = await channels.ListConnectionsAsync(ct);
            foreach (var slug in input.ChannelSlugs)
            {
                var match = connections.FirstOrDefault(c =>
                    string.Equals(c.ChannelType, slug, StringComparison.OrdinalIgnoreCase));
                if (match is null) continue; // silently skip
                try
                {
                    await channels.CreateBindingAsync(new AgentChannelBindingRecord
                    {
                        AgentId = dto.Id,
                        ChannelConnectionId = match.Id,
                    }, ct);
                }
                catch (DbUpdateException)
                {
                    // already bound — skip
                }
            }
        }

        return dto;
    }

    private static (string Skill, string Tool) SplitToolKey(string key)
    {
        var k = (key ?? string.Empty).Trim();
        var idx = k.IndexOf(':');
        if (idx <= 0) return (k.ToLowerInvariant(), string.Empty);
        return (k[..idx].Trim().ToLowerInvariant(), k[(idx + 1)..].Trim());
    }

    public async Task<AgentDto> UpdateAgent(
        Guid id,
        Types.UpdateAgentInput input,
        IResolverContext context,
        [Service] IAgentService agents,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        var dto = await agents.PatchAsync(
            id,
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
        return dto;
    }

    public async Task<bool> DeleteAgent(
        Guid id,
        IResolverContext context,
        [Service] IAgentService agents,
        CancellationToken ct)
    {
        _ = Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await agents.DeleteAsync(id, ct);
    }
}

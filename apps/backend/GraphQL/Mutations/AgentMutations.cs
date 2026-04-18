namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class AgentMutations
{
    public async Task<EnterpriseAgentOs.Domain.DTOs.Agents.AgentDto> CreateAgent(
        EnterpriseAgentOs.Api.GraphQL.Types.CreateAgentInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Agents.IAgentService agents,
        [Service] EnterpriseAgentOs.Domain.Interfaces.AgentSkills.IAgentSkillRepository agentSkills,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Channels.IChannelRepository channels,
        [Service] EnterpriseAgentOs.Infrastructure.Persistence.EaosDbContext db,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        EnterpriseAgentOs.Domain.DTOs.Agents.AgentDto dto;
        try
        {
            dto = await agents.CreateAsync(
                new EnterpriseAgentOs.Domain.DTOs.Agents.CreateAgentRequest(input.Name, input.Provider, input.Model, input.Prompt),
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
                db.AgentToolPermissions.Add(new EnterpriseAgentOs.Domain.Models.AgentToolPermissionRecord
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
                    await channels.CreateBindingAsync(new EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord
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

    public async Task<EnterpriseAgentOs.Domain.DTOs.Agents.AgentDto> UpdateAgent(
        Guid id,
        EnterpriseAgentOs.Api.GraphQL.Types.UpdateAgentInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.Agents.IAgentService agents,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var dto = await agents.PatchAsync(
            id,
            new EnterpriseAgentOs.Domain.Interfaces.Agents.PatchAgentRequest(input.Provider, input.Model, input.Name, input.Prompt),
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
        [Service] EnterpriseAgentOs.Domain.Interfaces.Agents.IAgentService agents,
        CancellationToken ct)
    {
        _ = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        return await agents.DeleteAsync(id, ct);
    }
}

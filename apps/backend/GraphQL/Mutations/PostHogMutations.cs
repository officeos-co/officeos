namespace EnterpriseAgentOs.Api.GraphQL.Mutations;

// One mutation per dashboard use case. The GraphQL schema lists every event
// we ever fire — no generic `captureEvent(name, properties)` escape hatch.
[ExtendObjectType(typeof(EnterpriseAgentOs.Api.GraphQLMutations))]
public class PostHogMutations
{
    public Task<bool> TrackPageView(
        EnterpriseAgentOs.Domain.DTOs.PostHog.TrackPageViewInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.PostHog.IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "$pageview", new() { ["path"] = input.Path }, ct);

    public Task<bool> TrackNavClicked(
        EnterpriseAgentOs.Domain.DTOs.PostHog.TrackNavClickedInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.PostHog.IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "nav_clicked", new() { ["destination"] = input.Destination }, ct);

    public Task<bool> TrackSkillInstalled(
        EnterpriseAgentOs.Domain.DTOs.PostHog.TrackSkillInstalledInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.PostHog.IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "skill_installed", new() { ["skill_name"] = input.SkillName }, ct);

    public Task<bool> TrackSkillConfigured(
        EnterpriseAgentOs.Domain.DTOs.PostHog.TrackSkillConfiguredInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.PostHog.IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "skill_configured", new() { ["skill_name"] = input.SkillName }, ct);

    public Task<bool> TrackChannelConnected(
        EnterpriseAgentOs.Domain.DTOs.PostHog.TrackChannelConnectedInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.PostHog.IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "channel_connected", new() { ["channel_slug"] = input.ChannelSlug }, ct);

    public Task<bool> TrackAgentCreated(
        EnterpriseAgentOs.Domain.DTOs.PostHog.TrackAgentCreatedInput input,
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.PostHog.IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "agent_created", new()
        {
            ["agent_name"] = input.AgentName,
            ["provider"] = input.Provider,
            ["template"] = input.Template,
            ["skill_count"] = input.SkillCount,
            ["allow_skills"] = input.AllowSkills,
            ["deny_skills"] = input.DenySkills,
        }, ct);

    public async Task<bool> IdentifyUser(
        IResolverContext context,
        [Service] EnterpriseAgentOs.Domain.Interfaces.PostHog.IPostHogService posthog,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        var traits = new Dictionary<string, object?>
        {
            ["email"] = user.Email,
            ["name"] = user.Name,
        };
        await posthog.IdentifyAsync(user.Id.ToString(), traits, ct);
        return true;
    }

    private static async Task<bool> Capture(
        IResolverContext context,
        EnterpriseAgentOs.Domain.Interfaces.PostHog.IPostHogService posthog,
        string eventName,
        Dictionary<string, object?> properties,
        CancellationToken ct)
    {
        var user = EnterpriseAgentOs.Api.Middleware.DashboardAuthContextExtensions.GetUser(context);
        await posthog.CaptureAsync(user.Id.ToString(), eventName, properties, ct);
        return true;
    }
}

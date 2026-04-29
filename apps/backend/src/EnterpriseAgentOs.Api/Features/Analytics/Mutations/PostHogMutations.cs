namespace EnterpriseAgentOs.Api.Features.Analytics;

// One mutation per dashboard use case. The GraphQL schema lists every event
// we ever fire — no generic `captureEvent(name, properties)` escape hatch.
[ExtendObjectType(typeof(GraphQLMutations))]
public class PostHogMutations
{
    [GraphQLDescription("Fires a PostHog $pageview event with the given path.")]
    public Task<bool> TrackPageView(
        TrackPageViewInput input,
        IResolverContext context,
        [Service] IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "$pageview", new() { ["path"] = input.Path }, ct);

    [GraphQLDescription("Fires a PostHog nav_clicked event with the navigation destination.")]
    public Task<bool> TrackNavClicked(
        TrackNavClickedInput input,
        IResolverContext context,
        [Service] IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "nav_clicked", new() { ["destination"] = input.Destination }, ct);

    [GraphQLDescription("Fires a PostHog skill_installed event with the skill name.")]
    public Task<bool> TrackSkillInstalled(
        TrackSkillInstalledInput input,
        IResolverContext context,
        [Service] IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "skill_installed", new() { ["skill_name"] = input.SkillName }, ct);

    [GraphQLDescription("Fires a PostHog skill_configured event with the skill name.")]
    public Task<bool> TrackSkillConfigured(
        TrackSkillConfiguredInput input,
        IResolverContext context,
        [Service] IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "skill_configured", new() { ["skill_name"] = input.SkillName }, ct);

    [GraphQLDescription("Fires a PostHog channel_connected event with the channel slug.")]
    public Task<bool> TrackChannelConnected(
        TrackChannelConnectedInput input,
        IResolverContext context,
        [Service] IPostHogService posthog,
        CancellationToken ct)
        => Capture(context, posthog, "channel_connected", new() { ["channel_slug"] = input.ChannelSlug }, ct);

    [GraphQLDescription("Fires a PostHog agent_created event with agent name, provider, template, and skill counts.")]
    public Task<bool> TrackAgentCreated(
        TrackAgentCreatedInput input,
        IResolverContext context,
        [Service] IPostHogService posthog,
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

    [GraphQLDescription("Calls PostHog identify with the authenticated user's email and name.")]
    public async Task<bool> IdentifyUser(
        IResolverContext context,
        [Service] IPostHogService posthog,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
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
        IPostHogService posthog,
        string eventName,
        Dictionary<string, object?> properties,
        CancellationToken ct)
    {
        var user = DashboardAuthContextExtensions.GetUser(context);
        await posthog.CaptureAsync(user.Id.ToString(), eventName, properties, ct);
        return true;
    }
}

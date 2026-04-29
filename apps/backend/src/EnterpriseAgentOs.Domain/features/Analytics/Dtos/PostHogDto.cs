namespace EnterpriseAgentOs.Domain.Features.Analytics;

// One input record per tracked use-case in dashboard. Keep them flat and
// strongly typed — the goal is to make the GraphQL schema the source of truth
// for which events exist, not a generic string/JSON wrapper.

public record TrackPageViewInput(string Path);

public record TrackNavClickedInput(string Destination);

public record TrackSkillInstalledInput(string SkillName);

public record TrackSkillConfiguredInput(string SkillName);

public record TrackChannelConnectedInput(string ChannelSlug);

public record TrackAgentCreatedInput(
    string AgentName,
    string Provider,
    string Template,
    int SkillCount,
    int AllowSkills,
    int DenySkills);

namespace EnterpriseAgentOs.Application.Models;

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

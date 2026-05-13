namespace OffceOs.Domain.Features.Channels;

/// <summary>
/// Static definitions of supported channel types and their config field schemas.
/// </summary>
public static class ChannelKinds
{
    public static readonly IReadOnlyList<ChannelTypeDefinition> All = new[]
    {
        new ChannelTypeDefinition("internal", "Internal Agent Channel", "Let agents message each other inside EnterpriseAgentOs",
            string.Empty,
            Array.Empty<OnboardingStep>()),
        new ChannelTypeDefinition("slack", "Slack", "Connect a Slack workspace",
            "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M5.04 15.16a2.53 2.53 0 0 1-2.52 2.53A2.53 2.53 0 0 1 0 15.16a2.53 2.53 0 0 1 2.52-2.52h2.52v2.52zm1.27 0a2.53 2.53 0 0 1 2.52-2.52 2.53 2.53 0 0 1 2.52 2.52v6.32A2.53 2.53 0 0 1 8.83 24a2.53 2.53 0 0 1-2.52-2.52v-6.32zM8.83 5.04a2.53 2.53 0 0 1-2.52-2.52A2.53 2.53 0 0 1 8.83 0a2.53 2.53 0 0 1 2.52 2.52v2.52H8.83zm0 1.27a2.53 2.53 0 0 1 2.52 2.52 2.53 2.53 0 0 1-2.52 2.52H2.52A2.53 2.53 0 0 1 0 8.83a2.53 2.53 0 0 1 2.52-2.52h6.31zm10.13 2.52a2.53 2.53 0 0 1 2.52-2.52A2.53 2.53 0 0 1 24 8.83a2.53 2.53 0 0 1-2.52 2.52h-2.52V8.83zm-1.27 0a2.53 2.53 0 0 1-2.52 2.52 2.53 2.53 0 0 1-2.52-2.52V2.52A2.53 2.53 0 0 1 15.17 0a2.53 2.53 0 0 1 2.52 2.52v6.31zm-2.52 10.13a2.53 2.53 0 0 1 2.52 2.52A2.53 2.53 0 0 1 15.17 24a2.53 2.53 0 0 1-2.52-2.52v-2.52h2.52zm0-1.27a2.53 2.53 0 0 1-2.52-2.52 2.53 2.53 0 0 1 2.52-2.52h6.31A2.53 2.53 0 0 1 24 15.17a2.53 2.53 0 0 1-2.52 2.52h-6.31z\"/></svg>",
            new OnboardingStep[]
            {
                new("input", "Bot Token", "Paste the Bot User OAuth Token from your Slack app settings.", InputKey: "botToken", InputKind: "password", InputPlaceholder: "xoxb-...", InputHelp: "Found under OAuth & Permissions"),
                new("input", "Signing Secret", "Used to verify incoming webhook requests from Slack.", InputKey: "signingSecret", InputKind: "password", InputHelp: "Found under Basic Information → App Credentials"),
            }),
        new ChannelTypeDefinition("telegram", "Telegram", "Connect a Telegram bot",
            "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M11.94 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0h-.06zm5.53 7.3-2.03 9.57c-.15.69-.56.86-1.13.53l-3.13-2.3-1.51 1.46c-.17.17-.31.31-.63.31l.22-3.18 5.8-5.24c.25-.22-.06-.35-.39-.13l-7.17 4.51-3.09-.96c-.67-.21-.68-.67.14-.99l12.07-4.65c.56-.21 1.05.13.85.98z\"/></svg>",
            new OnboardingStep[]
            {
                new("input", "Bot Token", "Paste the token BotFather gave you.", InputKey: "botToken", InputKind: "password", InputHelp: "Looks like 123456:ABC-DEF..."),
            }),
    };

    public static ChannelTypeDefinition? GetByType(string channelType)
        => All.FirstOrDefault(t => string.Equals(t.Type, channelType, StringComparison.OrdinalIgnoreCase));
}

public sealed record ChannelTypeDefinition(
    string Type,
    string DisplayName,
    string Description,
    string Logo,
    IReadOnlyList<OnboardingStep> OnboardingSteps);

public sealed record OnboardingStep(
    string Type,
    string Title,
    string Description,
    string? Value = null,
    string? InputKey = null,
    string? InputLabel = null,
    string? InputPlaceholder = null,
    string? InputHelp = null,
    string InputKind = "text",
    bool InputRequired = true);

namespace EnterpriseAgentOs.Domain.Features.Channels;

// ---------- Channel Connection DTOs ----------

public sealed record ChannelConnectionDto(
    Guid Id,
    string ChannelType,
    string DisplayName,
    bool Enabled,
    bool Configured,
    DateTime CreatedAt,
    Guid? CreatedById);

public sealed record CreateChannelConnectionRequest(
    string ChannelType,
    string DisplayName,
    Dictionary<string, string>? Config);

public sealed record UpdateChannelConnectionRequest(
    string? DisplayName,
    bool? Enabled,
    Dictionary<string, string>? Config);

// ---------- Channel Type DTOs ----------

public sealed record ChannelTypeDto(
    string Type,
    string DisplayName,
    string Description,
    IReadOnlyList<OnboardingStep> OnboardingSteps);

// ---------- Agent Channel Binding DTOs ----------

public sealed record AgentChannelBindingDto(
    Guid Id,
    Guid AgentId,
    Guid ChannelConnectionId,
    string ChannelType,
    string ChannelDisplayName,
    bool Enabled,
    AgentChannelConfig? Config,
    DateTime CreatedAt);

public sealed record CreateAgentChannelBindingRequest(
    Guid ChannelConnectionId,
    AgentChannelConfig? Config);

public sealed record UpdateAgentChannelBindingRequest(
    bool? Enabled,
    AgentChannelConfig? Config);

/// <summary>
/// Agent-specific channel configuration. All fields are optional and
/// default to sensible values in the message router.
/// </summary>
public sealed record AgentChannelConfig
{
    public string DmPolicy { get; init; } = "open";
    public string GroupPolicy { get; init; } = "mention";
    public string[]? AllowedUsers { get; init; }
    public string[]? AllowedGroups { get; init; }
    public bool RequireMention { get; init; } = true;
    public string[]? MentionPatterns { get; init; }
    public int HistoryLimit { get; init; } = 10;
    public string StreamingMode { get; init; } = "off";
}

/// <summary>
/// Static definitions of supported channel types and their config field schemas.
/// </summary>
public static class ChannelTypes
{
    public static readonly IReadOnlyList<ChannelTypeDefinition> All = new[]
    {
        new ChannelTypeDefinition("slack", "Slack", "Connect a Slack workspace",
            "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M5.04 15.16a2.53 2.53 0 0 1-2.52 2.53A2.53 2.53 0 0 1 0 15.16a2.53 2.53 0 0 1 2.52-2.52h2.52v2.52zm1.27 0a2.53 2.53 0 0 1 2.52-2.52 2.53 2.53 0 0 1 2.52 2.52v6.32A2.53 2.53 0 0 1 8.83 24a2.53 2.53 0 0 1-2.52-2.52v-6.32zM8.83 5.04a2.53 2.53 0 0 1-2.52-2.52A2.53 2.53 0 0 1 8.83 0a2.53 2.53 0 0 1 2.52 2.52v2.52H8.83zm0 1.27a2.53 2.53 0 0 1 2.52 2.52 2.53 2.53 0 0 1-2.52 2.52H2.52A2.53 2.53 0 0 1 0 8.83a2.53 2.53 0 0 1 2.52-2.52h6.31zm10.13 2.52a2.53 2.53 0 0 1 2.52-2.52A2.53 2.53 0 0 1 24 8.83a2.53 2.53 0 0 1-2.52 2.52h-2.52V8.83zm-1.27 0a2.53 2.53 0 0 1-2.52 2.52 2.53 2.53 0 0 1-2.52-2.52V2.52A2.53 2.53 0 0 1 15.17 0a2.53 2.53 0 0 1 2.52 2.52v6.31zm-2.52 10.13a2.53 2.53 0 0 1 2.52 2.52A2.53 2.53 0 0 1 15.17 24a2.53 2.53 0 0 1-2.52-2.52v-2.52h2.52zm0-1.27a2.53 2.53 0 0 1-2.52-2.52 2.53 2.53 0 0 1 2.52-2.52h6.31A2.53 2.53 0 0 1 24 15.17a2.53 2.53 0 0 1-2.52 2.52h-6.31z\"/></svg>",
            new OnboardingStep[]
            {
                new("url", "Install Slack App", "Click below to add the AgentOS bot to your Slack workspace.", Value: "https://api.slack.com/apps"),
                new("input", "Bot Token", "Paste the Bot User OAuth Token from your Slack app settings.", InputKey: "botToken", InputKind: "password", InputPlaceholder: "xoxb-...", InputHelp: "Found under OAuth & Permissions"),
                new("input", "Signing Secret", "Used to verify incoming webhook requests from Slack.", InputKey: "signingSecret", InputKind: "password", InputHelp: "Found under Basic Information → App Credentials"),
            }),
        new ChannelTypeDefinition("telegram", "Telegram", "Connect a Telegram bot",
            "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M11.94 0A12 12 0 0 0 0 12a12 12 0 0 0 12 12 12 12 0 0 0 12-12A12 12 0 0 0 12 0h-.06zm5.53 7.3-2.03 9.57c-.15.69-.56.86-1.13.53l-3.13-2.3-1.51 1.46c-.17.17-.31.31-.63.31l.22-3.18 5.8-5.24c.25-.22-.06-.35-.39-.13l-7.17 4.51-3.09-.96c-.67-.21-.68-.67.14-.99l12.07-4.65c.56-.21 1.05.13.85.98z\"/></svg>",
            new OnboardingStep[]
            {
                new("qr", "Open BotFather", "Scan the QR code or tap the link to create a bot with @BotFather.", Value: "https://t.me/BotFather"),
                new("input", "Bot Token", "Paste the token BotFather gave you.", InputKey: "botToken", InputKind: "password", InputHelp: "Looks like 123456:ABC-DEF..."),
                new("input", "Webhook Secret", "Optional secret for webhook verification.", InputKey: "webhookSecret", InputKind: "password", InputRequired: false),
            }),
        new ChannelTypeDefinition("discord", "Discord", "Connect a Discord bot",
            "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M20.32 4.37a19.8 19.8 0 0 0-4.93-1.52.07.07 0 0 0-.08.04c-.21.38-.45.87-.61 1.26a18.27 18.27 0 0 0-5.49 0 12.64 12.64 0 0 0-.62-1.26.08.08 0 0 0-.08-.04 19.74 19.74 0 0 0-4.93 1.52.07.07 0 0 0-.03.03C.53 9.05-.32 13.58.1 18.06a.08.08 0 0 0 .03.05 19.9 19.9 0 0 0 5.99 3.03.08.08 0 0 0 .08-.03c.46-.63.87-1.3 1.22-2a.08.08 0 0 0-.04-.11 13.1 13.1 0 0 1-1.87-.9.08.08 0 0 1 0-.12c.13-.09.25-.19.37-.29a.08.08 0 0 1 .08-.01c3.93 1.79 8.18 1.79 12.07 0a.08.08 0 0 1 .08.01c.12.1.25.2.37.29a.08.08 0 0 1 0 .12c-.6.35-1.22.65-1.87.9a.08.08 0 0 0-.04.1c.36.7.77 1.37 1.22 2a.08.08 0 0 0 .08.03 19.83 19.83 0 0 0 6-3.03.08.08 0 0 0 .04-.05c.5-5.18-.84-9.68-3.55-13.66a.06.06 0 0 0-.04-.03zM8.02 15.33c-1.18 0-2.16-1.09-2.16-2.42s.96-2.42 2.16-2.42c1.21 0 2.18 1.1 2.16 2.42 0 1.33-.96 2.42-2.16 2.42zm7.97 0c-1.18 0-2.16-1.09-2.16-2.42s.96-2.42 2.16-2.42c1.21 0 2.18 1.1 2.16 2.42 0 1.33-.95 2.42-2.16 2.42z\"/></svg>",
            new OnboardingStep[]
            {
                new("url", "Create Discord Application", "Go to the Discord Developer Portal and create an application.", Value: "https://discord.com/developers/applications"),
                new("input", "Bot Token", "The bot token from your Discord application.", InputKey: "botToken", InputKind: "password"),
                new("input", "Application ID", "Your Discord application ID.", InputKey: "applicationId", InputHelp: "Found on the General Information page"),
                new("input", "Public Key", "Used to verify interaction payloads from Discord.", InputKey: "publicKey", InputHelp: "Found on the General Information page"),
            }),
        new ChannelTypeDefinition("whatsapp", "WhatsApp", "Connect via QR code — like WhatsApp Web",
            "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M17.47 14.38c-.3-.15-1.76-.87-2.03-.97-.27-.1-.47-.15-.67.15-.2.3-.77.97-.95 1.17-.17.2-.35.22-.65.07-.3-.15-1.27-.47-2.42-1.49-.9-.8-1.5-1.78-1.67-2.08-.18-.3-.02-.46.13-.61.13-.13.3-.35.45-.52.15-.17.2-.3.3-.5.1-.2.05-.37-.02-.52-.08-.15-.67-1.62-.92-2.22-.24-.58-.49-.5-.67-.51h-.58c-.2 0-.52.07-.8.37-.27.3-1.04 1.02-1.04 2.49s1.07 2.89 1.22 3.09c.15.2 2.1 3.2 5.08 4.49.71.31 1.27.49 1.7.63.71.23 1.36.2 1.87.12.57-.09 1.76-.72 2.01-1.42.25-.7.25-1.29.17-1.42-.07-.13-.27-.2-.57-.35zm-5.42 7.4A9.87 9.87 0 0 1 7 20.07l-.36-.21-3.73.98.99-3.63-.24-.37a9.87 9.87 0 0 1-1.51-5.26c0-5.45 4.44-9.89 9.9-9.89a9.89 9.89 0 0 1 9.89 9.9c0 5.45-4.44 9.88-9.9 9.88zm8.41-18.29A11.82 11.82 0 0 0 12.05 0C5.47 0 .1 5.37.1 11.95c0 2.1.55 4.16 1.6 5.97L0 24l6.24-1.64a11.94 11.94 0 0 0 5.81 1.49c6.58 0 11.94-5.37 11.94-11.95a11.87 11.87 0 0 0-3.53-8.41z\"/></svg>",
            // No onboarding steps — WhatsApp uses QR code pairing, handled by the dashboard's dedicated WhatsApp flow
            Array.Empty<OnboardingStep>()),
        new ChannelTypeDefinition("teams", "Microsoft Teams", "Connect to Microsoft Teams",
            "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M20.63 8.26h-3.67c.59-.55.96-1.34.96-2.21a2.9 2.9 0 0 0-2.9-2.9c-.58 0-1.12.17-1.57.47A3.62 3.62 0 0 0 10.2 2a3.62 3.62 0 0 0-3.63 3.63c0 .96.38 1.83.99 2.48H4.97a.94.94 0 0 0-.94.94v5.53a5.57 5.57 0 0 0 5.57 5.57h1.13a5.57 5.57 0 0 0 5.56-5.57v-.52h4.34a.94.94 0 0 0 .94-.94V9.2a.94.94 0 0 0-.94-.94zM15.02 4.4a1.66 1.66 0 1 1 0 3.31 1.66 1.66 0 0 1 0-3.31zM10.2 3.24a2.39 2.39 0 1 1 0 4.78 2.39 2.39 0 0 1 0-4.78zm5.28 11.34a4.33 4.33 0 0 1-4.33 4.33H9.72a4.33 4.33 0 0 1-4.33-4.33V9.5h5.4v2.1a.94.94 0 0 0 .94.93h4.75v2.05zm4.9-1.76h-3.91V9.5h3.9v3.32z\"/></svg>",
            new OnboardingStep[]
            {
                new("url", "Register Azure AD App", "Create an app registration in the Azure portal.", Value: "https://portal.azure.com/#view/Microsoft_AAD_RegisteredApps"),
                new("input", "App ID", "Azure AD application (client) ID.", InputKey: "appId", InputHelp: "Found on the app registration Overview page"),
                new("input", "App Secret", "Azure AD client secret.", InputKey: "appSecret", InputKind: "password", InputHelp: "Create under Certificates & secrets"),
            }),
        new ChannelTypeDefinition("google-chat", "Google Chat", "Connect to Google Chat",
            "<svg viewBox=\"0 0 24 24\" xmlns=\"http://www.w3.org/2000/svg\"><path d=\"M22 4.5v11a2 2 0 0 1-2 2h-4v3l-4-3H6a2 2 0 0 1-2-2V8.5a2 2 0 0 1 2-2h4v-2a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2zM6 8.5v7h6.59L16 18v-2.5h2v-7H6z\"/></svg>",
            new OnboardingStep[]
            {
                new("url", "Create Service Account", "Go to the Google Cloud Console to create a service account.", Value: "https://console.cloud.google.com/iam-admin/serviceaccounts"),
                new("input", "Service Account JSON", "Paste the full service account key JSON.", InputKey: "serviceAccountJson", InputKind: "textarea", InputHelp: "Download from the service account Keys tab"),
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

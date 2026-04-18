namespace EnterpriseAgentOs.Domain.DTOs.Channels;

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
    IReadOnlyList<ChannelConfigField> ConfigFields);

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
        new ChannelTypeDefinition("slack", "Slack", "Connect a Slack workspace", new[]
        {
            new ChannelConfigField("botToken", "Bot Token", "password", true, "xoxb-...", "Bot User OAuth Token from Slack App"),
            new ChannelConfigField("signingSecret", "Signing Secret", "password", true, null, "Used to verify incoming webhook requests"),
        }),
        new ChannelTypeDefinition("telegram", "Telegram", "Connect a Telegram bot", new[]
        {
            new ChannelConfigField("botToken", "Bot Token", "password", true, null, "Token from @BotFather"),
            new ChannelConfigField("webhookSecret", "Webhook Secret", "password", false, null, "Optional secret for webhook verification"),
        }),
        new ChannelTypeDefinition("discord", "Discord", "Connect a Discord bot", new[]
        {
            new ChannelConfigField("botToken", "Bot Token", "password", true, null, "Discord bot token"),
            new ChannelConfigField("applicationId", "Application ID", "text", true, null, "Discord application ID"),
            new ChannelConfigField("publicKey", "Public Key", "text", true, null, "Used to verify interaction payloads"),
        }),
        new ChannelTypeDefinition("whatsapp", "WhatsApp", "Connect via WhatsApp Business API", new[]
        {
            new ChannelConfigField("phoneNumberId", "Phone Number ID", "text", true, null, "WhatsApp Business phone number ID"),
            new ChannelConfigField("accessToken", "Access Token", "password", true, null, "Meta Graph API access token"),
            new ChannelConfigField("verifyToken", "Verify Token", "text", true, null, "Token for webhook verification"),
        }),
        new ChannelTypeDefinition("teams", "Microsoft Teams", "Connect to Microsoft Teams", new[]
        {
            new ChannelConfigField("appId", "App ID", "text", true, null, "Azure AD application (client) ID"),
            new ChannelConfigField("appSecret", "App Secret", "password", true, null, "Azure AD client secret"),
        }),
        new ChannelTypeDefinition("google-chat", "Google Chat", "Connect to Google Chat", new[]
        {
            new ChannelConfigField("serviceAccountJson", "Service Account JSON", "textarea", true, null, "Google Cloud service account key JSON"),
        }),
        new ChannelTypeDefinition("webchat", "Webchat", "Built-in web chat widget", Array.Empty<ChannelConfigField>()),
    };

    public static ChannelTypeDefinition? GetByType(string channelType)
        => All.FirstOrDefault(t => string.Equals(t.Type, channelType, StringComparison.OrdinalIgnoreCase));
}

public sealed record ChannelTypeDefinition(
    string Type,
    string DisplayName,
    string Description,
    IReadOnlyList<ChannelConfigField> ConfigFields);

public sealed record ChannelConfigField(
    string Key,
    string Label,
    string Kind,
    bool Required,
    string? Placeholder,
    string? Help);

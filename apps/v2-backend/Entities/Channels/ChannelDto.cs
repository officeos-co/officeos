using System.Text.Json;

namespace EnterpriseAgentOs.Api.Entities.Channels;

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

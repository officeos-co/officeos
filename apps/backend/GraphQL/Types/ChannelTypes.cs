namespace EnterpriseAgentOs.Api.GraphQL.Types;

// ── Output types ──────────────────────────────────────────────────────────

public sealed record ChannelConnectionGqlDto(
    Guid Id,
    string ChannelType,
    string DisplayName,
    bool Enabled,
    DateTime CreatedAt);

public sealed record AgentChannelBindingGqlDto(
    Guid Id,
    Guid AgentId,
    Guid ChannelConnectionId,
    bool Enabled,
    EnterpriseAgentOs.Domain.Models.ChannelBindingConfig? Config,
    DateTime CreatedAt);

public sealed record ChannelTypeGqlDto(
    string Type,
    string DisplayName,
    string Description);

// ── Input types ───────────────────────────────────────────────────────────

public sealed record CreateChannelConnectionInput(
    string ChannelType,
    string DisplayName,
    string ConfigJson);

public sealed record UpdateChannelConnectionInput(
    string? DisplayName,
    string? ConfigJson,
    bool? Enabled);

public sealed record ChannelBindingConfigInput(
    EnterpriseAgentOs.Domain.Models.ChannelPermission Receive,
    EnterpriseAgentOs.Domain.Models.ChannelPermission Send,
    EnterpriseAgentOs.Domain.Models.ChannelPermission Initiate,
    string? DmPolicy,
    string? GroupPolicy,
    List<string>? AllowedUsers,
    List<string>? AllowedGroups,
    bool? RequireMention,
    List<string>? MentionPatterns,
    int? HistoryLimit,
    string? StreamingMode);

// ── Mapping helpers (shared between queries + mutations) ──────────────────

internal static class ChannelGraphQLMapper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ChannelConnectionGqlDto ToDto(EnterpriseAgentOs.Domain.Models.ChannelConnectionRecord r) => new(
        r.Id,
        r.ChannelType,
        r.DisplayName,
        r.Enabled,
        r.CreatedAt);

    public static AgentChannelBindingGqlDto ToDto(EnterpriseAgentOs.Domain.Models.AgentChannelBindingRecord r) => new(
        r.Id,
        r.AgentId,
        r.ChannelConnectionId,
        r.Enabled,
        DeserializeConfig(r.Config),
        r.CreatedAt);

    public static EnterpriseAgentOs.Domain.Models.ChannelBindingConfig? DeserializeConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<EnterpriseAgentOs.Domain.Models.ChannelBindingConfig>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public static string SerializeConfig(ChannelBindingConfigInput input)
    {
        var cfg = new EnterpriseAgentOs.Domain.Models.ChannelBindingConfig
        {
            Receive = input.Receive,
            Send = input.Send,
            Initiate = input.Initiate,
            DmPolicy = input.DmPolicy,
            GroupPolicy = input.GroupPolicy,
            AllowedUsers = input.AllowedUsers,
            AllowedGroups = input.AllowedGroups,
            RequireMention = input.RequireMention,
            MentionPatterns = input.MentionPatterns,
            HistoryLimit = input.HistoryLimit,
            StreamingMode = input.StreamingMode,
        };
        return JsonSerializer.Serialize(cfg);
    }
}

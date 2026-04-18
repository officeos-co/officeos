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
    ChannelBindingConfig? Config,
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
    ChannelPermission Receive,
    ChannelPermission Send,
    ChannelPermission Initiate,
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

    public static ChannelConnectionGqlDto ToDto(ChannelConnectionRecord r) => new(
        r.Id,
        r.ChannelType,
        r.DisplayName,
        r.Enabled,
        r.CreatedAt);

    public static AgentChannelBindingGqlDto ToDto(AgentChannelBindingRecord r) => new(
        r.Id,
        r.AgentId,
        r.ChannelConnectionId,
        r.Enabled,
        DeserializeConfig(r.Config),
        r.CreatedAt);

    public static ChannelBindingConfig? DeserializeConfig(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<ChannelBindingConfig>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public static string SerializeConfig(ChannelBindingConfigInput input)
    {
        var cfg = new ChannelBindingConfig
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

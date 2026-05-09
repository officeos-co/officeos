namespace OffceOs.Api.Features.Channels;

// ── Output types (projections that exclude sensitive/nav fields) ─────────

public sealed record ChannelConnectionPayload(
    Guid Id,
    string ChannelType,
    string DisplayName,
    bool Enabled,
    DateTime CreatedAt);

public sealed record AgentChannelBindingPayload(
    Guid Id,
    Guid AgentId,
    Guid ChannelConnectionId,
    bool Enabled,
    ChannelBindingConfig? Config,
    DateTime CreatedAt);

// ── Input types ───────────────────────────────────────────────────────────

public sealed record CreateChannelConnectionInput(
    string ChannelType,
    string DisplayName,
    string ConfigJson,
    string? DefaultChannelId = null);

public sealed record UpdateChannelConnectionInput(
    string? DisplayName,
    string? ConfigJson,
    bool? Enabled);

public sealed record ChannelBindingConfigInput(
    string? PlatformId,
    string? ThreadId);

// ── Mapping helpers (shared bcetween queries + mutations) ──────────────────

internal static class ChannelGraphQLMapper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ChannelConnectionPayload ToPayload(ChannelConnectionRecord r) => new(
        r.Id,
        r.ChannelType.ToStorageString(),
        r.DisplayName,
        r.Enabled,
        r.CreatedAt);

    public static AgentChannelBindingPayload ToPayload(AgentChannelBindingRecord r) => new(
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
            PlatformId = input.PlatformId,
            ThreadId = input.ThreadId,
        };
        return JsonSerializer.Serialize(cfg);
    }
}

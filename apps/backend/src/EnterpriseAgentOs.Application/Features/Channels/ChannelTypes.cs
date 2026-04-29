namespace EnterpriseAgentOs.Application.Features.Channels;

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

public sealed record CreateChannelConnectionInput(
    string ChannelType,
    string DisplayName,
    string ConfigJson);

public sealed record UpdateChannelConnectionInput(
    string? DisplayName,
    string? ConfigJson,
    bool? Enabled);

public sealed record ChannelBindingConfigInput(
    string? PlatformId,
    string? ThreadId);

internal static class ChannelGraphQLMapper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static ChannelConnectionGqlDto ToDto(ChannelConnectionRecord r) => new(
        r.Id,
        r.ChannelType.ToStorageString(),
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
            PlatformId = input.PlatformId,
            ThreadId = input.ThreadId,
        };
        return JsonSerializer.Serialize(cfg);
    }
}

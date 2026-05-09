namespace OffceOs.Configuration;

public sealed record SessionAuthConfig
{
    public string[] SkipPrefixes { get; init; } = [];
}

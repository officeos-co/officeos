namespace EnterpriseAgentOs.Infrastructure.Common.Configuration;

public sealed class DaytonaConfig
{
    public string ApiUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string? Target { get; init; }
    public string? Snapshot { get; init; }
    public string Workdir { get; init; } = "/workspace";
    public int TimeoutSeconds { get; init; } = 60;

    public Uri ApiBaseUri => new(AppendSlash(ApiUrl));

    private static string AppendSlash(string value) => value.EndsWith('/') ? value : value + "/";
}

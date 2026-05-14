namespace OffceOs.Configuration;

public sealed class CodexAppServerConfig
{
    public string Command { get; init; } = "codex";
    public string HomeRoot { get; init; } = "";
    public int LoginTimeoutSeconds { get; init; } = 300;
    public int TurnTimeoutSeconds { get; init; } = 600;

    public string EffectiveHomeRoot =>
        string.IsNullOrWhiteSpace(HomeRoot)
            ? System.IO.Path.Combine(System.IO.Path.GetTempPath(), "eaos-codex")
            : HomeRoot.Trim();
}

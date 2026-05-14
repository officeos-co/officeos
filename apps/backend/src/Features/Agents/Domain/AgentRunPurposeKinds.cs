namespace OffceOs.Domain.Features.Agents;

public static class AgentRunPurposeKinds
{
    public const string Manual = "manual";
    public const string Bootstrap = "bootstrap";
    public const string Channel = "channel";
    public const string Routine = "routine";

    public static string Normalize(string? purpose) =>
        string.IsNullOrWhiteSpace(purpose) ? Manual : purpose.Trim().ToLowerInvariant();
}

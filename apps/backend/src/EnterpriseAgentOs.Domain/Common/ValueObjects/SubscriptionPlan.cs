namespace EnterpriseAgentOs.Domain.Common.ValueObjects;

public enum SubscriptionPlan
{
    Free,
    Pro,
    Team,
    Enterprise,
}

public static class SubscriptionPlanExtensions
{
    public static string ToStorageString(this SubscriptionPlan plan) => plan switch
    {
        SubscriptionPlan.Free => "free",
        SubscriptionPlan.Pro => "pro",
        SubscriptionPlan.Team => "team",
        SubscriptionPlan.Enterprise => "enterprise",
        _ => throw new ArgumentOutOfRangeException(nameof(plan)),
    };

    public static SubscriptionPlan ToSubscriptionPlan(this string value) => value switch
    {
        "free" => SubscriptionPlan.Free,
        "pro" => SubscriptionPlan.Pro,
        "team" => SubscriptionPlan.Team,
        "enterprise" => SubscriptionPlan.Enterprise,
        _ => throw new ArgumentOutOfRangeException(nameof(value), $"Unknown plan: {value}"),
    };
}

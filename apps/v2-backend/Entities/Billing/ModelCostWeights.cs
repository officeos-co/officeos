namespace EnterpriseAgentOs.Api.Entities.Billing;

/// <summary>
/// Converts raw model tokens to normalized credits.
/// 1 credit = 1 raw token on the cheapest model (gpt-4o-mini / gemini-2.5-flash).
/// Weights reflect approximate relative cost so the credit budget is fair
/// regardless of which model an agent uses.
/// </summary>
public static class ModelCostWeights
{
    private static readonly Dictionary<string, int> Weights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["gpt-4o-mini"]       = 1,
        ["gemini-2.5-flash"]  = 1,
        ["claude-haiku-4-5"]  = 5,
        ["gemini-2.5-pro"]    = 8,
        ["gpt-4o"]            = 15,
        ["claude-sonnet-4-6"] = 20,
        ["claude-opus-4-6"]   = 75,
    };

    /// <summary>
    /// Returns the credit cost for <paramref name="rawTokens"/> consumed by <paramref name="model"/>.
    /// Unknown models default to the Sonnet weight (20×) to avoid undercharging.
    /// </summary>
    public static long ToCredits(string model, long rawTokens)
    {
        var weight = Weights.GetValueOrDefault(model, 20);
        return rawTokens * weight;
    }
}

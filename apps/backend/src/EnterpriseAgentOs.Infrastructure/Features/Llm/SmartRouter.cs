namespace EnterpriseAgentOs.Infrastructure.Features.Llm;

/// <summary>
/// Classifies request complexity and returns a concrete model name.
/// When <paramref name="requestedModel"/> is not <c>"auto"</c> it is
/// returned unchanged. Otherwise the model is chosen from the preferred
/// provider family based on estimated token load.
/// </summary>
public static class SmartRouter
{
    // Tier thresholds (total chars across all message content)
    private const int SimpleMaxChars = 800;    // → haiku / gpt-4o-mini / gemini-flash
    private const int StandardMaxChars = 4000; // → sonnet / gpt-4o / gemini-pro

    public static string Resolve(
        string requestedModel,
        JsonElement requestBody,
        string preferredFamily = "anthropic")
    {
        if (!requestedModel.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return requestedModel;

        var tier = ClassifyComplexity(requestBody);

        return preferredFamily.ToLowerInvariant() switch
        {
            "openai" => tier switch
            {
                ComplexityTier.Simple => "gpt-4o-mini",
                _ => "gpt-4o",
            },
            "google" => tier switch
            {
                ComplexityTier.Simple => "gemini-2.5-flash",
                _ => "gemini-2.5-pro",
            },
            _ => tier switch // anthropic default
            {
                ComplexityTier.Simple => "claude-haiku-4-5",
                ComplexityTier.Standard => "claude-sonnet-4-6",
                ComplexityTier.Complex => "claude-sonnet-4-6", // Opus only if explicitly named
                _ => "claude-sonnet-4-6",
            },
        };
    }

    private static ComplexityTier ClassifyComplexity(JsonElement body)
    {
        // Count total chars across all messages
        int totalChars = 0;
        if (body.TryGetProperty("messages", out var messages))
        {
            foreach (var msg in messages.EnumerateArray())
            {
                if (msg.TryGetProperty("content", out var content))
                {
                    totalChars += content.ValueKind == JsonValueKind.String
                        ? content.GetString()?.Length ?? 0
                        : content.GetRawText().Length;
                }
            }
        }

        // Tool count — many tools signals complex reasoning
        int toolCount = body.TryGetProperty("tools", out var tools)
            ? tools.GetArrayLength()
            : 0;

        if (totalChars <= SimpleMaxChars && toolCount <= 3) return ComplexityTier.Simple;
        if (totalChars <= StandardMaxChars) return ComplexityTier.Standard;
        return ComplexityTier.Complex;
    }

    private enum ComplexityTier { Simple, Standard, Complex }
}

namespace OffceOs.Infrastructure.Features.Agents;

/// <summary>
/// Classifies request complexity and returns a concrete model name.
/// When <paramref name="requestedModel"/> is not <c>"auto"</c> it is
/// returned unchanged. Otherwise the model is chosen from the preferred
/// provider family based on estimated token load.
/// </summary>
public static class SmartRouter
{
    private const int SimpleMaxChars = 800;
    private const int StandardMaxChars = 4000;

    public static string Resolve(
        string requestedModel,
        JsonElement requestBody,
        string preferredFamily = "anthropic")
    {
        if (!requestedModel.Equals("auto", StringComparison.OrdinalIgnoreCase))
            return requestedModel;

        var tier = ClassifyComplexity(requestBody);

        var smartTier = tier switch
        {
            ComplexityTier.Simple => SmartRoutingTier.Simple,
            ComplexityTier.Standard => SmartRoutingTier.Standard,
            _ => SmartRoutingTier.Complex,
        };

        return ProviderRegistry.GetSmartRouteModel(preferredFamily, smartTier)
            ?? ProviderRegistry.GetSmartRouteModel(preferredFamily, SmartRoutingTier.Standard)
            ?? "claude-sonnet-4-6"; // ultimate fallback
    }

    private static ComplexityTier ClassifyComplexity(JsonElement body)
    {
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

        int toolCount = body.TryGetProperty("tools", out var tools)
            ? tools.GetArrayLength()
            : 0;

        if (totalChars <= SimpleMaxChars && toolCount <= 3) return ComplexityTier.Simple;
        if (totalChars <= StandardMaxChars) return ComplexityTier.Standard;
        return ComplexityTier.Complex;
    }

    private enum ComplexityTier { Simple, Standard, Complex }
}

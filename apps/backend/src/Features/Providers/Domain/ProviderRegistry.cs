namespace OffceOs.Domain.Features.Providers;

/// <summary>
/// Single source of truth for all supported LLM providers, their models, and metadata.
/// </summary>
public static class ProviderRegistry
{
    public const string CustomProviderSlug = "custom";

    public static readonly IReadOnlyList<ProviderDefinition> All = new[]
    {
        new ProviderDefinition(
            Slug: "anthropic",
            DisplayName: "Anthropic",
            ApiFormat: ApiFormat.Anthropic,
            BaseUrl: "https://api.anthropic.com/v1",
            PlatformKeyConfigName: AnthropicKey,
            Models: new[]
            {
                new ModelDefinition("claude-haiku-4-5", "Claude Haiku 4.5", CostWeight: 5, SmartRoutingTier.Simple),
                new ModelDefinition("claude-sonnet-4-6", "Claude Sonnet 4.6", CostWeight: 20, SmartRoutingTier.Standard),
                new ModelDefinition("claude-opus-4-6", "Claude Opus 4.6", CostWeight: 75, null),
            }),

        new ProviderDefinition(
            Slug: "openai",
            DisplayName: "OpenAI",
            ApiFormat: ApiFormat.OpenAiCompat,
            BaseUrl: "https://api.openai.com/v1",
            PlatformKeyConfigName: OpenAiKey,
            Models: new[]
            {
                new ModelDefinition("gpt-4o-mini", "GPT-4o Mini", CostWeight: 1, SmartRoutingTier.Simple),
                new ModelDefinition("gpt-4o", "GPT-4o", CostWeight: 15, SmartRoutingTier.Standard),
            }),

        new ProviderDefinition(
            Slug: "google",
            DisplayName: "Google Gemini",
            ApiFormat: ApiFormat.OpenAiCompat,
            BaseUrl: "https://generativelanguage.googleapis.com/v1beta/openai",
            PlatformKeyConfigName: GeminiKey,
            Models: new[]
            {
                new ModelDefinition("gemini-2.5-flash", "Gemini 2.5 Flash", CostWeight: 1, SmartRoutingTier.Simple),
                new ModelDefinition("gemini-2.5-pro", "Gemini 2.5 Pro", CostWeight: 8, SmartRoutingTier.Standard),
            }),

        new ProviderDefinition(
            Slug: "xai",
            DisplayName: "xAI Grok",
            ApiFormat: ApiFormat.OpenAiCompat,
            BaseUrl: "https://api.x.ai/v1",
            PlatformKeyConfigName: XaiKey,
            Models: new[]
            {
                new ModelDefinition("grok-4", "Grok 4", CostWeight: 20, SmartRoutingTier.Standard),
            }),

        new ProviderDefinition(
            Slug: AwsBedrockProviderSlug,
            DisplayName: "Amazon Bedrock",
            ApiFormat: ApiFormat.Anthropic,
            BaseUrl: "https://bedrock-runtime.aws.amazon.com",
            PlatformKeyConfigName: null,
            Models: new[]
            {
                new ModelDefinition("us.anthropic.claude-haiku-4-5-20251001-v1:0", "Claude Haiku 4.5 20251001", CostWeight: 5, SmartRoutingTier.Simple),
                new ModelDefinition("us.anthropic.claude-sonnet-4-6", "Claude Sonnet 4.6", CostWeight: 20, SmartRoutingTier.Standard),
                new ModelDefinition("us.anthropic.claude-opus-4-7", "Claude Opus 4.7", CostWeight: 75, null),
            },
            EnterpriseOnly: true,
            RequiresPinnedModels: true),

        new ProviderDefinition(
            Slug: GoogleVertexProviderSlug,
            DisplayName: "Google Vertex AI",
            ApiFormat: ApiFormat.Anthropic,
            BaseUrl: "https://aiplatform.googleapis.com",
            PlatformKeyConfigName: null,
            Models: new[]
            {
                new ModelDefinition("claude-haiku-4-5@20251001", "Claude Haiku 4.5 20251001", CostWeight: 5, SmartRoutingTier.Simple),
                new ModelDefinition("claude-sonnet-4-6", "Claude Sonnet 4.6", CostWeight: 20, SmartRoutingTier.Standard),
                new ModelDefinition("claude-opus-4-7", "Claude Opus 4.7", CostWeight: 75, null),
            },
            EnterpriseOnly: true,
            RequiresPinnedModels: true),

        new ProviderDefinition(
            Slug: AzureFoundryProviderSlug,
            DisplayName: "Microsoft Foundry",
            ApiFormat: ApiFormat.OpenAiCompat,
            BaseUrl: "https://models.inference.ai.azure.com",
            PlatformKeyConfigName: null,
            Models: new[]
            {
                new ModelDefinition("claude-haiku-4-5", "Claude Haiku 4.5", CostWeight: 5, SmartRoutingTier.Simple),
                new ModelDefinition("claude-sonnet-4-6", "Claude Sonnet 4.6", CostWeight: 20, SmartRoutingTier.Standard),
                new ModelDefinition("claude-opus-4-7", "Claude Opus 4.7", CostWeight: 75, null),
            },
            EnterpriseOnly: true,
            RequiresPinnedModels: true),

        // Dispatch-only providers — no models exposed in dashboard, no seeding, no platform keys
        new ProviderDefinition("groq", "Groq", ApiFormat.OpenAiCompat, "https://api.groq.com/openai/v1", null, Array.Empty<ModelDefinition>()),
        new ProviderDefinition("deepseek", "DeepSeek", ApiFormat.OpenAiCompat, "https://api.deepseek.com/v1", null, Array.Empty<ModelDefinition>()),
        new ProviderDefinition("openrouter", "OpenRouter", ApiFormat.OpenAiCompat, "https://openrouter.ai/api/v1", null, Array.Empty<ModelDefinition>()),
    };

    // ── Lookup indexes (built once) ─────────────────────────────────────

    private static readonly Dictionary<string, ProviderDefinition> BySlug =
        All.ToDictionary(p => p.Slug, StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, ModelDefinition> ModelById =
        All.SelectMany(p => p.Models)
           .GroupBy(m => m.Id, StringComparer.OrdinalIgnoreCase)
           .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, ProviderDefinition> ProviderByModel =
        All.SelectMany(p => p.Models.Select(m => (p, m)))
           .GroupBy(x => x.m.Id, StringComparer.OrdinalIgnoreCase)
           .ToDictionary(group => group.Key, group => group.First().p, StringComparer.OrdinalIgnoreCase);

    // provider+tier → model (for smart routing)
    private static readonly Dictionary<(string Slug, SmartRoutingTier Tier), string> SmartRouteMap =
        All.SelectMany(p => p.Models
                .Where(m => m.SmartTier.HasValue)
                .Select(m => (Key: (p.Slug, m.SmartTier!.Value), m.Id)))
           .ToDictionary(x => x.Key, x => x.Id, new SlugTierComparer());

    // ── Public API ──────────────────────────────────────────────────────

    public static ProviderDefinition? Get(string slug) =>
        BySlug.GetValueOrDefault(slug);

    public static ProviderDefinition? GetByModel(string modelId) =>
        ProviderByModel.GetValueOrDefault(modelId);

    public static readonly IReadOnlyList<string> SupportedModels =
        All.Where(p => p.Models.Count > 0)
           .SelectMany(p => p.Models.Select(m => m.Id))
           .Prepend("auto")
           .ToList();

    public static bool IsValidModel(string modelId) =>
        modelId.Equals("auto", StringComparison.OrdinalIgnoreCase) || ModelById.ContainsKey(modelId);

    public static bool IsCustomProvider(string providerSlug) =>
        providerSlug.Equals(CustomProviderSlug, StringComparison.OrdinalIgnoreCase);

    public static int GetCostWeight(string modelId, int defaultWeight = 20) =>
        ModelById.TryGetValue(modelId, out var m) ? m.CostWeight : defaultWeight;

    public static long ToCredits(string model, long rawTokens) =>
        rawTokens * GetCostWeight(model);

    public static IReadOnlyDictionary<string, int> GetCostWeights() =>
        DashboardProviders
            .SelectMany(p => p.Models)
            .ToDictionary(m => m.Id, m => m.CostWeight, StringComparer.OrdinalIgnoreCase);

    public static string GetDisplayName(string modelId) =>
        modelId.Equals("auto", StringComparison.OrdinalIgnoreCase)
            ? "Auto (smart routing)"
            : ModelById.TryGetValue(modelId, out var m) ? m.DisplayName : modelId;

    public static IReadOnlyList<string> GetModelIds(string providerSlug) =>
        BySlug.TryGetValue(providerSlug, out var p) ? p.Models.Select(m => m.Id).ToList() : [];

    public static bool IsEnterpriseProvider(string providerSlug) =>
        BySlug.TryGetValue(providerSlug, out var p) && p.EnterpriseOnly;

    public static string? GetSmartRouteModel(string providerSlug, SmartRoutingTier tier) =>
        SmartRouteMap.GetValueOrDefault((providerSlug, tier));

    /// <summary>Providers that expose selectable models in the dashboard.</summary>
    public static IReadOnlyList<ProviderDefinition> DashboardProviders =>
        All.Where(p => p.Models.Count > 0 && !p.EnterpriseOnly).ToList();

    public const string DefaultModel = "auto";
    public const string AwsBedrockProviderSlug = "aws-bedrock";
    public const string GoogleVertexProviderSlug = "google-vertex";
    public const string AzureFoundryProviderSlug = "azure-foundry";

    // ── Stable well-known config property names (referenced by PlatformKeyConfigName) ──
    // These are just string constants so Domain doesn't depend on PlatformKeysConfig.
    private const string AnthropicKey = "AnthropicApiKey";
    private const string OpenAiKey = "OpenAiApiKey";
    private const string GeminiKey = "GeminiApiKey";
    private const string XaiKey = "XaiApiKey";

    private sealed class SlugTierComparer : IEqualityComparer<(string Slug, SmartRoutingTier Tier)>
    {
        public bool Equals((string Slug, SmartRoutingTier Tier) x, (string Slug, SmartRoutingTier Tier) y) =>
            string.Equals(x.Slug, y.Slug, StringComparison.OrdinalIgnoreCase) && x.Tier == y.Tier;

        public int GetHashCode((string Slug, SmartRoutingTier Tier) obj) =>
            HashCode.Combine(obj.Slug.ToLowerInvariant(), obj.Tier);
    }
}

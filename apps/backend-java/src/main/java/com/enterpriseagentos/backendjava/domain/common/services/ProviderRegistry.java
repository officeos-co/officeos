package com.enterpriseagentos.backendjava.domain.common.services;

import java.util.List;
import java.util.Locale;
import java.util.Map;
import java.util.Optional;
import java.util.stream.Collectors;

public final class ProviderRegistry {
    public static final String DEFAULT_MODEL = "auto";

    public static final List<ProviderDefinition> ALL = List.of(
        new ProviderDefinition("anthropic", "Anthropic", ApiFormat.Anthropic, "https://api.anthropic.com/v1", "AnthropicApiKey", List.of(
            new ModelDefinition("claude-haiku-4-5", "Claude Haiku 4.5", 5, SmartRoutingTier.Simple),
            new ModelDefinition("claude-sonnet-4-6", "Claude Sonnet 4.6", 20, SmartRoutingTier.Standard),
            new ModelDefinition("claude-opus-4-6", "Claude Opus 4.6", 75, null))),
        new ProviderDefinition("openai", "OpenAI", ApiFormat.OpenAiCompat, "https://api.openai.com/v1", "OpenAiApiKey", List.of(
            new ModelDefinition("gpt-4o-mini", "GPT-4o Mini", 1, SmartRoutingTier.Simple),
            new ModelDefinition("gpt-4o", "GPT-4o", 15, SmartRoutingTier.Standard))),
        new ProviderDefinition("google", "Google Gemini", ApiFormat.OpenAiCompat, "https://generativelanguage.googleapis.com/v1beta/openai", "GeminiApiKey", List.of(
            new ModelDefinition("gemini-2.5-flash", "Gemini 2.5 Flash", 1, SmartRoutingTier.Simple),
            new ModelDefinition("gemini-2.5-pro", "Gemini 2.5 Pro", 8, SmartRoutingTier.Standard))),
        new ProviderDefinition("xai", "xAI Grok", ApiFormat.OpenAiCompat, "https://api.x.ai/v1", "XaiApiKey", List.of(
            new ModelDefinition("grok-4", "Grok 4", 20, SmartRoutingTier.Standard))),
        new ProviderDefinition("groq", "Groq", ApiFormat.OpenAiCompat, "https://api.groq.com/openai/v1", null, List.of()),
        new ProviderDefinition("deepseek", "DeepSeek", ApiFormat.OpenAiCompat, "https://api.deepseek.com/v1", null, List.of()),
        new ProviderDefinition("openrouter", "OpenRouter", ApiFormat.OpenAiCompat, "https://openrouter.ai/api/v1", null, List.of())
    );

    public static final List<String> SUPPORTED_MODELS = java.util.stream.Stream.concat(
        java.util.stream.Stream.of(DEFAULT_MODEL),
        ALL.stream().flatMap(provider -> provider.models().stream()).map(ModelDefinition::id)
    ).toList();

    private static final Map<String, ProviderDefinition> BY_SLUG =
        ALL.stream().collect(Collectors.toUnmodifiableMap(provider -> key(provider.slug()), provider -> provider));

    private static final Map<String, ModelDefinition> MODEL_BY_ID =
        ALL.stream().flatMap(provider -> provider.models().stream())
            .collect(Collectors.toUnmodifiableMap(model -> key(model.id()), model -> model));

    private ProviderRegistry() {
    }

    public static Optional<ProviderDefinition> get(String slug) {
        return Optional.ofNullable(BY_SLUG.get(key(slug)));
    }

    public static Optional<ProviderDefinition> getByModel(String modelId) {
        String normalized = key(modelId);
        return ALL.stream()
            .filter(provider -> provider.models().stream().anyMatch(model -> key(model.id()).equals(normalized)))
            .findFirst();
    }

    public static boolean isValidModel(String modelId) {
        return DEFAULT_MODEL.equalsIgnoreCase(modelId) || MODEL_BY_ID.containsKey(key(modelId));
    }

    public static int getCostWeight(String modelId, int defaultWeight) {
        ModelDefinition model = MODEL_BY_ID.get(key(modelId));
        return model == null ? defaultWeight : model.costWeight();
    }

    public static long toCredits(String model, long rawTokens) {
        return rawTokens * getCostWeight(model, 20);
    }

    public static List<ProviderDefinition> dashboardProviders() {
        return ALL.stream().filter(provider -> !provider.models().isEmpty()).toList();
    }

    public static String getDisplayName(String modelId) {
        if (DEFAULT_MODEL.equalsIgnoreCase(modelId)) {
            return "Auto (smart routing)";
        }
        ModelDefinition model = MODEL_BY_ID.get(key(modelId));
        return model == null ? modelId : model.displayName();
    }

    private static String key(String value) {
        return value == null ? "" : value.toLowerCase(Locale.ROOT);
    }
}

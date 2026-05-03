export function getModelTooltip(modelId: string): string {
  if (modelId === "auto") {
    return "Smart routing is transparent: today it only uses configured Anthropic models, routing simpler turns to Haiku and standard turns to Sonnet.";
  }

  if (modelId.startsWith("claude-haiku")) {
    return "Anthropic Claude Haiku. Lower-cost option for short, simple turns.";
  }

  if (modelId.startsWith("claude-sonnet")) {
    return "Anthropic Claude Sonnet. Default workhorse model for normal agent turns.";
  }

  if (modelId.startsWith("claude-opus")) {
    return "Anthropic Claude Opus. Highest-capability Claude option; use when quality matters more than cost.";
  }

  if (modelId.includes("mini")) {
    return "OpenAI mini model. Lower-cost concrete model; no hidden routing is applied.";
  }

  if (modelId.startsWith("gpt-")) {
    return "OpenAI concrete model. Requests go directly to this model; no auto routing is applied.";
  }

  if (modelId.startsWith("gemini-")) {
    return "Google Gemini concrete model. Requests go directly to this model; no auto routing is applied.";
  }

  if (modelId.startsWith("grok-")) {
    return "xAI Grok concrete model. Requests go directly to this model; no auto routing is applied.";
  }

  return "Concrete model selected by this deployment. Requests go directly to this model unless the model is explicitly Auto.";
}

export function getProviderTooltip(providerName: string, configured: boolean): string {
  const provider = providerName.toLowerCase();
  const keyName =
    provider === "anthropic"
      ? "AnthropicApiKey"
      : provider === "openai"
        ? "OpenAiApiKey"
        : provider === "google"
          ? "GeminiApiKey"
          : provider === "xai"
            ? "XaiApiKey"
            : "the provider API key";

  if (configured) {
    return provider === "anthropic"
      ? "Configured. Anthropic is the only provider that currently exposes the Auto smart-routing option."
      : "Configured. The dashboard exposes this provider's concrete models directly; Auto is not routed through this provider.";
  }

  return `Not configured. Set ${keyName} for this deployment to expose this provider's concrete models in model pickers.`;
}

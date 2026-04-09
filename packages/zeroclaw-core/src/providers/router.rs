use super::Provider;
use super::traits::{
    ChatMessage, ChatRequest, ChatResponse, StreamChunk, StreamEvent, StreamOptions, StreamResult,
};
use crate::config::schema::ModelPricing;
use async_trait::async_trait;
use futures_util::stream::BoxStream;
use std::collections::HashMap;

/// A single route: maps a task hint to a provider + model combo.
#[derive(Debug, Clone)]
pub struct Route {
    pub provider_name: String,
    pub model: String,
}

/// Multi-model router — routes requests to different provider+model combos
/// based on a task hint encoded in the model parameter.
///
/// The model parameter can be:
/// - A regular model name (e.g. "anthropic/claude-sonnet-4") → uses default provider
/// - A hint-prefixed string (e.g. "hint:reasoning") → resolves via route table
///
/// This wraps multiple pre-created providers and selects the right one per request.
pub struct RouterProvider {
    routes: HashMap<String, (usize, String)>, // hint → (provider_index, model)
    providers: Vec<(String, Box<dyn Provider>)>,
    default_index: usize,
    default_model: String,
}

impl RouterProvider {
    /// Create a new router with a default provider and optional routes.
    ///
    /// `providers` is a list of (name, provider) pairs. The first one is the default.
    /// `routes` maps hint names to Route structs containing provider_name and model.
    pub fn new(
        providers: Vec<(String, Box<dyn Provider>)>,
        routes: Vec<(String, Route)>,
        default_model: String,
    ) -> Self {
        // Build provider name → index lookup
        let name_to_index: HashMap<&str, usize> = providers
            .iter()
            .enumerate()
            .map(|(i, (name, _))| (name.as_str(), i))
            .collect();

        // Resolve routes to provider indices
        let resolved_routes: HashMap<String, (usize, String)> = routes
            .into_iter()
            .filter_map(|(hint, route)| {
                let index = name_to_index.get(route.provider_name.as_str()).copied();
                match index {
                    Some(i) => Some((hint, (i, route.model))),
                    None => {
                        tracing::warn!(
                            hint = hint,
                            provider = route.provider_name,
                            "Route references unknown provider, skipping"
                        );
                        None
                    }
                }
            })
            .collect();

        Self {
            routes: resolved_routes,
            providers,
            default_index: 0,
            default_model,
        }
    }

    /// Resolve a model parameter to the cheapest qualifying route based on pricing.
    ///
    /// If the model starts with `"hint:cost-optimized"` or `"hint:cheapest"`, this
    /// method scores each route by `input_price + output_price` (a simple proxy for
    /// total cost), optionally filtering by capability requirements, and returns the
    /// cheapest qualifying route.
    ///
    /// Falls back to the default route when no pricing data matches.
    pub fn resolve_cost_optimized(
        &self,
        model: &str,
        prices: &HashMap<String, ModelPricing>,
        required_vision: bool,
        required_tools: bool,
    ) -> (usize, String) {
        let hint = model.strip_prefix("hint:");
        let is_cost_hint = matches!(hint, Some("cost-optimized" | "cheapest"));

        if !is_cost_hint {
            return self.resolve(model);
        }

        let mut candidates: Vec<(usize, String, f64)> = Vec::new();

        for (idx, route_model) in self.routes.values() {
            // Capability filtering
            if let Some((_, provider)) = self.providers.get(*idx) {
                if required_vision && !provider.supports_vision() {
                    continue;
                }
                if required_tools && !provider.supports_native_tools() {
                    continue;
                }
            }

            if let Some(pricing) = prices.get(route_model) {
                let total_cost = pricing.input + pricing.output;
                candidates.push((*idx, route_model.clone(), total_cost));
            }
        }

        // Sort by total cost (ascending) and pick the cheapest
        candidates.sort_by(|a, b| a.2.partial_cmp(&b.2).unwrap_or(std::cmp::Ordering::Equal));

        if let Some((idx, route_model, _)) = candidates.into_iter().next() {
            return (idx, route_model);
        }

        // Fallback to default
        tracing::warn!(
            "No cost-optimized route found with matching pricing data, \
             falling back to default"
        );
        (self.default_index, self.default_model.clone())
    }

    /// Resolve a model parameter to a (provider, actual_model) pair.
    ///
    /// If the model starts with "hint:", look up the hint in the route table.
    /// Otherwise, use the default provider with the given model name.
    /// Resolve a model parameter to a (provider_index, actual_model) pair.
    fn resolve(&self, model: &str) -> (usize, String) {
        if let Some(hint) = model.strip_prefix("hint:") {
            if let Some((idx, resolved_model)) = self.routes.get(hint) {
                return (*idx, resolved_model.clone());
            }
            tracing::warn!(
                hint = hint,
                "Unknown route hint, falling back to default provider"
            );
        }

        // Not a hint or hint not found — use default provider with the model as-is
        (self.default_index, model.to_string())
    }
}

/// A cost-optimized routing strategy that selects the cheapest qualifying
/// provider from the route table based on `ModelPricing` data.
///
/// This wraps pricing config and capability requirements, scoring candidates
/// by their combined input + output cost per 1M tokens.
#[derive(Debug, Clone)]
pub struct CostOptimizedStrategy {
    /// Per-model pricing data (keyed by model name).
    pub prices: HashMap<String, ModelPricing>,
    /// Whether the request requires vision support.
    pub required_vision: bool,
    /// Whether the request requires native tool support.
    pub required_tools: bool,
}

impl CostOptimizedStrategy {
    /// Create a new cost-optimized strategy with the given pricing data.
    pub fn new(prices: HashMap<String, ModelPricing>) -> Self {
        Self {
            prices,
            required_vision: false,
            required_tools: false,
        }
    }

    /// Set whether vision support is required.
    pub fn with_vision(mut self, required: bool) -> Self {
        self.required_vision = required;
        self
    }

    /// Set whether native tool support is required.
    pub fn with_tools(mut self, required: bool) -> Self {
        self.required_tools = required;
        self
    }

    /// Score a model by total cost (input + output per 1M tokens).
    /// Returns `None` if no pricing data is available for the model.
    pub fn score(&self, model: &str) -> Option<f64> {
        self.prices.get(model).map(|p| p.input + p.output)
    }
}

#[async_trait]
impl Provider for RouterProvider {
    async fn chat_with_system(
        &self,
        system_prompt: Option<&str>,
        message: &str,
        model: &str,
        temperature: f64,
    ) -> anyhow::Result<String> {
        let (provider_idx, resolved_model) = self.resolve(model);

        let (provider_name, provider) = &self.providers[provider_idx];
        tracing::info!(
            provider = provider_name.as_str(),
            model = resolved_model.as_str(),
            "Router dispatching request"
        );

        provider
            .chat_with_system(system_prompt, message, &resolved_model, temperature)
            .await
    }

    async fn chat_with_history(
        &self,
        messages: &[ChatMessage],
        model: &str,
        temperature: f64,
    ) -> anyhow::Result<String> {
        let (provider_idx, resolved_model) = self.resolve(model);
        let (_, provider) = &self.providers[provider_idx];
        provider
            .chat_with_history(messages, &resolved_model, temperature)
            .await
    }

    async fn chat(
        &self,
        request: ChatRequest<'_>,
        model: &str,
        temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        let (provider_idx, resolved_model) = self.resolve(model);
        let (_, provider) = &self.providers[provider_idx];
        provider.chat(request, &resolved_model, temperature).await
    }

    async fn chat_with_tools(
        &self,
        messages: &[ChatMessage],
        tools: &[serde_json::Value],
        model: &str,
        temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        let (provider_idx, resolved_model) = self.resolve(model);
        let (_, provider) = &self.providers[provider_idx];
        provider
            .chat_with_tools(messages, tools, &resolved_model, temperature)
            .await
    }

    fn supports_native_tools(&self) -> bool {
        self.providers
            .get(self.default_index)
            .map(|(_, p)| p.supports_native_tools())
            .unwrap_or(false)
    }

    fn supports_streaming(&self) -> bool {
        self.providers
            .iter()
            .any(|(_, provider)| provider.supports_streaming())
    }

    fn supports_streaming_tool_events(&self) -> bool {
        self.providers
            .iter()
            .any(|(_, provider)| provider.supports_streaming_tool_events())
    }

    fn stream_chat_with_history(
        &self,
        messages: &[ChatMessage],
        model: &str,
        temperature: f64,
        options: StreamOptions,
    ) -> BoxStream<'static, StreamResult<StreamChunk>> {
        let (provider_idx, resolved_model) = self.resolve(model);
        let (_, provider) = &self.providers[provider_idx];
        provider.stream_chat_with_history(messages, &resolved_model, temperature, options)
    }

    fn stream_chat(
        &self,
        request: ChatRequest<'_>,
        model: &str,
        temperature: f64,
        options: StreamOptions,
    ) -> BoxStream<'static, StreamResult<StreamEvent>> {
        let (provider_idx, resolved_model) = self.resolve(model);
        let (_, provider) = &self.providers[provider_idx];
        provider.stream_chat(request, &resolved_model, temperature, options)
    }

    fn supports_vision(&self) -> bool {
        self.providers
            .iter()
            .any(|(_, provider)| provider.supports_vision())
    }

    async fn warmup(&self) -> anyhow::Result<()> {
        for (name, provider) in &self.providers {
            tracing::info!(provider = name, "Warming up routed provider");
            if let Err(e) = provider.warmup().await {
                tracing::warn!(provider = name, "Warmup failed (non-fatal): {e}");
            }
        }
        Ok(())
    }
}

#[cfg(test)]
#[path = "router.test.rs"]
mod tests;

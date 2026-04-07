use serde::{Deserialize, Serialize};

/// How Anthropic credentials should be sent.
#[derive(Debug, Clone, Copy, PartialEq, Eq, Serialize, Deserialize)]
#[serde(rename_all = "kebab-case")]
pub enum AnthropicAuthKind {
    /// Standard Anthropic API key via `x-api-key`.
    ApiKey,
    /// Subscription / setup token via `Authorization: Bearer ...`.
    Authorization,
}

impl AnthropicAuthKind {
    pub fn as_metadata_value(self) -> &'static str {
        match self {
            Self::ApiKey => "api-key",
            Self::Authorization => "authorization",
        }
    }

    pub fn from_metadata_value(value: &str) -> Option<Self> {
        match value.trim().to_ascii_lowercase().as_str() {
            "api-key" | "x-api-key" | "apikey" => Some(Self::ApiKey),
            "authorization" | "bearer" | "auth-token" | "oauth" => Some(Self::Authorization),
            _ => None,
        }
    }
}

/// Detect auth kind with explicit override support.
pub fn detect_auth_kind(token: &str, explicit: Option<&str>) -> AnthropicAuthKind {
    if let Some(kind) = explicit.and_then(AnthropicAuthKind::from_metadata_value) {
        return kind;
    }

    let trimmed = token.trim();

    // JWT-like shape strongly suggests bearer token mode.
    if trimmed.matches('.').count() >= 2 {
        return AnthropicAuthKind::Authorization;
    }

    // Anthropic platform keys commonly start with this prefix.
    if trimmed.starts_with("sk-ant-api") {
        return AnthropicAuthKind::ApiKey;
    }

    // Default to API key for backward compatibility unless explicitly configured.
    AnthropicAuthKind::ApiKey
}


#[cfg(test)]
#[path = "anthropic_token.test.rs"]
mod tests;

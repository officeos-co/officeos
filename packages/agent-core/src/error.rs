// Rust guideline compliant 2026-02-21

//! Top-level error type for the crate.
//!
//! Data only — no behavior. Every module returns `crate::Result<T>` which
//! aliases `Result<T, Error>`. The `Tool` trait still returns
//! `anyhow::Result<ToolResult>` for trait-compat; surface errors for tools
//! come back as `ToolResult { success: false, error: Some(...) }`.

use thiserror::Error;

/// All errors the crate can produce.
///
/// # Variants
///
/// Each variant maps to a distinct failure domain (environment,
/// bootstrap, personality, LLM, gateway, tools, or generic).
#[derive(Error, Debug)]
pub enum Error {
    /// A required environment variable is missing, empty, or malformed.
    #[error("environment: {0}")]
    Env(String),

    /// Bootstrap transport failure (connect error, TLS error, etc).
    #[error("bootstrap http error: {0}")]
    BootstrapHttp(#[from] reqwest::Error),

    /// The backend rejected the agent's bearer token.
    #[error("bootstrap unauthorized")]
    BootstrapUnauthorized,

    /// The backend has no record of this agent.
    #[error("bootstrap agent not found")]
    BootstrapNotFound,

    /// Payload parsed as JSON but failed validation.
    ///
    /// Examples: `gateway.port == 0`, empty `systemPrompt`, unknown
    /// permission mode.
    #[error("bootstrap invalid payload: {0}")]
    BootstrapPayload(String),

    /// A required personality file is missing or empty after seed.
    #[error("personality: required file {0} missing or empty")]
    Personality(String),

    /// LLM request or stream failure.
    #[error("llm: {0}")]
    Llm(String),

    /// Filesystem IO under `memory_dir` failed.
    #[error("memory io: {0}")]
    MemoryIo(#[from] std::io::Error),

    /// Gateway (WS server, upgrade, frame parse) failure.
    #[error("gateway: {0}")]
    Gateway(String),

    /// Tool dispatch surfaced an unrecoverable error.
    #[error("tool {tool}: {message}")]
    Tool {
        /// Name of the tool that failed.
        tool: String,
        /// Human-readable error description.
        message: String,
    },

    /// Escape hatch for `anyhow::Error` from tool impls.
    #[error(transparent)]
    Other(#[from] anyhow::Error),
}

/// Crate-wide result alias.
pub type Result<T> = std::result::Result<T, Error>;

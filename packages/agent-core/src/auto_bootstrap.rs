// Rust guideline compliant 2026-02-21

//! Auto-bootstrap: send BOOTSTRAP.md as the first user message.
//!
//! Reads the seeded BOOTSTRAP.md, feeds it to the agent's turn loop,
//! and forwards both the input and response to the backend as log
//! entries.

use std::sync::Arc;

use crate::agent::Agent;
use crate::config::RuntimeConfig;
use crate::error::{Error, Result};
use crate::gateway::protocol::WsOutbound;
use crate::log_client::LogClient;

/// Read BOOTSTRAP.md, send it as the first user message, and log both
/// directions to the backend.
///
/// # Errors
///
/// Returns `Error::Personality` if the file is missing or empty, and
/// propagates transport errors from the log client.
pub async fn run(
    cfg: &Arc<RuntimeConfig>,
    agent: &Arc<Agent>,
    log_client: &LogClient,
) -> Result<()> {
    let bootstrap_path = cfg.memory_dir.join("BOOTSTRAP.md");

    let content = tokio::fs::read_to_string(&bootstrap_path)
        .await
        .map_err(|e| Error::Personality(format!("BOOTSTRAP.md: {e}")))?;

    if content.trim().is_empty() {
        return Err(Error::Personality("BOOTSTRAP.md is empty".into()));
    }

    tracing::info!(name: "auto_bootstrap.start", "auto-bootstrap: sending BOOTSTRAP.md as first message");

    // Forward the bootstrap message as a MessageIn log.
    log_client.forward("MessageIn", &content, None).await?;

    // Send the bootstrap content to the agent and capture the response.
    let (ws_tx, mut ws_rx) = tokio::sync::mpsc::unbounded_channel::<WsOutbound>();

    let turn_id = uuid::Uuid::new_v4().to_string();
    agent
        .handle_user_message_with_ws(content, Some(turn_id), &ws_tx)
        .await?;

    // Drain events and collect assistant text.
    drop(ws_tx);
    let mut response_text = String::new();
    while let Some(event) = ws_rx.recv().await {
        if let WsOutbound::AssistantDelta { text, .. } = event {
            response_text.push_str(&text);
        }
    }

    // Forward the agent's response as a MessageOut log.
    if !response_text.is_empty() {
        log_client
            .forward("MessageOut", &response_text, None)
            .await?;
    }

    tracing::info!(name: "auto_bootstrap.complete", "auto-bootstrap: complete");
    Ok(())
}

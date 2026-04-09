//! Emoji reaction tool for cross-channel message reactions.
//!
//! Exposes `add_reaction` and `remove_reaction` from the [`Channel`] trait as an
//! agent-callable tool. The tool holds a late-binding channel map handle that is
//! populated once channels are initialized (after tool construction). This mirrors
//! the pattern used by [`DelegateTool`] for its parent-tools handle.

use super::traits::{Tool, ToolResult};
use crate::channels::traits::Channel;
use crate::security::SecurityPolicy;
use crate::security::policy::ToolOperation;
use async_trait::async_trait;
use parking_lot::RwLock;
use serde_json::json;
use std::collections::HashMap;
use std::sync::Arc;

/// Shared handle to the channel map. Starts empty; populated once channels boot.
pub type ChannelMapHandle = Arc<RwLock<HashMap<String, Arc<dyn Channel>>>>;

/// Agent-callable tool for adding or removing emoji reactions on messages.
pub struct ReactionTool {
    channels: ChannelMapHandle,
    security: Arc<SecurityPolicy>,
}

impl ReactionTool {
    /// Create a new reaction tool with an empty channel map.
    /// Call [`populate`] or write to the returned [`ChannelMapHandle`] once channels
    /// are available.
    pub fn new(security: Arc<SecurityPolicy>) -> Self {
        Self {
            channels: Arc::new(RwLock::new(HashMap::new())),
            security,
        }
    }

    /// Return the shared handle so callers can populate it after channel init.
    pub fn channel_map_handle(&self) -> ChannelMapHandle {
        Arc::clone(&self.channels)
    }

    /// Convenience: populate the channel map from a pre-built map.
    pub fn populate(&self, map: HashMap<String, Arc<dyn Channel>>) {
        *self.channels.write() = map;
    }
}

#[async_trait]
impl Tool for ReactionTool {
    fn name(&self) -> &str {
        "reaction"
    }

    fn description(&self) -> &str {
        "Add or remove an emoji reaction on a message in any active channel. \
         Provide the channel name (e.g. 'discord', 'slack'), the platform channel ID, \
         the platform message ID, and the emoji (Unicode character or platform shortcode)."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        json!({
            "type": "object",
            "properties": {
                "channel": {
                    "type": "string",
                    "description": "Name of the channel to react in (e.g. 'discord', 'slack', 'telegram')"
                },
                "channel_id": {
                    "type": "string",
                    "description": "Platform-specific channel/conversation identifier (e.g. Discord channel snowflake, Slack channel ID)"
                },
                "message_id": {
                    "type": "string",
                    "description": "Platform-scoped message identifier to react to"
                },
                "emoji": {
                    "type": "string",
                    "description": "Emoji to react with (Unicode character or platform shortcode)"
                },
                "action": {
                    "type": "string",
                    "enum": ["add", "remove"],
                    "description": "Whether to add or remove the reaction (default: 'add')"
                }
            },
            "required": ["channel", "channel_id", "message_id", "emoji"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        // Security gate
        if let Err(error) = self
            .security
            .enforce_tool_operation(ToolOperation::Act, "reaction")
        {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(error),
            });
        }

        let channel_name = args
            .get("channel")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'channel' parameter"))?;

        let channel_id = args
            .get("channel_id")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'channel_id' parameter"))?;

        let message_id = args
            .get("message_id")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'message_id' parameter"))?;

        let emoji = args
            .get("emoji")
            .and_then(|v| v.as_str())
            .ok_or_else(|| anyhow::anyhow!("Missing 'emoji' parameter"))?;

        let action = args.get("action").and_then(|v| v.as_str()).unwrap_or("add");

        if action != "add" && action != "remove" {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!(
                    "Invalid action '{action}': must be 'add' or 'remove'"
                )),
            });
        }

        // Read-lock the channel map to find the target channel.
        let channel = {
            let map = self.channels.read();
            if map.is_empty() {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some("No channels available yet (channels not initialized)".to_string()),
                });
            }
            match map.get(channel_name) {
                Some(ch) => Arc::clone(ch),
                None => {
                    let available: Vec<String> = map.keys().cloned().collect();
                    return Ok(ToolResult {
                        success: false,
                        output: String::new(),
                        error: Some(format!(
                            "Channel '{channel_name}' not found. Available channels: {}",
                            available.join(", ")
                        )),
                    });
                }
            }
        };

        let result = if action == "add" {
            channel.add_reaction(channel_id, message_id, emoji).await
        } else {
            channel.remove_reaction(channel_id, message_id, emoji).await
        };

        let past_tense = if action == "remove" {
            "removed"
        } else {
            "added"
        };

        match result {
            Ok(()) => Ok(ToolResult {
                success: true,
                output: format!(
                    "Reaction {past_tense}: {emoji} on message {message_id} in {channel_name}"
                ),
                error: None,
            }),
            Err(e) => Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("Failed to {action} reaction: {e}")),
            }),
        }
    }
}

#[cfg(test)]
#[path = "reaction.test.rs"]
mod tests;

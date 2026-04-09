use super::*;

/// A stub channel that records sent messages but never produces incoming messages.
struct SilentChannel {
    channel_name: String,
    sent: Arc<RwLock<Vec<String>>>,
}

impl SilentChannel {
    fn new(name: &str) -> Self {
        Self {
            channel_name: name.to_string(),
            sent: Arc::new(RwLock::new(Vec::new())),
        }
    }
}

#[async_trait]
impl Channel for SilentChannel {
    fn name(&self) -> &str {
        &self.channel_name
    }

    async fn send(&self, message: &SendMessage) -> anyhow::Result<()> {
        self.sent.write().push(message.content.clone());
        Ok(())
    }

    async fn listen(&self, _tx: tokio::sync::mpsc::Sender<ChannelMessage>) -> anyhow::Result<()> {
        // Never sends anything — simulates no user response
        tokio::time::sleep(std::time::Duration::from_secs(600)).await;
        Ok(())
    }
}

/// A stub channel that immediately responds with a canned message.
struct RespondingChannel {
    channel_name: String,
    response: String,
    sent: Arc<RwLock<Vec<String>>>,
}

impl RespondingChannel {
    fn new(name: &str, response: &str) -> Self {
        Self {
            channel_name: name.to_string(),
            response: response.to_string(),
            sent: Arc::new(RwLock::new(Vec::new())),
        }
    }
}

#[async_trait]
impl Channel for RespondingChannel {
    fn name(&self) -> &str {
        &self.channel_name
    }

    async fn send(&self, message: &SendMessage) -> anyhow::Result<()> {
        self.sent.write().push(message.content.clone());
        Ok(())
    }

    async fn listen(&self, tx: tokio::sync::mpsc::Sender<ChannelMessage>) -> anyhow::Result<()> {
        let msg = ChannelMessage {
            id: "resp_1".to_string(),
            sender: "human".to_string(),
            reply_target: "human".to_string(),
            content: self.response.clone(),
            channel: self.channel_name.clone(),
            timestamp: 1000,
            thread_ts: None,
            interruption_scope_id: None,
            attachments: vec![],
        };
        let _ = tx.send(msg).await;
        Ok(())
    }
}

fn make_tool_with_channels(channels: Vec<(&str, Arc<dyn Channel>)>) -> EscalateToHumanTool {
    let tool = EscalateToHumanTool::new(Arc::new(SecurityPolicy::default()), PathBuf::from("/tmp"));
    let map: HashMap<String, Arc<dyn Channel>> = channels
        .into_iter()
        .map(|(name, ch)| (name.to_string(), ch))
        .collect();
    *tool.channel_map.write() = map;
    tool
}

// ── 1. test_tool_metadata ──

#[test]
fn test_tool_metadata() {
    let tool = EscalateToHumanTool::new(Arc::new(SecurityPolicy::default()), PathBuf::from("/tmp"));
    assert_eq!(tool.name(), "escalate_to_human");
    assert!(!tool.description().is_empty());
    assert!(tool.description().to_lowercase().contains("escalat"));
}

// ── 2. test_parameters_schema ──

#[test]
fn test_parameters_schema() {
    let tool = EscalateToHumanTool::new(Arc::new(SecurityPolicy::default()), PathBuf::from("/tmp"));
    let schema = tool.parameters_schema();
    assert_eq!(schema["type"], "object");
    assert!(schema["properties"]["summary"].is_object());
    assert!(schema["properties"]["urgency"].is_object());
    assert!(schema["properties"]["context"].is_object());
    assert!(schema["properties"]["wait_for_response"].is_object());
    assert!(schema["properties"]["timeout_secs"].is_object());
    let required = schema["required"].as_array().unwrap();
    assert!(required.iter().any(|v| v == "summary"));
    // Optional fields should not be in required
    assert!(!required.iter().any(|v| v == "urgency"));
    assert!(!required.iter().any(|v| v == "context"));
    assert!(!required.iter().any(|v| v == "wait_for_response"));
    assert!(!required.iter().any(|v| v == "timeout_secs"));
}

// ── 3. test_default_urgency_is_medium ──

#[tokio::test]
async fn test_default_urgency_is_medium() {
    let channel = Arc::new(SilentChannel::new("test"));
    let sent = Arc::clone(&channel.sent);
    let tool = make_tool_with_channels(vec![("test", channel as Arc<dyn Channel>)]);

    let result = tool
        .execute(json!({ "summary": "Need help" }))
        .await
        .unwrap();

    assert!(result.success, "error: {:?}", result.error);
    // Check the output JSON contains medium urgency
    assert!(result.output.contains("\"medium\""));
    // Check the sent message contains MEDIUM prefix
    let messages = sent.read();
    assert!(!messages.is_empty());
    assert!(messages[0].contains("[MEDIUM]"));
}

// ── 4. test_message_format_low ──

#[test]
fn test_message_format_low() {
    let msg = EscalateToHumanTool::format_message("low", "Disk space low", None);
    assert!(msg.starts_with("\u{2139}\u{fe0f} [LOW]"));
    assert!(msg.contains("Summary: Disk space low"));
    assert!(msg.contains("Reply to this message to respond."));
}

// ── 5. test_message_format_critical ──

#[test]
fn test_message_format_critical() {
    let msg = EscalateToHumanTool::format_message(
        "critical",
        "Production down",
        Some("Database unreachable for 5 minutes"),
    );
    assert!(msg.starts_with("\u{1f6a8} [CRITICAL]"));
    assert!(msg.contains("Summary: Production down"));
    assert!(msg.contains("Context: Database unreachable for 5 minutes"));
}

// ── 6. test_invalid_urgency_rejected ──

#[tokio::test]
async fn test_invalid_urgency_rejected() {
    let tool = make_tool_with_channels(vec![(
        "test",
        Arc::new(SilentChannel::new("test")) as Arc<dyn Channel>,
    )]);

    let result = tool
        .execute(json!({ "summary": "Help", "urgency": "extreme" }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.as_deref().unwrap().contains("Invalid urgency"));
    assert!(result.error.as_deref().unwrap().contains("extreme"));
}

// ── 7. test_non_blocking_returns_status ──

#[tokio::test]
async fn test_non_blocking_returns_status() {
    let tool = make_tool_with_channels(vec![(
        "slack",
        Arc::new(SilentChannel::new("slack")) as Arc<dyn Channel>,
    )]);

    let result = tool
        .execute(json!({
            "summary": "Need approval",
            "urgency": "low"
        }))
        .await
        .unwrap();

    assert!(result.success, "error: {:?}", result.error);
    let parsed: serde_json::Value = serde_json::from_str(&result.output).unwrap();
    assert_eq!(parsed["status"], "escalated");
    assert_eq!(parsed["urgency"], "low");
    assert_eq!(parsed["channel"], "slack");
}

// ── 8. test_blocking_mode_returns_response ──

#[tokio::test]
async fn test_blocking_mode_returns_response() {
    let tool = make_tool_with_channels(vec![(
        "test",
        Arc::new(RespondingChannel::new("test", "Approved, go ahead")) as Arc<dyn Channel>,
    )]);

    let result = tool
        .execute(json!({
            "summary": "Need deployment approval",
            "wait_for_response": true,
            "timeout_secs": 5
        }))
        .await
        .unwrap();

    assert!(result.success, "error: {:?}", result.error);
    assert_eq!(result.output, "Approved, go ahead");
}

// ── 9. test_blocking_mode_timeout ──

#[tokio::test]
async fn test_blocking_mode_timeout() {
    let tool = make_tool_with_channels(vec![(
        "test",
        Arc::new(SilentChannel::new("test")) as Arc<dyn Channel>,
    )]);

    let result = tool
        .execute(json!({
            "summary": "Waiting for response",
            "wait_for_response": true,
            "timeout_secs": 1
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert_eq!(result.output, "TIMEOUT");
    assert!(result.error.as_deref().unwrap().contains("1 seconds"));
}

// ── 10. test_pushover_not_required ──

#[tokio::test]
async fn test_pushover_not_required() {
    // High urgency without Pushover credentials should still succeed (channel-only)
    let tool = make_tool_with_channels(vec![(
        "test",
        Arc::new(SilentChannel::new("test")) as Arc<dyn Channel>,
    )]);

    let result = tool
        .execute(json!({
            "summary": "Critical alert",
            "urgency": "high"
        }))
        .await
        .unwrap();

    assert!(result.success, "error: {:?}", result.error);
    let parsed: serde_json::Value = serde_json::from_str(&result.output).unwrap();
    assert_eq!(parsed["status"], "escalated");
    assert_eq!(parsed["urgency"], "high");
}

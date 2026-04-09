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
            sender: "user".to_string(),
            reply_target: "user".to_string(),
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

fn make_tool_with_channels(channels: Vec<(&str, Arc<dyn Channel>)>) -> AskUserTool {
    let tool = AskUserTool::new(Arc::new(SecurityPolicy::default()));
    let map: HashMap<String, Arc<dyn Channel>> = channels
        .into_iter()
        .map(|(name, ch)| (name.to_string(), ch))
        .collect();
    tool.populate(map);
    tool
}

// ── Metadata tests ──

#[test]
fn tool_name_and_description() {
    let tool = AskUserTool::new(Arc::new(SecurityPolicy::default()));
    assert_eq!(tool.name(), "ask_user");
    assert!(!tool.description().is_empty());
    assert!(tool.description().contains("question"));
}

#[test]
fn parameter_schema_validation() {
    let tool = AskUserTool::new(Arc::new(SecurityPolicy::default()));
    let schema = tool.parameters_schema();
    assert_eq!(schema["type"], "object");
    assert!(schema["properties"]["question"].is_object());
    assert!(schema["properties"]["choices"].is_object());
    assert!(schema["properties"]["timeout_secs"].is_object());
    assert!(schema["properties"]["channel"].is_object());
    let required = schema["required"].as_array().unwrap();
    assert!(required.iter().any(|v| v == "question"));
    // choices, timeout_secs, channel are optional
    assert!(!required.iter().any(|v| v == "choices"));
    assert!(!required.iter().any(|v| v == "timeout_secs"));
    assert!(!required.iter().any(|v| v == "channel"));
}

#[test]
fn spec_matches_metadata() {
    let tool = AskUserTool::new(Arc::new(SecurityPolicy::default()));
    let spec = tool.spec();
    assert_eq!(spec.name, "ask_user");
    assert_eq!(spec.description, tool.description());
    assert!(spec.parameters["required"].is_array());
}

// ── Format question tests ──

#[test]
fn format_question_without_choices() {
    let text = format_question("Are you sure?", None);
    assert!(text.contains("Are you sure?"));
    assert!(!text.contains("1."));
}

#[test]
fn format_question_with_choices() {
    let choices = vec!["Yes".to_string(), "No".to_string(), "Maybe".to_string()];
    let text = format_question("Continue?", Some(&choices));
    assert!(text.contains("Continue?"));
    assert!(text.contains("1. Yes"));
    assert!(text.contains("2. No"));
    assert!(text.contains("3. Maybe"));
    assert!(text.contains("Reply with a number"));
}

// ── Execute tests ──

#[tokio::test]
async fn execute_rejects_missing_question() {
    let tool = make_tool_with_channels(vec![(
        "test",
        Arc::new(SilentChannel::new("test")) as Arc<dyn Channel>,
    )]);
    let result = tool.execute(json!({})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn execute_rejects_empty_question() {
    let tool = make_tool_with_channels(vec![(
        "test",
        Arc::new(SilentChannel::new("test")) as Arc<dyn Channel>,
    )]);
    let result = tool.execute(json!({ "question": "  " })).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn empty_channels_returns_not_initialized() {
    let tool = AskUserTool::new(Arc::new(SecurityPolicy::default()));
    let result = tool.execute(json!({ "question": "Hello?" })).await.unwrap();
    assert!(!result.success);
    assert!(result.error.as_deref().unwrap().contains("not initialized"));
}

#[tokio::test]
async fn unknown_channel_returns_error() {
    let tool = make_tool_with_channels(vec![(
        "slack",
        Arc::new(SilentChannel::new("slack")) as Arc<dyn Channel>,
    )]);
    let result = tool
        .execute(json!({ "question": "Hello?", "channel": "nonexistent" }))
        .await;
    assert!(result.is_err());
}

#[tokio::test]
async fn timeout_returns_timeout_output() {
    let tool = make_tool_with_channels(vec![(
        "test",
        Arc::new(SilentChannel::new("test")) as Arc<dyn Channel>,
    )]);
    let result = tool
        .execute(json!({
            "question": "Confirm?",
            "timeout_secs": 1
        }))
        .await
        .unwrap();
    assert!(!result.success);
    assert_eq!(result.output, "TIMEOUT");
    assert!(result.error.as_deref().unwrap().contains("1 seconds"));
}

#[tokio::test]
async fn successful_response_flow() {
    let tool = make_tool_with_channels(vec![(
        "test",
        Arc::new(RespondingChannel::new("test", "Yes, proceed!")) as Arc<dyn Channel>,
    )]);
    let result = tool
        .execute(json!({
            "question": "Should we deploy?",
            "timeout_secs": 5
        }))
        .await
        .unwrap();
    assert!(result.success, "error: {:?}", result.error);
    assert_eq!(result.output, "Yes, proceed!");
    assert!(result.error.is_none());
}

#[tokio::test]
async fn successful_response_with_choices() {
    let tool = make_tool_with_channels(vec![(
        "telegram",
        Arc::new(RespondingChannel::new("telegram", "2")) as Arc<dyn Channel>,
    )]);
    let result = tool
        .execute(json!({
            "question": "Pick an option",
            "choices": ["Option A", "Option B"],
            "channel": "telegram",
            "timeout_secs": 5
        }))
        .await
        .unwrap();
    assert!(result.success, "error: {:?}", result.error);
    assert_eq!(result.output, "2");
}

#[tokio::test]
async fn channel_map_handle_allows_late_binding() {
    let tool = AskUserTool::new(Arc::new(SecurityPolicy::default()));
    let handle = tool.channel_map_handle();

    // Initially empty — tool reports not initialized
    let result = tool.execute(json!({ "question": "Hello?" })).await.unwrap();
    assert!(!result.success);

    // Populate via the handle
    {
        let mut map = handle.write();
        map.insert(
            "cli".to_string(),
            Arc::new(RespondingChannel::new("cli", "ok")) as Arc<dyn Channel>,
        );
    }

    // Now the tool can route to the channel
    let result = tool
        .execute(json!({ "question": "Hello?", "timeout_secs": 5 }))
        .await
        .unwrap();
    assert!(result.success);
    assert_eq!(result.output, "ok");
}

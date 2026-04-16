use super::*;
use crate::channels::traits::ChannelMessage;

struct StubChannel {
    name: String,
    sent: Arc<RwLock<Vec<String>>>,
}

impl StubChannel {
    fn new(name: &str) -> Self {
        Self {
            name: name.to_string(),
            sent: Arc::new(RwLock::new(Vec::new())),
        }
    }
}

#[async_trait]
impl Channel for StubChannel {
    fn name(&self) -> &str {
        &self.name
    }

    async fn send(&self, message: &SendMessage) -> anyhow::Result<()> {
        self.sent.write().push(message.content.clone());
        Ok(())
    }

    async fn listen(&self, _tx: tokio::sync::mpsc::Sender<ChannelMessage>) -> anyhow::Result<()> {
        Ok(())
    }
}

fn make_channel_map(channels: Vec<Arc<dyn Channel>>) -> ChannelMapHandle {
    let mut map = HashMap::new();
    for ch in channels {
        map.insert(ch.name().to_string(), ch);
    }
    Arc::new(RwLock::new(map))
}

fn default_tool() -> PollTool {
    let security = Arc::new(SecurityPolicy::default());
    let stub: Arc<dyn Channel> = Arc::new(StubChannel::new("slack"));
    let channels = make_channel_map(vec![stub]);
    PollTool::new(security, channels)
}

// ── Option validation tests ──

#[test]
fn validate_options_rejects_too_few() {
    let args = json!({ "options": ["only_one"] });
    let err = validate_options(&args).unwrap_err();
    assert!(err.contains("at least 2"), "got: {err}");
}

#[test]
fn validate_options_rejects_too_many() {
    let opts: Vec<String> = (0..11).map(|i| format!("opt{i}")).collect();
    let args = json!({ "options": opts });
    let err = validate_options(&args).unwrap_err();
    assert!(err.contains("at most 10"), "got: {err}");
}

#[test]
fn validate_options_rejects_empty_strings() {
    let args = json!({ "options": ["a", "  ", "b"] });
    let err = validate_options(&args).unwrap_err();
    assert!(err.contains("non-empty string"), "got: {err}");
}

#[test]
fn validate_options_rejects_missing_field() {
    let args = json!({});
    let err = validate_options(&args).unwrap_err();
    assert!(err.contains("Missing"), "got: {err}");
}

#[test]
fn validate_options_accepts_valid_range() {
    let args = json!({ "options": ["yes", "no"] });
    let opts = validate_options(&args).unwrap();
    assert_eq!(opts, vec!["yes", "no"]);

    let opts10: Vec<String> = (0..10).map(|i| format!("opt{i}")).collect();
    let args10 = json!({ "options": opts10 });
    let result = validate_options(&args10).unwrap();
    assert_eq!(result.len(), 10);
}

// ── Text-based poll formatting tests ──

#[test]
fn format_text_poll_contains_question_and_options() {
    let text = format_text_poll(
        "Favorite color?",
        &["Red".into(), "Blue".into(), "Green".into()],
        30,
        false,
    );
    assert!(text.contains("Favorite color?"));
    assert!(text.contains("Red"));
    assert!(text.contains("Blue"));
    assert!(text.contains("Green"));
    assert!(text.contains("30 min"));
    assert!(text.contains("single choice"));
}

#[test]
fn format_text_poll_multi_select_label() {
    let text = format_text_poll("Pick any", &["A".into(), "B".into()], 60, true);
    assert!(text.contains("multiple choices allowed"));
}

#[test]
fn format_text_poll_includes_emoji_per_option() {
    let options: Vec<String> = (1..=5).map(|i| format!("Option {i}")).collect();
    let text = format_text_poll("Q?", &options, 10, false);
    // Each option line should contain its number emoji
    for emoji in &VOTE_EMOJIS[..5] {
        assert!(text.contains(emoji), "missing emoji {emoji}");
    }
}

// ── Missing parameters tests ──

#[tokio::test]
async fn execute_rejects_missing_question() {
    let tool = default_tool();
    let result = tool.execute(json!({ "options": ["a", "b"] })).await;
    assert!(
        result.is_err() || {
            let r = result.unwrap();
            !r.success || r.error.is_some()
        }
    );
}

#[tokio::test]
async fn execute_rejects_missing_options() {
    let tool = default_tool();
    let result = tool.execute(json!({ "question": "What?" })).await.unwrap();
    assert!(!result.success);
    assert!(result.error.as_deref().unwrap().contains("Missing"));
}

#[tokio::test]
async fn execute_rejects_invalid_option_count() {
    let tool = default_tool();
    let result = tool
        .execute(json!({ "question": "Q?", "options": ["only_one"] }))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.as_deref().unwrap().contains("at least 2"));
}

#[tokio::test]
async fn execute_succeeds_with_valid_args() {
    let tool = default_tool();
    let result = tool
        .execute(json!({
            "question": "Lunch?",
            "options": ["Pizza", "Sushi"],
            "channel": "slack",
            "recipient": "general"
        }))
        .await
        .unwrap();
    assert!(result.success, "error: {:?}", result.error);
    assert!(result.output.contains("Lunch?"));
    assert!(result.output.contains("Pizza"));
}

#[tokio::test]
async fn execute_reports_unknown_channel() {
    let tool = default_tool();
    let result = tool
        .execute(json!({
            "question": "Q?",
            "options": ["a", "b"],
            "channel": "nonexistent"
        }))
        .await;
    // Should be an Err because channel not found
    assert!(result.is_err());
}

#[test]
fn supports_native_poll_recognizes_telegram_and_discord() {
    assert!(supports_native_poll("telegram"));
    assert!(supports_native_poll("Telegram"));
    assert!(supports_native_poll("my_telegram_bot"));
    assert!(supports_native_poll("discord"));
    assert!(supports_native_poll("Discord"));
    assert!(!supports_native_poll("slack"));
    assert!(!supports_native_poll("whatsapp"));
}

use super::*;
use std::time::Duration;

#[test]
fn log_observer_name() {
    assert_eq!(LogObserver::new().name(), "log");
}

#[test]
fn log_observer_all_events_no_panic() {
    let obs = LogObserver::new();
    obs.record_event(&ObserverEvent::AgentStart {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
    });
    obs.record_event(&ObserverEvent::AgentEnd {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
        duration: Duration::from_millis(500),
        tokens_used: Some(100),
        cost_usd: Some(0.0015),
    });
    obs.record_event(&ObserverEvent::AgentEnd {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
        duration: Duration::ZERO,
        tokens_used: None,
        cost_usd: None,
    });
    obs.record_event(&ObserverEvent::LlmResponse {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
        duration: Duration::from_millis(150),
        success: true,
        error_message: None,
        input_tokens: Some(100),
        output_tokens: Some(50),
    });
    obs.record_event(&ObserverEvent::LlmResponse {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
        duration: Duration::from_millis(200),
        success: false,
        error_message: Some("rate limited".into()),
        input_tokens: None,
        output_tokens: None,
    });
    obs.record_event(&ObserverEvent::ToolCall {
        tool: "shell".into(),
        duration: Duration::from_millis(10),
        success: false,
    });
    obs.record_event(&ObserverEvent::ChannelMessage {
        channel: "telegram".into(),
        direction: "outbound".into(),
    });
    obs.record_event(&ObserverEvent::HeartbeatTick);
    obs.record_event(&ObserverEvent::Error {
        component: "provider".into(),
        message: "timeout".into(),
    });
}

#[test]
fn log_observer_all_metrics_no_panic() {
    let obs = LogObserver::new();
    obs.record_metric(&ObserverMetric::RequestLatency(Duration::from_secs(2)));
    obs.record_metric(&ObserverMetric::TokensUsed(0));
    obs.record_metric(&ObserverMetric::TokensUsed(u64::MAX));
    obs.record_metric(&ObserverMetric::ActiveSessions(1));
    obs.record_metric(&ObserverMetric::QueueDepth(999));
}

#[test]
fn log_observer_hand_events_no_panic() {
    let obs = LogObserver::new();
    obs.record_event(&ObserverEvent::HandStarted {
        hand_name: "review".into(),
    });
    obs.record_event(&ObserverEvent::HandCompleted {
        hand_name: "review".into(),
        duration_ms: 1500,
        findings_count: 3,
    });
    obs.record_event(&ObserverEvent::HandFailed {
        hand_name: "review".into(),
        error: "timeout".into(),
        duration_ms: 5000,
    });
}

#[test]
fn log_observer_hand_metrics_no_panic() {
    let obs = LogObserver::new();
    obs.record_metric(&ObserverMetric::HandRunDuration {
        hand_name: "review".into(),
        duration: Duration::from_millis(1500),
    });
    obs.record_metric(&ObserverMetric::HandFindingsCount {
        hand_name: "review".into(),
        count: 5,
    });
    obs.record_metric(&ObserverMetric::HandSuccessRate {
        hand_name: "review".into(),
        success: true,
    });
}

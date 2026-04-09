use super::*;
use std::time::Duration;

#[test]
fn prometheus_observer_name() {
    assert_eq!(PrometheusObserver::new().name(), "prometheus");
}

#[test]
fn records_all_events_without_panic() {
    let obs = PrometheusObserver::new();
    obs.record_event(&ObserverEvent::AgentStart {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
    });
    obs.record_event(&ObserverEvent::AgentEnd {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
        duration: Duration::from_millis(500),
        tokens_used: Some(100),
        cost_usd: None,
    });
    obs.record_event(&ObserverEvent::AgentEnd {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
        duration: Duration::ZERO,
        tokens_used: None,
        cost_usd: None,
    });
    obs.record_event(&ObserverEvent::ToolCall {
        tool: "shell".into(),
        duration: Duration::from_millis(10),
        success: true,
    });
    obs.record_event(&ObserverEvent::ToolCall {
        tool: "file_read".into(),
        duration: Duration::from_millis(5),
        success: false,
    });
    obs.record_event(&ObserverEvent::ChannelMessage {
        channel: "telegram".into(),
        direction: "inbound".into(),
    });
    obs.record_event(&ObserverEvent::HeartbeatTick);
    obs.record_event(&ObserverEvent::Error {
        component: "provider".into(),
        message: "timeout".into(),
    });
}

#[test]
fn records_all_metrics_without_panic() {
    let obs = PrometheusObserver::new();
    obs.record_metric(&ObserverMetric::RequestLatency(Duration::from_secs(2)));
    obs.record_metric(&ObserverMetric::TokensUsed(500));
    obs.record_metric(&ObserverMetric::TokensUsed(0));
    obs.record_metric(&ObserverMetric::ActiveSessions(3));
    obs.record_metric(&ObserverMetric::QueueDepth(42));
}

#[test]
fn encode_produces_prometheus_text_format() {
    let obs = PrometheusObserver::new();
    obs.record_event(&ObserverEvent::AgentStart {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
    });
    obs.record_event(&ObserverEvent::ToolCall {
        tool: "shell".into(),
        duration: Duration::from_millis(100),
        success: true,
    });
    obs.record_event(&ObserverEvent::HeartbeatTick);
    obs.record_metric(&ObserverMetric::RequestLatency(Duration::from_millis(250)));

    let output = obs.encode();
    assert!(output.contains("zeroclaw_agent_starts_total"));
    assert!(output.contains("zeroclaw_tool_calls_total"));
    assert!(output.contains("zeroclaw_heartbeat_ticks_total"));
    assert!(output.contains("zeroclaw_request_latency_seconds"));
}

#[test]
fn counters_increment_correctly() {
    let obs = PrometheusObserver::new();

    for _ in 0..3 {
        obs.record_event(&ObserverEvent::HeartbeatTick);
    }

    let output = obs.encode();
    assert!(output.contains("zeroclaw_heartbeat_ticks_total 3"));
}

#[test]
fn tool_calls_track_success_and_failure_separately() {
    let obs = PrometheusObserver::new();

    obs.record_event(&ObserverEvent::ToolCall {
        tool: "shell".into(),
        duration: Duration::from_millis(10),
        success: true,
    });
    obs.record_event(&ObserverEvent::ToolCall {
        tool: "shell".into(),
        duration: Duration::from_millis(10),
        success: true,
    });
    obs.record_event(&ObserverEvent::ToolCall {
        tool: "shell".into(),
        duration: Duration::from_millis(10),
        success: false,
    });

    let output = obs.encode();
    assert!(output.contains(r#"zeroclaw_tool_calls_total{success="true",tool="shell"} 2"#));
    assert!(output.contains(r#"zeroclaw_tool_calls_total{success="false",tool="shell"} 1"#));
}

#[test]
fn errors_track_by_component() {
    let obs = PrometheusObserver::new();
    obs.record_event(&ObserverEvent::Error {
        component: "provider".into(),
        message: "timeout".into(),
    });
    obs.record_event(&ObserverEvent::Error {
        component: "provider".into(),
        message: "rate limit".into(),
    });
    obs.record_event(&ObserverEvent::Error {
        component: "channels".into(),
        message: "disconnected".into(),
    });

    let output = obs.encode();
    assert!(output.contains(r#"zeroclaw_errors_total{component="provider"} 2"#));
    assert!(output.contains(r#"zeroclaw_errors_total{component="channels"} 1"#));
}

#[test]
fn gauge_reflects_latest_value() {
    let obs = PrometheusObserver::new();
    obs.record_metric(&ObserverMetric::TokensUsed(100));
    obs.record_metric(&ObserverMetric::TokensUsed(200));

    let output = obs.encode();
    assert!(output.contains("zeroclaw_tokens_used_last 200"));
}

#[test]
fn llm_response_tracks_request_count_and_tokens() {
    let obs = PrometheusObserver::new();

    obs.record_event(&ObserverEvent::LlmResponse {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
        duration: Duration::from_millis(200),
        success: true,
        error_message: None,
        input_tokens: Some(100),
        output_tokens: Some(50),
    });
    obs.record_event(&ObserverEvent::LlmResponse {
        provider: "openrouter".into(),
        model: "claude-sonnet".into(),
        duration: Duration::from_millis(300),
        success: true,
        error_message: None,
        input_tokens: Some(200),
        output_tokens: Some(80),
    });

    let output = obs.encode();
    assert!(output.contains(
            r#"zeroclaw_llm_requests_total{model="claude-sonnet",provider="openrouter",success="true"} 2"#
        ));
    assert!(output.contains(
        r#"zeroclaw_tokens_input_total{model="claude-sonnet",provider="openrouter"} 300"#
    ));
    assert!(output.contains(
        r#"zeroclaw_tokens_output_total{model="claude-sonnet",provider="openrouter"} 130"#
    ));
}

#[test]
fn hand_events_track_runs_and_duration() {
    let obs = PrometheusObserver::new();

    obs.record_event(&ObserverEvent::HandCompleted {
        hand_name: "review".into(),
        duration_ms: 1500,
        findings_count: 3,
    });
    obs.record_event(&ObserverEvent::HandCompleted {
        hand_name: "review".into(),
        duration_ms: 2000,
        findings_count: 1,
    });
    obs.record_event(&ObserverEvent::HandFailed {
        hand_name: "review".into(),
        error: "timeout".into(),
        duration_ms: 5000,
    });

    let output = obs.encode();
    assert!(output.contains(r#"zeroclaw_hand_runs_total{hand="review",success="true"} 2"#));
    assert!(output.contains(r#"zeroclaw_hand_runs_total{hand="review",success="false"} 1"#));
    assert!(output.contains(r#"zeroclaw_hand_findings_total{hand="review"} 4"#));
    assert!(output.contains("zeroclaw_hand_duration_seconds"));
}

#[test]
fn hand_metrics_record_duration_and_findings() {
    let obs = PrometheusObserver::new();

    obs.record_metric(&ObserverMetric::HandRunDuration {
        hand_name: "scan".into(),
        duration: Duration::from_millis(800),
    });
    obs.record_metric(&ObserverMetric::HandFindingsCount {
        hand_name: "scan".into(),
        count: 5,
    });
    obs.record_metric(&ObserverMetric::HandSuccessRate {
        hand_name: "scan".into(),
        success: true,
    });
    obs.record_metric(&ObserverMetric::HandSuccessRate {
        hand_name: "scan".into(),
        success: false,
    });

    let output = obs.encode();
    assert!(output.contains("zeroclaw_hand_duration_seconds"));
    assert!(output.contains(r#"zeroclaw_hand_findings_total{hand="scan"} 5"#));
    assert!(output.contains(r#"zeroclaw_hand_runs_total{hand="scan",success="true"} 1"#));
    assert!(output.contains(r#"zeroclaw_hand_runs_total{hand="scan",success="false"} 1"#));
}

#[test]
fn llm_response_without_tokens_increments_request_only() {
    let obs = PrometheusObserver::new();

    obs.record_event(&ObserverEvent::LlmResponse {
        provider: "ollama".into(),
        model: "llama3".into(),
        duration: Duration::from_millis(100),
        success: false,
        error_message: Some("timeout".into()),
        input_tokens: None,
        output_tokens: None,
    });

    let output = obs.encode();
    assert!(output.contains(
        r#"zeroclaw_llm_requests_total{model="llama3",provider="ollama",success="false"} 1"#
    ));
    // Token counters should not appear (no data recorded)
    assert!(!output.contains("zeroclaw_tokens_input_total{"));
    assert!(!output.contains("zeroclaw_tokens_output_total{"));
}

#[test]
fn dora_deployment_events_track_counters() {
    let obs = PrometheusObserver::new();

    obs.record_event(&ObserverEvent::DeploymentCompleted {
        deploy_id: "d1".into(),
        commit_sha: "abc123".into(),
    });
    obs.record_event(&ObserverEvent::DeploymentCompleted {
        deploy_id: "d2".into(),
        commit_sha: "def456".into(),
    });
    obs.record_event(&ObserverEvent::DeploymentFailed {
        deploy_id: "d3".into(),
        reason: "timeout".into(),
    });

    let output = obs.encode();
    assert!(output.contains(r#"zeroclaw_deployments_total{status="success"} 2"#));
    assert!(output.contains(r#"zeroclaw_deployments_total{status="failure"} 1"#));
}

#[test]
fn dora_failure_rate_gauge_updates() {
    let obs = PrometheusObserver::new();

    obs.record_event(&ObserverEvent::DeploymentCompleted {
        deploy_id: "d1".into(),
        commit_sha: "abc".into(),
    });
    obs.record_event(&ObserverEvent::DeploymentFailed {
        deploy_id: "d2".into(),
        reason: "error".into(),
    });

    let output = obs.encode();
    // 1 failure out of 2 total = 0.5
    assert!(output.contains("zeroclaw_deployment_failure_rate 0.5"));
}

#[test]
fn dora_lead_time_and_recovery_metrics() {
    let obs = PrometheusObserver::new();

    obs.record_metric(&ObserverMetric::DeploymentLeadTime(Duration::from_secs(
        3600,
    )));
    obs.record_metric(&ObserverMetric::RecoveryTime(Duration::from_secs(600)));

    let output = obs.encode();
    assert!(output.contains("zeroclaw_deployment_lead_time_seconds"));
    assert!(output.contains("zeroclaw_recovery_time_seconds"));
    assert!(output.contains("zeroclaw_mttr_seconds 600"));
}

#[test]
fn dora_started_and_recovery_events_no_panic() {
    let obs = PrometheusObserver::new();

    obs.record_event(&ObserverEvent::DeploymentStarted {
        deploy_id: "d1".into(),
    });
    obs.record_event(&ObserverEvent::RecoveryCompleted {
        deploy_id: "d1".into(),
    });
}

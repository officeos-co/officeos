use super::*;

fn test_observability_config() -> ObservabilityConfig {
    ObservabilityConfig {
        backend: "none".to_string(),
        otel_endpoint: None,
        otel_service_name: None,
        runtime_trace_mode: "rolling".to_string(),
        runtime_trace_path: "state/runtime-trace.jsonl".to_string(),
        runtime_trace_max_entries: 3,
    }
}

#[test]
fn resolve_trace_path_relative_joins_workspace() {
    let cfg = test_observability_config();
    let workspace = tempfile::tempdir().unwrap();
    let path = resolve_trace_path(&cfg, workspace.path());
    assert_eq!(path, workspace.path().join("state/runtime-trace.jsonl"));
}

#[test]
fn storage_mode_parses_known_values() {
    let mut cfg = test_observability_config();
    cfg.runtime_trace_mode = "none".into();
    assert_eq!(
        storage_mode_from_config(&cfg),
        RuntimeTraceStorageMode::None
    );

    cfg.runtime_trace_mode = "rolling".into();
    assert_eq!(
        storage_mode_from_config(&cfg),
        RuntimeTraceStorageMode::Rolling
    );

    cfg.runtime_trace_mode = "full".into();
    assert_eq!(
        storage_mode_from_config(&cfg),
        RuntimeTraceStorageMode::Full
    );
}

#[test]
fn rolling_mode_keeps_latest_entries() {
    let tmp = tempfile::tempdir().unwrap();
    let path = tmp.path().join("trace.jsonl");
    let logger = RuntimeTraceLogger::new(RuntimeTraceStorageMode::Rolling, 2, path.clone());

    for i in 0..5 {
        let event = RuntimeTraceEvent {
            id: format!("id-{i}"),
            timestamp: Utc::now().to_rfc3339(),
            event_type: "test".into(),
            channel: None,
            provider: None,
            model: None,
            turn_id: None,
            success: None,
            message: Some(format!("event-{i}")),
            payload: serde_json::json!({ "i": i }),
        };
        logger.append(&event).unwrap();
    }

    let events = load_events(&path, 10, None, None).unwrap();
    assert_eq!(events.len(), 2);
    assert_eq!(events[0].message.as_deref(), Some("event-4"));
    assert_eq!(events[1].message.as_deref(), Some("event-3"));
}

#[test]
fn find_event_by_id_returns_match() {
    let tmp = tempfile::tempdir().unwrap();
    let path = tmp.path().join("trace.jsonl");
    let logger = RuntimeTraceLogger::new(RuntimeTraceStorageMode::Full, 100, path.clone());

    let target_id = "target-event";
    let event = RuntimeTraceEvent {
        id: target_id.into(),
        timestamp: Utc::now().to_rfc3339(),
        event_type: "tool_call_result".into(),
        channel: Some("telegram".into()),
        provider: Some("openrouter".into()),
        model: Some("x".into()),
        turn_id: Some("turn-1".into()),
        success: Some(false),
        message: Some("boom".into()),
        payload: serde_json::json!({ "error": "boom" }),
    };
    logger.append(&event).unwrap();

    let found = find_event_by_id(&path, target_id).unwrap();
    assert!(found.is_some());
    assert_eq!(found.unwrap().id, target_id);
}

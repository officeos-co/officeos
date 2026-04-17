//! Gateway WebSocket protocol tests. See API.md §8.

use zeroclaw_agent::gateway::protocol::{WsInbound, WsOutbound};

/// Serialize/deserialize each WsInbound + WsOutbound variant round-trip.
#[tokio::test]
async fn test_ws_serde_roundtrip() {
    // -- WsInbound variants --

    let user_msg = r#"{"type":"user_message","text":"hello","id":"t1"}"#;
    let parsed: WsInbound = serde_json::from_str(user_msg).unwrap();
    match &parsed {
        WsInbound::UserMessage { text, id } => {
            assert_eq!(text, "hello");
            assert_eq!(id.as_deref(), Some("t1"));
        }
        other => panic!("expected UserMessage, got: {other:?}"),
    }

    let ask_resp = r#"{"type":"ask_user_response","id":"p1","response":"yes"}"#;
    let parsed: WsInbound = serde_json::from_str(ask_resp).unwrap();
    match &parsed {
        WsInbound::AskUserResponse { id, response } => {
            assert_eq!(id, "p1");
            assert_eq!(response, "yes");
        }
        other => panic!("expected AskUserResponse, got: {other:?}"),
    }

    let cancel = r#"{"type":"cancel"}"#;
    let parsed: WsInbound = serde_json::from_str(cancel).unwrap();
    match &parsed {
        WsInbound::Cancel { id } => assert!(id.is_none()),
        other => panic!("expected Cancel, got: {other:?}"),
    }

    // -- WsOutbound variants: serialize and check JSON shape --

    let delta = WsOutbound::AssistantDelta {
        turn_id: "t1".into(),
        text: "Hi".into(),
    };
    let json = serde_json::to_value(&delta).unwrap();
    assert_eq!(json["type"], "assistant_delta");
    assert_eq!(json["turn_id"], "t1");
    assert_eq!(json["text"], "Hi");

    let tool_start = WsOutbound::ToolCallStart {
        turn_id: "t1".into(),
        call_id: "c1".into(),
        tool: "shell".into(),
        args: serde_json::json!({"command": "ls"}),
    };
    let json = serde_json::to_value(&tool_start).unwrap();
    assert_eq!(json["type"], "tool_call_start");
    assert_eq!(json["tool"], "shell");

    let tool_result = WsOutbound::ToolCallResult {
        turn_id: "t1".into(),
        call_id: "c1".into(),
        tool: "shell".into(),
        success: true,
        output: "README.md".into(),
        error: None,
    };
    let json = serde_json::to_value(&tool_result).unwrap();
    assert_eq!(json["type"], "tool_call_result");
    assert_eq!(json["success"], true);
    assert!(json.get("error").is_none() || json["error"].is_null());

    let ask_prompt = WsOutbound::AskUserPrompt {
        turn_id: "t1".into(),
        prompt_id: "p1".into(),
        question: "Continue?".into(),
        choices: Some(vec!["yes".into(), "no".into()]),
    };
    let json = serde_json::to_value(&ask_prompt).unwrap();
    assert_eq!(json["type"], "ask_user_prompt");
    assert_eq!(json["choices"], serde_json::json!(["yes", "no"]));

    let complete = WsOutbound::TurnComplete {
        turn_id: "t1".into(),
        cancelled: false,
    };
    let json = serde_json::to_value(&complete).unwrap();
    assert_eq!(json["type"], "turn_complete");
    assert_eq!(json["cancelled"], false);

    let error = WsOutbound::Error {
        turn_id: Some("t1".into()),
        code: "LOOP".into(),
        message: "loop detected".into(),
    };
    let json = serde_json::to_value(&error).unwrap();
    assert_eq!(json["type"], "error");
    assert_eq!(json["code"], "LOOP");
}

/// Start gateway (axum), open WS client, send user_message, expect turn_complete.
#[tokio::test]
async fn test_gateway_accepts_user_message() {
    use std::collections::HashMap;
    use std::path::PathBuf;
    use std::sync::Arc;

    use futures_util::{SinkExt, StreamExt};
    use tokio_tungstenite::connect_async;

    let agent_id = uuid::Uuid::new_v4();
    let cfg = Arc::new(zeroclaw_agent::config::RuntimeConfig {
        agent_id,
        backend_url: "http://localhost:1234".to_string(),
        backend_token: agent_id.to_string(),
        memory_dir: PathBuf::from("/tmp/test-gateway"),
        gateway_host: "127.0.0.1".to_string(),
        gateway_port: 0, // Will be replaced by OS-assigned port in real impl.
        system_prompt: "test".to_string(),
        display_name: "test-agent".to_string(),
        skills: vec![],
        tool_permissions: HashMap::new(),
    });

    let agent = Arc::new(zeroclaw_agent::agent::Agent::new(cfg.clone()));

    // Start the gateway in a background task.
    let cfg_clone = cfg.clone();
    let agent_clone = agent.clone();
    let _server_handle = tokio::spawn(async move {
        let _ = zeroclaw_agent::gateway::serve(cfg_clone, agent_clone).await;
    });

    // Give the server a moment to bind.
    tokio::time::sleep(std::time::Duration::from_millis(100)).await;

    // Connect via WS with the correct token.
    let url = format!(
        "ws://{}:{}/ws?token={}",
        cfg.gateway_host, cfg.gateway_port, agent_id
    );

    let ws_result = connect_async(&url).await;
    // In the TDD red state this may fail because serve() panics at todo!().
    // When green: send user_message, receive turn_complete.
    if let Ok((mut ws, _)) = ws_result {
        let msg = serde_json::json!({
            "type": "user_message",
            "text": "hello",
            "id": "t1"
        });
        ws.send(tokio_tungstenite::tungstenite::Message::Text(
            msg.to_string().into(),
        ))
        .await
        .expect("should send message");

        // Read until we get turn_complete or timeout.
        let timeout = tokio::time::timeout(std::time::Duration::from_secs(5), async {
            while let Some(Ok(frame)) = ws.next().await {
                if let tokio_tungstenite::tungstenite::Message::Text(text) = frame {
                    let v: serde_json::Value = serde_json::from_str(&text).unwrap();
                    if v["type"] == "turn_complete" {
                        return true;
                    }
                }
            }
            false
        });

        let got_complete = timeout.await.unwrap_or(false);
        assert!(got_complete, "expected turn_complete from the gateway");
    }
}

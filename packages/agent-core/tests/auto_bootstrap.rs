//! Tests for auto-bootstrap: BOOTSTRAP.md content is sent as the first
//! message to the agent and synced to the backend as a log entry.

mod helpers;

use std::sync::Arc;
use tempfile::TempDir;
use wiremock::matchers::{header, method, path};
use wiremock::{Mock, MockServer, ResponseTemplate};

/// After personality seed, the auto-bootstrap function reads `BOOTSTRAP.md`,
/// POSTs a `MessageIn` log to the backend, and sends the content as the first
/// user message to the agent. The agent's response is posted as `MessageOut`.
#[tokio::test]
async fn bootstrap_message_sent_and_logged() {
    let server = MockServer::start().await;

    // -- Mock: LLM completions (agent responds with simple text, no tools) --
    let llm_body = "data: {\"choices\":[{\"delta\":{\"content\":\"Hello from agent\"}}]}\n\n\
                    data: {\"choices\":[{\"delta\":{},\"finish_reason\":\"stop\"}]}\n\n\
                    data: [DONE]\n\n";
    Mock::given(method("POST"))
        .and(path("/v1/chat/completions"))
        .respond_with(ResponseTemplate::new(200).set_body_raw(llm_body, "text/event-stream"))
        .mount(&server)
        .await;

    // -- Mock: log forward endpoint (accept any POST) --
    Mock::given(method("POST"))
        .and(path("/api/agents/me/logs"))
        .and(header(
            "Authorization",
            format!("Bearer {}", helpers::CANNED_AGENT_ID).as_str(),
        ))
        .respond_with(ResponseTemplate::new(200).set_body_json(serde_json::json!({
            "id": "00000000-0000-0000-0000-000000000001",
            "agentId": helpers::CANNED_AGENT_ID,
            "time": "2026-04-17T12:00:00Z",
            "type": "MessageIn",
            "content": "test",
            "agentName": null,
            "tool": null,
            "integration": null,
            "channel": null,
            "durationMs": null,
            "inputTokens": null,
            "outputTokens": null,
            "correlationId": null
        })))
        .expect(2) // MessageIn + MessageOut
        .mount(&server)
        .await;

    // -- Set up config with temp memory dir containing BOOTSTRAP.md --
    let tmp = TempDir::new().unwrap();
    let bootstrap_content = "You are a helpful test agent.";
    tokio::fs::write(
        tmp.path().join("BOOTSTRAP.md"),
        format!("# BOOTSTRAP.md\n\n## Operator instructions\n\n{bootstrap_content}"),
    )
    .await
    .unwrap();
    // Seed SOUL.md and IDENTITY.md so prompt composition doesn't fail.
    tokio::fs::write(tmp.path().join("SOUL.md"), "soul")
        .await
        .unwrap();
    tokio::fs::write(tmp.path().join("IDENTITY.md"), "identity")
        .await
        .unwrap();

    let cfg = Arc::new(zeroclaw_agent::config::RuntimeConfig {
        agent_id: helpers::CANNED_AGENT_ID.parse().unwrap(),
        backend_url: server.uri(),
        backend_token: helpers::CANNED_AGENT_ID.to_string(),
        memory_dir: tmp.path().to_path_buf(),
        gateway_host: "0.0.0.0".to_string(),
        gateway_port: 0,
        system_prompt: bootstrap_content.to_string(),
        display_name: "test-agent".to_string(),
        skills: vec![],
        tool_permissions: std::collections::HashMap::new(),
    });

    let agent = Arc::new(zeroclaw_agent::agent::Agent::new(Arc::clone(&cfg)));
    let log_client = zeroclaw_agent::log_client::LogClient::new(
        server.uri(),
        helpers::CANNED_AGENT_ID.to_string(),
    );

    // -- Execute auto-bootstrap --
    zeroclaw_agent::auto_bootstrap::run(&cfg, &agent, &log_client)
        .await
        .expect("auto_bootstrap should succeed");

    // wiremock verifies 2 log POSTs were made (MessageIn + MessageOut).
}

/// If `BOOTSTRAP.md` is missing, `auto_bootstrap` returns an error.
#[tokio::test]
async fn bootstrap_message_fails_without_file() {
    let tmp = TempDir::new().unwrap();
    // No BOOTSTRAP.md written.
    tokio::fs::write(tmp.path().join("SOUL.md"), "soul")
        .await
        .unwrap();
    tokio::fs::write(tmp.path().join("IDENTITY.md"), "identity")
        .await
        .unwrap();

    let cfg = Arc::new(zeroclaw_agent::config::RuntimeConfig {
        agent_id: helpers::CANNED_AGENT_ID.parse().unwrap(),
        backend_url: "http://localhost:1".to_string(),
        backend_token: helpers::CANNED_AGENT_ID.to_string(),
        memory_dir: tmp.path().to_path_buf(),
        gateway_host: "0.0.0.0".to_string(),
        gateway_port: 0,
        system_prompt: "test".to_string(),
        display_name: "test-agent".to_string(),
        skills: vec![],
        tool_permissions: std::collections::HashMap::new(),
    });

    let agent = Arc::new(zeroclaw_agent::agent::Agent::new(Arc::clone(&cfg)));
    let log_client = zeroclaw_agent::log_client::LogClient::new(
        "http://localhost:1".to_string(),
        helpers::CANNED_AGENT_ID.to_string(),
    );

    let result = zeroclaw_agent::auto_bootstrap::run(&cfg, &agent, &log_client).await;
    assert!(result.is_err(), "should fail when BOOTSTRAP.md is missing");
}

//! Tests for the `log_client` module — agent pods forward log entries to the backend.

mod helpers;

use wiremock::matchers::{header, method, path};
use wiremock::{Mock, MockServer, ResponseTemplate};

/// The log client POSTs a `MessageIn` entry to `/api/agents/me/logs` with the
/// agent's bearer token and returns Ok on 200.
#[tokio::test]
async fn forward_message_in_log_to_backend() {
    let server = MockServer::start().await;

    Mock::given(method("POST"))
        .and(path("/api/agents/me/logs"))
        .and(header(
            "Authorization",
            format!("Bearer {}", helpers::CANNED_AGENT_ID).as_str(),
        ))
        .and(header("Content-Type", "application/json"))
        .respond_with(ResponseTemplate::new(200).set_body_json(serde_json::json!({
            "id": "00000000-0000-0000-0000-000000000001",
            "agentId": helpers::CANNED_AGENT_ID,
            "agentName": null,
            "time": "2026-04-17T12:00:00Z",
            "type": "MessageIn",
            "tool": null,
            "integration": null,
            "channel": null,
            "content": "hello world",
            "durationMs": null,
            "inputTokens": null,
            "outputTokens": null,
            "correlationId": "corr-1"
        })))
        .expect(1)
        .mount(&server)
        .await;

    let client = zeroclaw_agent::log_client::LogClient::new(
        server.uri(),
        helpers::CANNED_AGENT_ID.to_string(),
    );

    client
        .forward("MessageIn", "hello world", Some("corr-1".to_string()))
        .await
        .expect("forward should succeed");

    // wiremock asserts the expected request was received on drop.
}

/// The log client returns an error on non-200 responses.
#[tokio::test]
async fn forward_log_returns_error_on_server_failure() {
    let server = MockServer::start().await;

    Mock::given(method("POST"))
        .and(path("/api/agents/me/logs"))
        .respond_with(ResponseTemplate::new(500))
        .mount(&server)
        .await;

    let client = zeroclaw_agent::log_client::LogClient::new(
        server.uri(),
        helpers::CANNED_AGENT_ID.to_string(),
    );

    let result = client.forward("System", "test", None).await;
    assert!(result.is_err(), "should return error on 500");
}

/// A server that never responds must trigger a timeout error instead of
/// hanging forever.
#[tokio::test]
async fn forward_log_times_out_on_unresponsive_server() {
    let server = MockServer::start().await;

    Mock::given(method("POST"))
        .and(path("/api/agents/me/logs"))
        .respond_with(
            ResponseTemplate::new(200).set_delay(std::time::Duration::from_secs(60)),
        )
        .mount(&server)
        .await;

    let client = zeroclaw_agent::log_client::LogClient::new(
        server.uri(),
        helpers::CANNED_AGENT_ID.to_string(),
    );

    let start = std::time::Instant::now();
    let result = client.forward("System", "test", None).await;
    let elapsed = start.elapsed();

    assert!(result.is_err(), "should error on timeout, not hang");
    assert!(
        elapsed < std::time::Duration::from_secs(20),
        "should timeout well before 60s, took {elapsed:?}"
    );
}

//! No-default-fallback validation tests. See API.md §1, §19.

mod helpers;

use wiremock::matchers::{method, path};
use wiremock::{Mock, MockServer, ResponseTemplate};

use helpers::CANNED_AGENT_ID;

/// Bootstrap payload with gateway port 0 should produce `Error::BootstrapPayload`.
#[tokio::test]
async fn test_runtime_config_rejects_zero_gateway_port() {
    let server = MockServer::start().await;

    Mock::given(method("GET"))
        .and(path(format!("/api/agents/{CANNED_AGENT_ID}")))
        .respond_with(
            ResponseTemplate::new(200)
                .set_body_json(helpers::canned_payload_zero_port()),
        )
        .expect(1)
        .mount(&server)
        .await;

    let result = zeroclaw_agent::bootstrap::bootstrap(
        CANNED_AGENT_ID.to_string(),
        server.uri(),
    )
    .await;

    assert!(result.is_err(), "port 0 should be rejected");
    let err = result.unwrap_err().to_string();
    assert!(
        err.contains("bootstrap invalid payload") || err.contains("port"),
        "error should mention invalid payload or port, got: {err}"
    );
}

/// Bootstrap payload with empty systemPrompt should produce an error.
#[tokio::test]
async fn test_bootstrap_rejects_empty_system_prompt() {
    let server = MockServer::start().await;

    Mock::given(method("GET"))
        .and(path(format!("/api/agents/{CANNED_AGENT_ID}")))
        .respond_with(
            ResponseTemplate::new(200)
                .set_body_json(helpers::canned_payload_empty_prompt()),
        )
        .expect(1)
        .mount(&server)
        .await;

    let result = zeroclaw_agent::bootstrap::bootstrap(
        CANNED_AGENT_ID.to_string(),
        server.uri(),
    )
    .await;

    assert!(result.is_err(), "empty systemPrompt should be rejected");
    let err = result.unwrap_err().to_string();
    assert!(
        err.contains("bootstrap invalid payload") || err.contains("prompt"),
        "error should mention invalid payload or prompt, got: {err}"
    );
}

//! Bootstrap integration tests. See API.md §3 + §17 tests 1–3.

mod helpers;

use wiremock::matchers::{header, method, path};
use wiremock::{Mock, MockServer, ResponseTemplate};

use helpers::{canned_payload, CANNED_AGENT_ID};

/// Wiremock serves a canned `AgentBootstrapPayload` JSON.
/// Call `bootstrap(id, url)`. Assert RuntimeConfig fields match the payload.
#[tokio::test]
async fn test_bootstrap_roundtrip_ok() {
    let server = MockServer::start().await;

    Mock::given(method("GET"))
        .and(path(format!("/api/agents/{CANNED_AGENT_ID}")))
        .and(header(
            "Authorization",
            format!("Bearer {CANNED_AGENT_ID}").as_str(),
        ))
        .respond_with(ResponseTemplate::new(200).set_body_json(canned_payload()))
        .expect(1)
        .mount(&server)
        .await;

    let cfg = zeroclaw_agent::bootstrap::bootstrap(CANNED_AGENT_ID.to_string(), server.uri())
        .await
        .expect("bootstrap should succeed");

    assert_eq!(cfg.agent_id.to_string(), CANNED_AGENT_ID);
    assert_eq!(cfg.display_name, "test-agent");
    assert_eq!(cfg.system_prompt, "You are a helpful test agent.");
    assert_eq!(cfg.gateway_host, "0.0.0.0");
    assert_eq!(cfg.gateway_port, 9090);
    assert_eq!(cfg.skills.len(), 2);
    assert_eq!(cfg.skills[0].name, "github");
    assert_eq!(cfg.backend_token, CANNED_AGENT_ID);

    // Permission map should be lowercased.
    use zeroclaw_agent::config::Permission;
    assert_eq!(
        cfg.tool_permissions
            .get(&("github".into(), "list_issues".into())),
        Some(&Permission::Allow)
    );
    assert_eq!(
        cfg.tool_permissions
            .get(&("notion".into(), "search".into())),
        Some(&Permission::Deny)
    );
}

/// Call `load_env()` with unset vars, assert `Error::Env`.
#[tokio::test]
async fn test_bootstrap_missing_env_panics() {
    // Ensure vars are not set (they shouldn't be in test, but be explicit).
    std::env::remove_var("ZEROCLAW_AGENT_ID");
    std::env::remove_var("BACKEND_URL");

    let result = zeroclaw_agent::env::load_env();
    assert!(result.is_err());
    let err = result.unwrap_err();
    let msg = err.to_string();
    assert!(
        msg.contains("environment"),
        "expected Env error, got: {msg}"
    );
}

/// Wiremock returns 503 three times, then 200. Assert bootstrap succeeds.
#[tokio::test]
async fn test_bootstrap_retries_then_succeeds() {
    let server = MockServer::start().await;
    let agent_path = format!("/api/agents/{CANNED_AGENT_ID}");

    // First 3 requests: 503.
    Mock::given(method("GET"))
        .and(path(&agent_path))
        .respond_with(ResponseTemplate::new(503))
        .up_to_n_times(3)
        .expect(3)
        .mount(&server)
        .await;

    // 4th request: 200 with the real payload.
    Mock::given(method("GET"))
        .and(path(&agent_path))
        .respond_with(ResponseTemplate::new(200).set_body_json(canned_payload()))
        .expect(1)
        .mount(&server)
        .await;

    let cfg = zeroclaw_agent::bootstrap::bootstrap(CANNED_AGENT_ID.to_string(), server.uri())
        .await
        .expect("bootstrap should succeed after retries");

    assert_eq!(cfg.agent_id.to_string(), CANNED_AGENT_ID);
}

/// Wiremock always returns 503. Assert error after 10 attempts.
#[tokio::test]
async fn test_bootstrap_exhausts_retries() {
    let server = MockServer::start().await;

    Mock::given(method("GET"))
        .and(path(format!("/api/agents/{CANNED_AGENT_ID}")))
        .respond_with(ResponseTemplate::new(503))
        .expect(10)
        .mount(&server)
        .await;

    let result =
        zeroclaw_agent::bootstrap::bootstrap(CANNED_AGENT_ID.to_string(), server.uri()).await;

    assert!(result.is_err(), "should fail after exhausting retries");
}

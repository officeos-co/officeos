//! Shared test helpers — canned payloads, config builders, wiremock matchers.
//!
//! Each integration test binary compiles this module independently and may
//! only use a subset of helpers; `#[allow(dead_code)]` prevents per-binary
//! warnings for the unused subset.

/// Returns a realistic `AgentBootstrapPayload` JSON matching API.md §3.2.
#[allow(dead_code, reason = "not all test binaries use every helper")]
pub fn canned_payload() -> serde_json::Value {
    serde_json::json!({
        "agentId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
        "displayName": "test-agent",
        "systemPrompt": "You are a helpful test agent.",
        "provider": {
            "name": "openai",
            "model": "gpt-4",
            "apiUrl": "https://api.openai.com/v1",
            "tokenRef": null
        },
        "proxy": {
            "url": "https://api.officeos.co/v1",
            "token": null
        },
        "gateway": {
            "host": "0.0.0.0",
            "port": 9090,
            "tlsCertRef": null
        },
        "skills": [
            {"name": "github"},
            {"name": "notion"}
        ],
        "toolPermissions": {
            "entries": [
                {"skill": "github", "tool": "list_issues", "mode": "allow"},
                {"skill": "notion", "tool": "search", "mode": "deny"}
            ]
        }
    })
}

/// Returns a canned payload with gateway port set to 0 (invalid).
#[allow(dead_code, reason = "not all test binaries use every helper")]
pub fn canned_payload_zero_port() -> serde_json::Value {
    let mut p = canned_payload();
    p["gateway"]["port"] = serde_json::json!(0);
    p
}

/// Returns a canned payload with empty systemPrompt.
#[allow(dead_code, reason = "not all test binaries use every helper")]
pub fn canned_payload_empty_prompt() -> serde_json::Value {
    let mut p = canned_payload();
    p["systemPrompt"] = serde_json::json!("");
    p
}

/// The agent UUID used in the canned payload.
#[allow(dead_code, reason = "not all test binaries use every helper")]
pub const CANNED_AGENT_ID: &str = "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";

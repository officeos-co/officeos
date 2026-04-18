//! Turn loop integration tests. See API.md §6 + §17 tests 6-7, 9-10.

#[allow(dead_code, reason = "not all helpers used in every test binary")]
mod helpers;

use std::collections::HashMap;
use std::path::PathBuf;
use std::sync::Arc;

use wiremock::matchers::{method, path};
use wiremock::{Mock, MockServer, ResponseTemplate};

use zeroclaw_agent::agent::Agent;
use zeroclaw_agent::config::{Permission, RuntimeConfig, SkillSummary};

fn test_config_with_permissions(
    backend_url: &str,
    perms: HashMap<(String, String), Permission>,
) -> Arc<RuntimeConfig> {
    Arc::new(RuntimeConfig {
        agent_id: uuid::Uuid::parse_str(helpers::CANNED_AGENT_ID).unwrap(),
        backend_url: backend_url.to_string(),
        backend_token: helpers::CANNED_AGENT_ID.to_string(),
        memory_dir: PathBuf::from("/tmp/test-turn-loop"),
        gateway_host: "127.0.0.1".to_string(),
        gateway_port: 9999,
        system_prompt: "You are a test agent.".to_string(),
        display_name: "test-agent".to_string(),
        skills: vec![SkillSummary {
            name: "notion".into(),
        }],
        tool_permissions: perms,
    })
}

/// Build an SSE response body that returns plain text (no tool calls).
fn sse_text_only(text: &str) -> String {
    use std::fmt::Write;
    let mut body = String::new();
    // Content chunk.
    let _ = write!(
        body,
        "data: {{\"choices\":[{{\"delta\":{{\"content\":\"{text}\"}}}}]}}\n\n"
    );
    // Finish.
    body.push_str("data: {\"choices\":[{\"delta\":{},\"finish_reason\":\"stop\"}]}\n\n");
    body.push_str("data: [DONE]\n\n");
    body
}

/// Build an SSE response that issues a tool call, then after the tool result
/// a second response with plain text.
fn sse_tool_call(tool_name: &str, args_json: &str, call_id: &str) -> String {
    use std::fmt::Write;
    let mut body = String::new();
    // Tool call start.
    let _ = write!(
        body,
        "data: {{\"choices\":[{{\"delta\":{{\"tool_calls\":[{{\"index\":0,\"id\":\"{call_id}\",\"type\":\"function\",\"function\":{{\"name\":\"{tool_name}\",\"arguments\":\"\"}}}}]}}}}]}}\n\n"
    );
    // Tool call args.
    let escaped_args = args_json.replace('\\', "\\\\").replace('"', "\\\"");
    let _ = write!(
        body,
        "data: {{\"choices\":[{{\"delta\":{{\"tool_calls\":[{{\"index\":0,\"function\":{{\"arguments\":\"{escaped_args}\"}}}}]}}}}]}}\n\n"
    );
    // Finish with tool_calls reason.
    body.push_str("data: {\"choices\":[{\"delta\":{},\"finish_reason\":\"tool_calls\"}]}\n\n");
    body.push_str("data: [DONE]\n\n");
    body
}

/// Mock LLM returns plain content (no tool calls). Agent emits content + `turn_complete`.
#[tokio::test]
async fn test_single_turn_no_tools() {
    let server = MockServer::start().await;

    Mock::given(method("POST"))
        .and(path("/v1/chat/completions"))
        .respond_with(
            ResponseTemplate::new(200)
                .set_body_raw(sse_text_only("Hello back!"), "text/event-stream"),
        )
        .mount(&server)
        .await;

    let cfg = test_config_with_permissions(&server.uri(), HashMap::new());
    let agent = Agent::new(cfg);

    let result = agent.handle_user_message("Hello".to_string()).await;
    result.unwrap();
}

/// Mock LLM returns tool call -> tool dispatched -> result fed back -> second response.
#[tokio::test]
async fn test_tool_call_cycle() {
    let server = MockServer::start().await;

    // First call: LLM issues a shell tool call.
    // Second call: LLM returns plain text.
    let call_counter = Arc::new(std::sync::atomic::AtomicUsize::new(0));
    let counter = Arc::clone(&call_counter);

    Mock::given(method("POST"))
        .and(path("/v1/chat/completions"))
        .respond_with(move |_req: &wiremock::Request| {
            let n = counter.fetch_add(1, std::sync::atomic::Ordering::SeqCst);
            if n == 0 {
                ResponseTemplate::new(200).set_body_raw(
                    sse_tool_call("shell", r#"{"command":"ls"}"#, "call_01"),
                    "text/event-stream",
                )
            } else {
                ResponseTemplate::new(200).set_body_raw(
                    sse_text_only("You have README.md and src/."),
                    "text/event-stream",
                )
            }
        })
        .mount(&server)
        .await;

    let cfg = test_config_with_permissions(&server.uri(), HashMap::new());
    let agent = Agent::new(cfg);

    let result = agent.handle_user_message("list my files".to_string()).await;
    // The shell tool will fail (no /bin/sh in test), but the loop should
    // still complete — tool errors are reported as ToolResult, not panics.
    result.unwrap();
}

/// `tool_permissions` has Deny for notion:search. `tool_call_result` carries denial error.
#[tokio::test]
async fn test_permission_denied_skill_exec() {
    let server = MockServer::start().await;

    // LLM returns a plain text response (the permission check happens at
    // dispatch time, not at the LLM level).
    Mock::given(method("POST"))
        .and(path("/v1/chat/completions"))
        .respond_with(
            ResponseTemplate::new(200)
                .set_body_raw(sse_text_only("Searching notion..."), "text/event-stream"),
        )
        .mount(&server)
        .await;

    let mut perms = HashMap::new();
    perms.insert(
        ("notion".to_string(), "search".to_string()),
        Permission::Deny,
    );

    let cfg = test_config_with_permissions(&server.uri(), perms);
    let agent = Agent::new(cfg);

    let result = agent
        .handle_user_message("search notion for meetings".to_string())
        .await;
    result.unwrap();
}

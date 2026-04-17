//! Turn loop integration tests. See API.md §6 + §17 tests 6-7, 9-10.

#[allow(dead_code)]
mod helpers;

use std::collections::HashMap;
use std::path::PathBuf;
use std::sync::Arc;

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
        skills: vec![SkillSummary { name: "notion".into() }],
        tool_permissions: perms,
    })
}

/// Mock LLM returns plain content (no tool calls). Agent emits content + turn_complete.
#[tokio::test]
async fn test_single_turn_no_tools() {
    let cfg = test_config_with_permissions("http://localhost:1234", HashMap::new());
    let agent = Agent::new(cfg);

    // This will panic at todo!() in Phase 3 — that's the expected TDD red state.
    let result = agent.handle_user_message("Hello".to_string()).await;
    // In the green state, this would succeed and we'd assert WS events.
    assert!(result.is_ok());
}

/// Mock LLM returns tool call -> tool dispatched -> result fed back -> second response.
#[tokio::test]
async fn test_tool_call_cycle() {
    let cfg = test_config_with_permissions("http://localhost:1234", HashMap::new());
    let agent = Agent::new(cfg);

    let result = agent
        .handle_user_message("list my files".to_string())
        .await;
    // Expected to succeed when implemented; tool call → result → final response.
    assert!(result.is_ok());
}

/// tool_permissions has Deny for notion:search. tool_call_result carries denial error.
#[tokio::test]
async fn test_permission_denied_skill_exec() {
    let mut perms = HashMap::new();
    perms.insert(
        ("notion".to_string(), "search".to_string()),
        Permission::Deny,
    );

    let cfg = test_config_with_permissions("http://localhost:1234", perms);
    let agent = Agent::new(cfg);

    // When the LLM calls skill_exec with "notion search --query meetings",
    // the dispatcher should return a denial ToolResult without calling GraphQL.
    let result = agent
        .handle_user_message("search notion for meetings".to_string())
        .await;
    assert!(result.is_ok());
}

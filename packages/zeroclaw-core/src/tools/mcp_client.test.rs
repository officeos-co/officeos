use super::*;
use crate::config::schema::McpTransport;

#[test]
fn tool_name_prefix_format() {
    let prefixed = format!("{}__{}", "filesystem", "read_file");
    assert_eq!(prefixed, "filesystem__read_file");
}

#[tokio::test]
async fn connect_nonexistent_command_fails_cleanly() {
    // A command that doesn't exist should fail at spawn, not panic.
    let config = McpServerConfig {
        name: "nonexistent".to_string(),
        command: "/usr/bin/this_binary_does_not_exist_zeroclaw_test".to_string(),
        args: vec![],
        env: std::collections::HashMap::default(),
        tool_timeout_secs: None,
        transport: McpTransport::Stdio,
        url: None,
        headers: std::collections::HashMap::default(),
    };
    let result = McpServer::connect(config).await;
    assert!(result.is_err());
    let msg = result.err().unwrap().to_string();
    assert!(msg.contains("failed to create transport"), "got: {msg}");
}

#[tokio::test]
async fn connect_all_nonfatal_on_single_failure() {
    // If one server config is bad, connect_all should succeed (with 0 servers).
    let configs = vec![McpServerConfig {
        name: "bad".to_string(),
        command: "/usr/bin/does_not_exist_zc_test".to_string(),
        args: vec![],
        env: std::collections::HashMap::default(),
        tool_timeout_secs: None,
        transport: McpTransport::Stdio,
        url: None,
        headers: std::collections::HashMap::default(),
    }];
    let registry = McpRegistry::connect_all(&configs)
        .await
        .expect("connect_all should not fail");
    assert!(registry.is_empty());
    assert_eq!(registry.tool_count(), 0);
}

#[test]
fn http_transport_requires_url() {
    let config = McpServerConfig {
        name: "test".into(),
        transport: McpTransport::Http,
        ..Default::default()
    };
    let result = create_transport(&config);
    assert!(result.is_err());
}

#[test]
fn sse_transport_requires_url() {
    let config = McpServerConfig {
        name: "test".into(),
        transport: McpTransport::Sse,
        ..Default::default()
    };
    let result = create_transport(&config);
    assert!(result.is_err());
}

// ── Empty registry (no servers) ────────────────────────────────────────

#[tokio::test]
async fn empty_registry_is_empty() {
    let registry = McpRegistry::connect_all(&[])
        .await
        .expect("connect_all on empty slice should succeed");
    assert!(registry.is_empty());
    assert_eq!(registry.server_count(), 0);
    assert_eq!(registry.tool_count(), 0);
}

#[tokio::test]
async fn empty_registry_tool_names_is_empty() {
    let registry = McpRegistry::connect_all(&[])
        .await
        .expect("connect_all should succeed");
    assert!(registry.tool_names().is_empty());
}

#[tokio::test]
async fn empty_registry_get_tool_def_returns_none() {
    let registry = McpRegistry::connect_all(&[])
        .await
        .expect("connect_all should succeed");
    let result = registry.get_tool_def("nonexistent__tool").await;
    assert!(result.is_none());
}

#[tokio::test]
async fn empty_registry_call_tool_unknown_name_returns_error() {
    let registry = McpRegistry::connect_all(&[])
        .await
        .expect("connect_all should succeed");
    let err = registry
        .call_tool("nonexistent__tool", serde_json::json!({}))
        .await
        .expect_err("should fail for unknown tool");
    assert!(err.to_string().contains("unknown MCP tool"), "got: {err}");
}

#[tokio::test]
async fn connect_all_empty_gives_zero_servers() {
    let registry = McpRegistry::connect_all(&[])
        .await
        .expect("connect_all should succeed");
    // Verify all three count methods agree on zero.
    assert_eq!(registry.server_count(), 0);
    assert_eq!(registry.tool_count(), 0);
    assert!(registry.is_empty());
}

use super::*;
use crate::tools::mcp_client::McpRegistry;
use crate::tools::mcp_deferred::DeferredMcpToolStub;
use crate::tools::mcp_protocol::McpToolDef;

async fn make_deferred_set(stubs: Vec<DeferredMcpToolStub>) -> DeferredMcpToolSet {
    let registry = Arc::new(McpRegistry::connect_all(&[]).await.unwrap());
    DeferredMcpToolSet { stubs, registry }
}

fn make_stub(name: &str, desc: &str) -> DeferredMcpToolStub {
    let def = McpToolDef {
        name: name.to_string(),
        description: Some(desc.to_string()),
        input_schema: serde_json::json!({"type": "object", "properties": {}}),
    };
    DeferredMcpToolStub::new(name.to_string(), def)
}

#[tokio::test]
async fn tool_metadata() {
    let tool = ToolSearchTool::new(
        make_deferred_set(vec![]).await,
        Arc::new(Mutex::new(ActivatedToolSet::new())),
    );
    assert_eq!(tool.name(), "tool_search");
    assert!(!tool.description().is_empty());
    assert!(tool.parameters_schema()["properties"]["query"].is_object());
}

#[tokio::test]
async fn empty_query_returns_error() {
    let tool = ToolSearchTool::new(
        make_deferred_set(vec![]).await,
        Arc::new(Mutex::new(ActivatedToolSet::new())),
    );
    let result = tool
        .execute(serde_json::json!({"query": ""}))
        .await
        .unwrap();
    assert!(!result.success);
}

#[tokio::test]
async fn select_nonexistent_tool_reports_not_found() {
    let tool = ToolSearchTool::new(
        make_deferred_set(vec![]).await,
        Arc::new(Mutex::new(ActivatedToolSet::new())),
    );
    let result = tool
        .execute(serde_json::json!({"query": "select:nonexistent"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("Not found"));
}

#[tokio::test]
async fn keyword_search_no_matches() {
    let tool = ToolSearchTool::new(
        make_deferred_set(vec![make_stub("fs__read", "Read file")]).await,
        Arc::new(Mutex::new(ActivatedToolSet::new())),
    );
    let result = tool
        .execute(serde_json::json!({"query": "zzzzz_nonexistent"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("No matching"));
}

#[tokio::test]
async fn keyword_search_finds_match() {
    let activated = Arc::new(Mutex::new(ActivatedToolSet::new()));
    let tool = ToolSearchTool::new(
        make_deferred_set(vec![make_stub("fs__read", "Read a file from disk")]).await,
        Arc::clone(&activated),
    );
    let result = tool
        .execute(serde_json::json!({"query": "read file"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("<function>"));
    assert!(result.output.contains("fs__read"));
    // Tool should now be activated
    assert!(activated.lock().unwrap().is_activated("fs__read"));
}

/// Verify tool_search works with stubs from multiple MCP servers,
/// simulating a daemon-mode setup where several servers are deferred.
#[tokio::test]
async fn multiple_servers_stubs_all_searchable() {
    let activated = Arc::new(Mutex::new(ActivatedToolSet::new()));
    let stubs = vec![
        make_stub("server_a__list_files", "List files on server A"),
        make_stub("server_a__read_file", "Read file on server A"),
        make_stub("server_b__query_db", "Query database on server B"),
        make_stub("server_b__insert_row", "Insert row on server B"),
    ];
    let tool = ToolSearchTool::new(make_deferred_set(stubs).await, Arc::clone(&activated));

    // Search should find tools across both servers
    let result = tool
        .execute(serde_json::json!({"query": "file"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("server_a__list_files"));
    assert!(result.output.contains("server_a__read_file"));

    // Server B tools should also be searchable
    let result = tool
        .execute(serde_json::json!({"query": "database query"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(result.output.contains("server_b__query_db"));
}

/// Verify select mode activates tools and they stay activated across calls,
/// matching the daemon-mode pattern where a single ActivatedToolSet persists.
#[tokio::test]
async fn select_activates_and_persists_across_calls() {
    let activated = Arc::new(Mutex::new(ActivatedToolSet::new()));
    let stubs = vec![
        make_stub("srv__tool_a", "Tool A"),
        make_stub("srv__tool_b", "Tool B"),
    ];
    let tool = ToolSearchTool::new(make_deferred_set(stubs).await, Arc::clone(&activated));

    // Activate tool_a
    let result = tool
        .execute(serde_json::json!({"query": "select:srv__tool_a"}))
        .await
        .unwrap();
    assert!(result.success);
    assert!(activated.lock().unwrap().is_activated("srv__tool_a"));
    assert!(!activated.lock().unwrap().is_activated("srv__tool_b"));

    // Activate tool_b in a separate call
    let result = tool
        .execute(serde_json::json!({"query": "select:srv__tool_b"}))
        .await
        .unwrap();
    assert!(result.success);

    // Both should remain activated
    let guard = activated.lock().unwrap();
    assert!(guard.is_activated("srv__tool_a"));
    assert!(guard.is_activated("srv__tool_b"));
    assert_eq!(guard.tool_specs().len(), 2);
}

/// Verify re-activating an already-activated tool does not duplicate it.
#[tokio::test]
async fn reactivation_is_idempotent() {
    let activated = Arc::new(Mutex::new(ActivatedToolSet::new()));
    let tool = ToolSearchTool::new(
        make_deferred_set(vec![make_stub("srv__tool", "A tool")]).await,
        Arc::clone(&activated),
    );

    tool.execute(serde_json::json!({"query": "select:srv__tool"}))
        .await
        .unwrap();
    tool.execute(serde_json::json!({"query": "select:srv__tool"}))
        .await
        .unwrap();

    assert_eq!(activated.lock().unwrap().tool_specs().len(), 1);
}

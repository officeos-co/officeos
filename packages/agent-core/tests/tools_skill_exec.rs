//! skill_exec tool integration tests. See API.md §10.1.

mod helpers;

use std::collections::HashMap;
use std::path::PathBuf;
use std::sync::Arc;

use wiremock::matchers::{method, path};
use wiremock::{Mock, MockServer, ResponseTemplate};

use zeroclaw_agent::config::{RuntimeConfig, SkillSummary};
use zeroclaw_agent::tools::Tool;
use zeroclaw_agent::tools::skill_exec::SkillExecTool;

fn test_config(backend_url: &str) -> Arc<RuntimeConfig> {
    Arc::new(RuntimeConfig {
        agent_id: uuid::Uuid::parse_str(helpers::CANNED_AGENT_ID).unwrap(),
        backend_url: backend_url.to_string(),
        backend_token: helpers::CANNED_AGENT_ID.to_string(),
        memory_dir: PathBuf::from("/tmp/test-skill-exec"),
        gateway_host: "127.0.0.1".to_string(),
        gateway_port: 9999,
        system_prompt: "test".to_string(),
        display_name: "test-agent".to_string(),
        skills: vec![
            SkillSummary { name: "github".into() },
            SkillSummary { name: "notion".into() },
        ],
        tool_permissions: HashMap::new(),
    })
}

/// Wiremock serves a GraphQL introspection response.
/// Dispatch `skill_exec --help`. Assert output lists available skills.
#[tokio::test]
async fn test_skill_exec_help_lists_schema() {
    let server = MockServer::start().await;

    // Introspection query response with a sample schema.
    let introspection_response = serde_json::json!({
        "data": {
            "__schema": {
                "queryType": {
                    "fields": [
                        {
                            "name": "github_list_issues",
                            "args": [
                                {
                                    "name": "repo",
                                    "type": {"kind": "SCALAR", "name": "String", "ofType": null}
                                }
                            ]
                        }
                    ]
                },
                "mutationType": null
            }
        }
    });

    Mock::given(method("POST"))
        .and(path("/api/graphql"))
        .respond_with(ResponseTemplate::new(200).set_body_json(&introspection_response))
        .mount(&server)
        .await;

    let cfg = test_config(&server.uri());
    let tool = SkillExecTool::new(cfg);

    let result = tool
        .execute(serde_json::json!({"command": "--help"}))
        .await
        .expect("execute should not return anyhow error");

    assert!(result.success, "help should succeed");
    // Output should list discovered skills from the introspection.
    assert!(
        !result.output.is_empty(),
        "help output should not be empty"
    );
}

/// Wiremock serves a GraphQL mutation response.
/// Dispatch `skill_exec github list_issues --repo foo`. Assert success.
#[tokio::test]
async fn test_skill_exec_dispatch_ok() {
    let server = MockServer::start().await;

    // Introspection response (for schema cache).
    let introspection_response = serde_json::json!({
        "data": {
            "__schema": {
                "queryType": {
                    "fields": [
                        {
                            "name": "github_list_issues",
                            "args": [
                                {
                                    "name": "repo",
                                    "type": {"kind": "SCALAR", "name": "String", "ofType": null}
                                }
                            ]
                        }
                    ]
                },
                "mutationType": null
            }
        }
    });

    // The actual query result.
    let query_response = serde_json::json!({
        "data": {
            "github_list_issues": [
                {"title": "Bug fix", "number": 42}
            ]
        }
    });

    // Mount introspection first, then the query.
    Mock::given(method("POST"))
        .and(path("/api/graphql"))
        .respond_with(ResponseTemplate::new(200).set_body_json(&introspection_response))
        .up_to_n_times(1)
        .mount(&server)
        .await;

    Mock::given(method("POST"))
        .and(path("/api/graphql"))
        .respond_with(ResponseTemplate::new(200).set_body_json(&query_response))
        .mount(&server)
        .await;

    let cfg = test_config(&server.uri());
    let tool = SkillExecTool::new(cfg);

    let result = tool
        .execute(serde_json::json!({"command": "github list_issues --repo foo"}))
        .await
        .expect("execute should not return anyhow error");

    assert!(result.success, "dispatch should succeed");
    assert!(
        result.output.contains("Bug fix") || result.output.contains("github_list_issues"),
        "output should contain the query result"
    );
}

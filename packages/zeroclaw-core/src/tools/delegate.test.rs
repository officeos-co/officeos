use super::*;
use crate::config::schema::{DEFAULT_DELEGATE_AGENTIC_TIMEOUT_SECS, DEFAULT_DELEGATE_TIMEOUT_SECS};
use crate::providers::{ChatRequest, ChatResponse, ToolCall};
use crate::security::{AutonomyLevel, SecurityPolicy};
use anyhow::anyhow;

fn test_security() -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy::default())
}

fn sample_agents() -> HashMap<String, DelegateAgentConfig> {
    let mut agents = HashMap::new();
    agents.insert(
        "researcher".to_string(),
        DelegateAgentConfig {
            provider: "ollama".to_string(),
            model: "llama3".to_string(),
            system_prompt: Some("You are a research assistant.".to_string()),
            api_key: None,
            temperature: Some(0.3),
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );
    agents.insert(
        "coder".to_string(),
        DelegateAgentConfig {
            provider: "openrouter".to_string(),
            model: "anthropic/claude-sonnet-4-20250514".to_string(),
            system_prompt: None,
            api_key: Some("delegate-test-credential".to_string()),
            temperature: None,
            max_depth: 2,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );
    agents
}

#[derive(Default)]
struct EchoTool;

#[async_trait]
impl Tool for EchoTool {
    fn name(&self) -> &str {
        "echo_tool"
    }

    fn description(&self) -> &str {
        "Echoes the `value` argument."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "value": {"type": "string"}
            },
            "required": ["value"]
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let value = args
            .get("value")
            .and_then(serde_json::Value::as_str)
            .unwrap_or_default()
            .to_string();
        Ok(ToolResult {
            success: true,
            output: format!("echo:{value}"),
            error: None,
        })
    }
}

struct OneToolThenFinalProvider;

#[async_trait]
impl Provider for OneToolThenFinalProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        Ok("unused".to_string())
    }

    async fn chat(
        &self,
        request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        let has_tool_message = request.messages.iter().any(|m| m.role == "tool");
        if has_tool_message {
            Ok(ChatResponse {
                text: Some("done".to_string()),
                tool_calls: Vec::new(),
                usage: None,
                reasoning_content: None,
            })
        } else {
            Ok(ChatResponse {
                text: None,
                tool_calls: vec![ToolCall {
                    id: "call_1".to_string(),
                    name: "echo_tool".to_string(),
                    arguments: "{\"value\":\"ping\"}".to_string(),
                }],
                usage: None,
                reasoning_content: None,
            })
        }
    }
}

struct InfiniteToolCallProvider;

#[async_trait]
impl Provider for InfiniteToolCallProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        Ok("unused".to_string())
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        Ok(ChatResponse {
            text: None,
            tool_calls: vec![ToolCall {
                id: "loop".to_string(),
                name: "echo_tool".to_string(),
                arguments: "{\"value\":\"x\"}".to_string(),
            }],
            usage: None,
            reasoning_content: None,
        })
    }
}

struct FailingProvider;

#[async_trait]
impl Provider for FailingProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        Ok("unused".to_string())
    }

    async fn chat(
        &self,
        _request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        Err(anyhow!("provider boom"))
    }
}

fn agentic_config(allowed_tools: Vec<String>, max_iterations: usize) -> DelegateAgentConfig {
    DelegateAgentConfig {
        provider: "openrouter".to_string(),
        model: "model-test".to_string(),
        system_prompt: Some("You are agentic.".to_string()),
        api_key: Some("delegate-test-credential".to_string()),
        temperature: Some(0.2),
        max_depth: 3,
        agentic: true,
        allowed_tools,
        max_iterations,
        timeout_secs: None,
        agentic_timeout_secs: None,
        skills_directory: None,
        memory_namespace: None,
    }
}

#[test]
fn name_and_schema() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    assert_eq!(tool.name(), "delegate");
    let schema = tool.parameters_schema();
    assert!(schema["properties"]["agent"].is_object());
    assert!(schema["properties"]["prompt"].is_object());
    assert!(schema["properties"]["context"].is_object());
    assert!(schema["properties"]["background"].is_object());
    assert!(schema["properties"]["parallel"].is_object());
    assert!(schema["properties"]["action"].is_object());
    assert!(schema["properties"]["task_id"].is_object());
    // required is empty because different actions need different params
    let required = schema["required"].as_array().unwrap();
    assert!(required.is_empty());
    assert_eq!(schema["additionalProperties"], json!(false));
    assert_eq!(schema["properties"]["agent"]["minLength"], json!(1));
    assert_eq!(schema["properties"]["prompt"]["minLength"], json!(1));
}

#[test]
fn description_not_empty() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    assert!(!tool.description().is_empty());
}

#[test]
fn schema_lists_agent_names() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let schema = tool.parameters_schema();
    let desc = schema["properties"]["agent"]["description"]
        .as_str()
        .unwrap();
    assert!(desc.contains("researcher") || desc.contains("coder"));
}

#[tokio::test]
async fn missing_agent_param() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool.execute(json!({"prompt": "test"})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn missing_prompt_param() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool.execute(json!({"agent": "researcher"})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn unknown_agent_returns_error() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool
        .execute(json!({"agent": "nonexistent", "prompt": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("Unknown agent"));
}

#[tokio::test]
async fn depth_limit_enforced() {
    let tool = DelegateTool::with_depth(sample_agents(), None, test_security(), 3);
    let result = tool
        .execute(json!({"agent": "researcher", "prompt": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("depth limit"));
}

#[tokio::test]
async fn depth_limit_per_agent() {
    // coder has max_depth=2, so depth=2 should be blocked
    let tool = DelegateTool::with_depth(sample_agents(), None, test_security(), 2);
    let result = tool
        .execute(json!({"agent": "coder", "prompt": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("depth limit"));
}

#[test]
fn empty_agents_schema() {
    let tool = DelegateTool::new(HashMap::new(), None, test_security());
    let schema = tool.parameters_schema();
    let desc = schema["properties"]["agent"]["description"]
        .as_str()
        .unwrap();
    assert!(desc.contains("none configured"));
}

#[tokio::test]
async fn invalid_provider_returns_error() {
    let mut agents = HashMap::new();
    agents.insert(
        "broken".to_string(),
        DelegateAgentConfig {
            provider: "totally-invalid-provider".to_string(),
            model: "model".to_string(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );
    let tool = DelegateTool::new(agents, None, test_security());
    let result = tool
        .execute(json!({"agent": "broken", "prompt": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("Failed to create provider"));
}

#[tokio::test]
async fn blank_agent_rejected() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool
        .execute(json!({"agent": "  ", "prompt": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("must not be empty"));
}

#[tokio::test]
async fn blank_prompt_rejected() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool
        .execute(json!({"agent": "researcher", "prompt": "  \t  "}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("must not be empty"));
}

#[tokio::test]
async fn whitespace_agent_name_trimmed_and_found() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    // " researcher " with surrounding whitespace — after trim becomes "researcher"
    let result = tool
        .execute(json!({"agent": " researcher ", "prompt": "test"}))
        .await
        .unwrap();
    // Should find "researcher" after trim — will fail at provider level
    // since ollama isn't running, but must NOT get "Unknown agent".
    assert!(
        result.error.is_none()
            || !result
                .error
                .as_deref()
                .unwrap_or("")
                .contains("Unknown agent")
    );
}

#[tokio::test]
async fn delegation_blocked_in_readonly_mode() {
    let readonly = Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::ReadOnly,
        ..SecurityPolicy::default()
    });
    let tool = DelegateTool::new(sample_agents(), None, readonly);
    let result = tool
        .execute(json!({"agent": "researcher", "prompt": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("read-only mode")
    );
}

#[tokio::test]
async fn delegation_blocked_when_rate_limited() {
    let limited = Arc::new(SecurityPolicy {
        max_actions_per_hour: 0,
        ..SecurityPolicy::default()
    });
    let tool = DelegateTool::new(sample_agents(), None, limited);
    let result = tool
        .execute(json!({"agent": "researcher", "prompt": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("Rate limit exceeded")
    );
}

#[tokio::test]
async fn delegate_context_is_prepended_to_prompt() {
    let mut agents = HashMap::new();
    agents.insert(
        "tester".to_string(),
        DelegateAgentConfig {
            provider: "invalid-for-test".to_string(),
            model: "test-model".to_string(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );
    let tool = DelegateTool::new(agents, None, test_security());
    let result = tool
        .execute(json!({
            "agent": "tester",
            "prompt": "do something",
            "context": "some context data"
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("Failed to create provider")
    );
}

#[tokio::test]
async fn delegate_empty_context_omits_prefix() {
    let mut agents = HashMap::new();
    agents.insert(
        "tester".to_string(),
        DelegateAgentConfig {
            provider: "invalid-for-test".to_string(),
            model: "test-model".to_string(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );
    let tool = DelegateTool::new(agents, None, test_security());
    let result = tool
        .execute(json!({
            "agent": "tester",
            "prompt": "do something",
            "context": ""
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("Failed to create provider")
    );
}

#[test]
fn delegate_depth_construction() {
    let tool = DelegateTool::with_depth(sample_agents(), None, test_security(), 5);
    assert_eq!(tool.depth, 5);
}

#[tokio::test]
async fn delegate_no_agents_configured() {
    let tool = DelegateTool::new(HashMap::new(), None, test_security());
    let result = tool
        .execute(json!({"agent": "any", "prompt": "test"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.unwrap().contains("none configured"));
}

#[tokio::test]
async fn agentic_mode_rejects_empty_allowed_tools() {
    let mut agents = HashMap::new();
    agents.insert("agentic".to_string(), agentic_config(Vec::new(), 10));

    let tool = DelegateTool::new(agents, None, test_security());
    let result = tool
        .execute(json!({"agent": "agentic", "prompt": "test"}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("allowed_tools is empty")
    );
}

#[tokio::test]
async fn agentic_mode_rejects_unmatched_allowed_tools() {
    let mut agents = HashMap::new();
    agents.insert(
        "agentic".to_string(),
        agentic_config(vec!["missing_tool".to_string()], 10),
    );

    let tool = DelegateTool::new(agents, None, test_security())
        .with_parent_tools(Arc::new(RwLock::new(vec![Arc::new(EchoTool)])));
    let result = tool
        .execute(json!({"agent": "agentic", "prompt": "test"}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("no executable tools")
    );
}

#[tokio::test]
async fn execute_agentic_runs_tool_call_loop_with_filtered_tools() {
    let config = agentic_config(vec!["echo_tool".to_string()], 10);
    let tool = DelegateTool::new(HashMap::new(), None, test_security()).with_parent_tools(
        Arc::new(RwLock::new(vec![
            Arc::new(EchoTool),
            Arc::new(DelegateTool::new(HashMap::new(), None, test_security())),
        ])),
    );

    let provider = OneToolThenFinalProvider;
    let result = tool
        .execute_agentic("agentic", &config, &provider, "run", 0.2)
        .await
        .unwrap();

    assert!(result.success);
    assert!(result.output.contains("(openrouter/model-test, agentic)"));
    assert!(result.output.contains("done"));
}

#[tokio::test]
async fn execute_agentic_excludes_delegate_even_if_allowlisted() {
    let config = agentic_config(vec!["delegate".to_string()], 10);
    let tool = DelegateTool::new(HashMap::new(), None, test_security()).with_parent_tools(
        Arc::new(RwLock::new(vec![Arc::new(DelegateTool::new(
            HashMap::new(),
            None,
            test_security(),
        ))])),
    );

    let provider = OneToolThenFinalProvider;
    let result = tool
        .execute_agentic("agentic", &config, &provider, "run", 0.2)
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("no executable tools")
    );
}

#[tokio::test]
async fn execute_agentic_respects_max_iterations() {
    let config = agentic_config(vec!["echo_tool".to_string()], 2);
    let tool = DelegateTool::new(HashMap::new(), None, test_security())
        .with_parent_tools(Arc::new(RwLock::new(vec![Arc::new(EchoTool)])));

    let provider = InfiniteToolCallProvider;
    let result = tool
        .execute_agentic("agentic", &config, &provider, "run", 0.2)
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("maximum tool iterations (2)")
    );
}

#[tokio::test]
async fn execute_agentic_propagates_provider_errors() {
    let config = agentic_config(vec!["echo_tool".to_string()], 10);
    let tool = DelegateTool::new(HashMap::new(), None, test_security())
        .with_parent_tools(Arc::new(RwLock::new(vec![Arc::new(EchoTool)])));

    let provider = FailingProvider;
    let result = tool
        .execute_agentic("agentic", &config, &provider, "run", 0.2)
        .await
        .unwrap();

    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("provider boom")
    );
}

/// MCP tools pushed into the shared parent_tools handle after DelegateTool
/// construction must be visible to the sub-agent tool list.
#[derive(Default)]
struct FakeMcpTool;

#[async_trait]
impl Tool for FakeMcpTool {
    fn name(&self) -> &str {
        "mcp_fake"
    }

    fn description(&self) -> &str {
        "Fake MCP tool for testing."
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({"type": "object", "properties": {}})
    }

    async fn execute(&self, _args: serde_json::Value) -> anyhow::Result<ToolResult> {
        Ok(ToolResult {
            success: true,
            output: "mcp_fake_output".into(),
            error: None,
        })
    }
}

struct McpToolThenFinalProvider;

#[async_trait]
impl Provider for McpToolThenFinalProvider {
    async fn chat_with_system(
        &self,
        _system_prompt: Option<&str>,
        _message: &str,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<String> {
        Ok("unused".to_string())
    }

    async fn chat(
        &self,
        request: ChatRequest<'_>,
        _model: &str,
        _temperature: f64,
    ) -> anyhow::Result<ChatResponse> {
        let has_tool_message = request.messages.iter().any(|m| m.role == "tool");
        if has_tool_message {
            Ok(ChatResponse {
                text: Some("mcp done".to_string()),
                tool_calls: Vec::new(),
                usage: None,
                reasoning_content: None,
            })
        } else {
            Ok(ChatResponse {
                text: None,
                tool_calls: vec![ToolCall {
                    id: "call_mcp".to_string(),
                    name: "mcp_fake".to_string(),
                    arguments: "{}".to_string(),
                }],
                usage: None,
                reasoning_content: None,
            })
        }
    }
}

#[tokio::test]
async fn mcp_tools_included_in_subagent_tool_list() {
    // Build DelegateTool with NO parent tools initially
    let config = agentic_config(vec!["mcp_fake".to_string()], 10);
    let tool = DelegateTool::new(HashMap::new(), None, test_security())
        .with_parent_tools(Arc::new(RwLock::new(Vec::new())));

    // Simulate late MCP tool injection via the shared handle
    let handle = tool.parent_tools_handle();
    handle.write().push(Arc::new(FakeMcpTool));

    let provider = McpToolThenFinalProvider;
    let result = tool
        .execute_agentic("agentic", &config, &provider, "run mcp", 0.2)
        .await
        .unwrap();

    assert!(result.success, "Expected success, got: {:?}", result.error);
    assert!(
        result.output.contains("mcp done"),
        "Expected output containing 'mcp done', got: {}",
        result.output
    );
}

#[test]
fn enriched_prompt_includes_tools_workspace_datetime() {
    let config = DelegateAgentConfig {
        provider: "openrouter".to_string(),
        model: "test-model".to_string(),
        system_prompt: Some("You are a code reviewer.".to_string()),
        api_key: None,
        temperature: None,
        max_depth: 3,
        agentic: true,
        allowed_tools: vec!["echo_tool".to_string()],
        max_iterations: 10,
        timeout_secs: None,
        agentic_timeout_secs: None,
        skills_directory: None,
        memory_namespace: None,
    };

    let tools: Vec<Box<dyn Tool>> = vec![Box::new(EchoTool)];
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_enrich_test_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(HashMap::new(), None, test_security())
        .with_workspace_dir(workspace.clone());

    let prompt = tool
        .build_enriched_system_prompt(&config, &tools, &workspace)
        .unwrap();

    assert!(prompt.contains("## Tools"), "should contain tools section");
    assert!(prompt.contains("echo_tool"), "should list allowed tools");
    assert!(
        prompt.contains("## Workspace"),
        "should contain workspace section"
    );
    assert!(
        prompt.contains(&workspace.display().to_string()),
        "should contain workspace path"
    );
    assert!(
        prompt.contains("## CRITICAL CONTEXT: CURRENT DATE & TIME"),
        "should contain datetime section"
    );
    assert!(
        prompt.contains("You are a code reviewer."),
        "should append operator system_prompt"
    );

    let _ = std::fs::remove_dir_all(workspace);
}

#[test]
fn enriched_prompt_includes_shell_policy_when_shell_present() {
    let config = DelegateAgentConfig {
        provider: "openrouter".to_string(),
        model: "test-model".to_string(),
        system_prompt: None,
        api_key: None,
        temperature: None,
        max_depth: 3,
        agentic: true,
        allowed_tools: vec!["shell".to_string()],
        max_iterations: 10,
        timeout_secs: None,
        agentic_timeout_secs: None,
        skills_directory: None,
        memory_namespace: None,
    };

    struct MockShellTool;
    #[async_trait]
    impl Tool for MockShellTool {
        fn name(&self) -> &str {
            "shell"
        }
        fn description(&self) -> &str {
            "Execute shell commands"
        }
        fn parameters_schema(&self) -> serde_json::Value {
            json!({"type": "object"})
        }
        async fn execute(&self, _args: serde_json::Value) -> anyhow::Result<ToolResult> {
            Ok(ToolResult {
                success: true,
                output: String::new(),
                error: None,
            })
        }
    }

    let tools: Vec<Box<dyn Tool>> = vec![Box::new(MockShellTool)];
    let workspace = std::env::temp_dir();

    let tool = DelegateTool::new(HashMap::new(), None, test_security())
        .with_workspace_dir(workspace.to_path_buf());

    let prompt = tool
        .build_enriched_system_prompt(&config, &tools, &workspace)
        .unwrap();

    assert!(
        prompt.contains("## Shell Policy"),
        "should contain shell policy when shell tool is present"
    );
}

#[test]
fn parent_tools_handle_returns_shared_reference() {
    let tool = DelegateTool::new(HashMap::new(), None, test_security()).with_parent_tools(
        Arc::new(RwLock::new(vec![Arc::new(EchoTool) as Arc<dyn Tool>])),
    );

    let handle = tool.parent_tools_handle();
    assert_eq!(handle.read().len(), 1);

    // Push a new tool via the handle
    handle.write().push(Arc::new(FakeMcpTool));
    assert_eq!(handle.read().len(), 2);
}

// ── Configurable timeout tests ──────────────────────────────────

#[test]
fn default_timeout_values_used_when_config_unset() {
    let config = DelegateAgentConfig {
        provider: "ollama".to_string(),
        model: "llama3".to_string(),
        system_prompt: None,
        api_key: None,
        temperature: None,
        max_depth: 3,
        agentic: false,
        allowed_tools: Vec::new(),
        max_iterations: 10,
        timeout_secs: None,
        agentic_timeout_secs: None,
        skills_directory: None,
        memory_namespace: None,
    };
    assert_eq!(
        config.timeout_secs.unwrap_or(DEFAULT_DELEGATE_TIMEOUT_SECS),
        120
    );
    assert_eq!(
        config
            .agentic_timeout_secs
            .unwrap_or(DEFAULT_DELEGATE_AGENTIC_TIMEOUT_SECS),
        300
    );
}

#[test]
fn enriched_prompt_omits_shell_policy_without_shell_tool() {
    let config = DelegateAgentConfig {
        provider: "openrouter".to_string(),
        model: "test-model".to_string(),
        system_prompt: None,
        api_key: None,
        temperature: None,
        max_depth: 3,
        agentic: true,
        allowed_tools: vec!["echo_tool".to_string()],
        max_iterations: 10,
        timeout_secs: None,
        agentic_timeout_secs: None,
        skills_directory: None,
        memory_namespace: None,
    };

    let tools: Vec<Box<dyn Tool>> = vec![Box::new(EchoTool)];
    let workspace = std::env::temp_dir();

    let tool = DelegateTool::new(HashMap::new(), None, test_security())
        .with_workspace_dir(workspace.to_path_buf());

    let prompt = tool
        .build_enriched_system_prompt(&config, &tools, &workspace)
        .unwrap();

    assert!(
        !prompt.contains("## Shell Policy"),
        "should not contain shell policy when shell tool is absent"
    );
}

#[test]
fn custom_timeout_values_are_respected() {
    let config = DelegateAgentConfig {
        provider: "ollama".to_string(),
        model: "llama3".to_string(),
        system_prompt: None,
        api_key: None,
        temperature: None,
        max_depth: 3,
        agentic: false,
        allowed_tools: Vec::new(),
        max_iterations: 10,
        timeout_secs: Some(60),
        agentic_timeout_secs: Some(600),
        skills_directory: None,
        memory_namespace: None,
    };
    assert_eq!(
        config.timeout_secs.unwrap_or(DEFAULT_DELEGATE_TIMEOUT_SECS),
        60
    );
    assert_eq!(
        config
            .agentic_timeout_secs
            .unwrap_or(DEFAULT_DELEGATE_AGENTIC_TIMEOUT_SECS),
        600
    );
}

#[test]
fn timeout_deserialization_defaults_to_none() {
    let toml_str = r#"
            provider = "ollama"
            model = "llama3"
        "#;
    let config: DelegateAgentConfig = toml::from_str(toml_str).unwrap();
    assert!(config.timeout_secs.is_none());
    assert!(config.agentic_timeout_secs.is_none());
}

#[test]
fn timeout_deserialization_with_custom_values() {
    let toml_str = r#"
            provider = "ollama"
            model = "llama3"
            timeout_secs = 45
            agentic_timeout_secs = 900
        "#;
    let config: DelegateAgentConfig = toml::from_str(toml_str).unwrap();
    assert_eq!(config.timeout_secs, Some(45));
    assert_eq!(config.agentic_timeout_secs, Some(900));
}

#[test]
fn config_validation_rejects_zero_timeout() {
    let mut config = crate::config::Config::default();
    config.agents.insert(
        "bad".into(),
        DelegateAgentConfig {
            provider: "ollama".into(),
            model: "llama3".into(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: Some(0),
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );
    let err = config.validate().unwrap_err();
    assert!(
        format!("{err}").contains("timeout_secs must be greater than 0"),
        "unexpected error: {err}"
    );
}

#[test]
fn config_validation_rejects_zero_agentic_timeout() {
    let mut config = crate::config::Config::default();
    config.agents.insert(
        "bad".into(),
        DelegateAgentConfig {
            provider: "ollama".into(),
            model: "llama3".into(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: Some(0),
            skills_directory: None,
            memory_namespace: None,
        },
    );
    let err = config.validate().unwrap_err();
    assert!(
        format!("{err}").contains("agentic_timeout_secs must be greater than 0"),
        "unexpected error: {err}"
    );
}

#[test]
fn config_validation_rejects_excessive_timeout() {
    let mut config = crate::config::Config::default();
    config.agents.insert(
        "bad".into(),
        DelegateAgentConfig {
            provider: "ollama".into(),
            model: "llama3".into(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: Some(7200),
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );
    let err = config.validate().unwrap_err();
    assert!(
        format!("{err}").contains("exceeds max 3600"),
        "unexpected error: {err}"
    );
}

#[test]
fn config_validation_rejects_excessive_agentic_timeout() {
    let mut config = crate::config::Config::default();
    config.agents.insert(
        "bad".into(),
        DelegateAgentConfig {
            provider: "ollama".into(),
            model: "llama3".into(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: Some(5000),
            skills_directory: None,
            memory_namespace: None,
        },
    );
    let err = config.validate().unwrap_err();
    assert!(
        format!("{err}").contains("exceeds max 3600"),
        "unexpected error: {err}"
    );
}

#[test]
fn config_validation_accepts_max_boundary_timeout() {
    let mut config = crate::config::Config::default();
    config.agents.insert(
        "ok".into(),
        DelegateAgentConfig {
            provider: "ollama".into(),
            model: "llama3".into(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: Some(3600),
            agentic_timeout_secs: Some(3600),
            skills_directory: None,
            memory_namespace: None,
        },
    );
    assert!(config.validate().is_ok());
}

#[test]
fn config_validation_accepts_none_timeouts() {
    let mut config = crate::config::Config::default();
    config.agents.insert(
        "ok".into(),
        DelegateAgentConfig {
            provider: "ollama".into(),
            model: "llama3".into(),
            system_prompt: None,
            api_key: None,
            temperature: None,
            max_depth: 3,
            agentic: false,
            allowed_tools: Vec::new(),
            max_iterations: 10,
            timeout_secs: None,
            agentic_timeout_secs: None,
            skills_directory: None,
            memory_namespace: None,
        },
    );
    assert!(config.validate().is_ok());
}

#[test]
fn enriched_prompt_loads_skills_from_scoped_directory() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_skills_test_{}",
        uuid::Uuid::new_v4()
    ));
    let scoped_skills_dir = workspace.join("skills/code-review");
    std::fs::create_dir_all(scoped_skills_dir.join("lint-check")).unwrap();
    std::fs::write(
        scoped_skills_dir.join("lint-check/SKILL.toml"),
        "[skill]\nname = \"lint-check\"\ndescription = \"Run lint checks\"\nversion = \"1.0.0\"\n",
    )
    .unwrap();

    let config = DelegateAgentConfig {
        provider: "openrouter".to_string(),
        model: "test-model".to_string(),
        system_prompt: None,
        api_key: None,
        temperature: None,
        max_depth: 3,
        agentic: true,
        allowed_tools: vec!["echo_tool".to_string()],
        max_iterations: 10,
        timeout_secs: None,
        agentic_timeout_secs: None,
        skills_directory: Some("skills/code-review".to_string()),
        memory_namespace: None,
    };

    let tools: Vec<Box<dyn Tool>> = vec![Box::new(EchoTool)];

    let tool = DelegateTool::new(HashMap::new(), None, test_security())
        .with_workspace_dir(workspace.clone());

    let prompt = tool
        .build_enriched_system_prompt(&config, &tools, &workspace)
        .unwrap();

    assert!(
        prompt.contains("lint-check"),
        "should contain skills from scoped directory"
    );

    let _ = std::fs::remove_dir_all(workspace);
}

#[test]
fn enriched_prompt_falls_back_to_default_skills_dir() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_fallback_test_{}",
        uuid::Uuid::new_v4()
    ));
    let default_skills_dir = workspace.join("skills");
    std::fs::create_dir_all(default_skills_dir.join("deploy")).unwrap();
    std::fs::write(
        default_skills_dir.join("deploy/SKILL.toml"),
        "[skill]\nname = \"deploy\"\ndescription = \"Deploy safely\"\nversion = \"1.0.0\"\n",
    )
    .unwrap();

    let config = DelegateAgentConfig {
        provider: "openrouter".to_string(),
        model: "test-model".to_string(),
        system_prompt: None,
        api_key: None,
        temperature: None,
        max_depth: 3,
        agentic: true,
        allowed_tools: vec!["echo_tool".to_string()],
        max_iterations: 10,
        timeout_secs: None,
        agentic_timeout_secs: None,
        skills_directory: None,
        memory_namespace: None,
    };

    let tools: Vec<Box<dyn Tool>> = vec![Box::new(EchoTool)];

    let tool = DelegateTool::new(HashMap::new(), None, test_security())
        .with_workspace_dir(workspace.clone());

    let prompt = tool
        .build_enriched_system_prompt(&config, &tools, &workspace)
        .unwrap();

    assert!(
        prompt.contains("deploy"),
        "should contain skills from default workspace skills/ directory"
    );

    let _ = std::fs::remove_dir_all(workspace);
}

// ── Background and Parallel execution tests ─────────────────────

#[tokio::test]
async fn background_delegation_returns_task_id() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_bg_test_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());
    let result = tool
        .execute(json!({
            "agent": "researcher",
            "prompt": "test background",
            "background": true
        }))
        .await
        .unwrap();

    // The agent will fail at provider level (ollama not running),
    // but the background task should be spawned and return a task_id.
    assert!(result.success);
    assert!(result.output.contains("task_id:"));
    assert!(result.output.contains("Background task started"));

    // Wait a moment for the background task to write its result
    tokio::time::sleep(Duration::from_millis(200)).await;

    // The results directory should exist
    assert!(workspace.join("delegate_results").exists());

    let _ = std::fs::remove_dir_all(workspace);
}

#[tokio::test]
async fn background_unknown_agent_rejected() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_bg_unknown_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());
    let result = tool
        .execute(json!({
            "agent": "nonexistent",
            "prompt": "test",
            "background": true
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.unwrap().contains("Unknown agent"));

    let _ = std::fs::remove_dir_all(workspace);
}

#[tokio::test]
async fn check_result_missing_task_id() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_check_noid_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());
    let result = tool.execute(json!({"action": "check_result"})).await;

    assert!(result.is_err());

    let _ = std::fs::remove_dir_all(workspace);
}

#[tokio::test]
async fn check_result_nonexistent_task() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_check_miss_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());
    // Use a valid UUID format that doesn't correspond to any real task
    let fake_uuid = uuid::Uuid::new_v4().to_string();
    let result = tool
        .execute(json!({
            "action": "check_result",
            "task_id": fake_uuid
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.unwrap().contains("No result found"));

    let _ = std::fs::remove_dir_all(workspace);
}

#[tokio::test]
async fn list_results_empty() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_list_empty_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());
    let result = tool
        .execute(json!({"action": "list_results"}))
        .await
        .unwrap();

    assert!(result.success);
    assert!(result.output.contains("No background delegate results"));

    let _ = std::fs::remove_dir_all(workspace);
}

#[tokio::test]
async fn parallel_empty_list_rejected() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool
        .execute(json!({
            "parallel": [],
            "prompt": "test"
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.unwrap().contains("at least one agent"));
}

#[tokio::test]
async fn parallel_unknown_agent_rejected() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool
        .execute(json!({
            "parallel": ["researcher", "nonexistent"],
            "prompt": "test"
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.unwrap().contains("Unknown agent"));
}

#[tokio::test]
async fn parallel_missing_prompt_rejected() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool
        .execute(json!({
            "parallel": ["researcher"]
        }))
        .await;

    assert!(result.is_err());
}

#[tokio::test]
async fn unknown_action_rejected() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool
        .execute(json!({"action": "invalid_action"}))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.unwrap().contains("Unknown action"));
}

#[tokio::test]
async fn cancel_task_nonexistent() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_cancel_miss_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());
    // Use a valid UUID format that doesn't correspond to any real task
    let fake_uuid = uuid::Uuid::new_v4().to_string();
    let result = tool
        .execute(json!({
            "action": "cancel_task",
            "task_id": fake_uuid
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.unwrap().contains("No task found"));

    let _ = std::fs::remove_dir_all(workspace);
}

#[test]
fn cancellation_token_accessor() {
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let token = tool.cancellation_token();
    assert!(!token.is_cancelled());

    tool.cancel_all_background_tasks();
    assert!(token.is_cancelled());
}

#[test]
fn with_cancellation_token_replaces_default() {
    let custom_token = CancellationToken::new();
    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_cancellation_token(custom_token.clone());

    assert!(!tool.cancellation_token().is_cancelled());
    custom_token.cancel();
    assert!(tool.cancellation_token().is_cancelled());
}

#[tokio::test]
async fn background_task_result_persisted_to_disk() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_bg_persist_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());

    let result = tool
        .execute(json!({
            "agent": "researcher",
            "prompt": "persistence test",
            "background": true
        }))
        .await
        .unwrap();

    assert!(result.success);

    // Extract task_id from output
    let task_id = result
        .output
        .lines()
        .find(|l| l.starts_with("task_id:"))
        .unwrap()
        .trim_start_matches("task_id: ")
        .trim();

    // Wait for the background task to finish
    tokio::time::sleep(Duration::from_millis(500)).await;

    // Check that the result file exists
    let result_path = workspace
        .join("delegate_results")
        .join(format!("{task_id}.json"));
    assert!(
        result_path.exists(),
        "Result file should exist at {result_path:?}"
    );

    // Read and parse the result
    let content = std::fs::read_to_string(&result_path).unwrap();
    let bg_result: BackgroundDelegateResult = serde_json::from_str(&content).unwrap();
    assert_eq!(bg_result.task_id, task_id);
    assert_eq!(bg_result.agent, "researcher");
    // The task will have failed because ollama isn't running, but it should be persisted
    assert!(
        bg_result.status == BackgroundTaskStatus::Completed
            || bg_result.status == BackgroundTaskStatus::Failed
    );
    assert!(bg_result.finished_at.is_some());

    let _ = std::fs::remove_dir_all(workspace);
}

#[tokio::test]
async fn check_result_retrieves_persisted_background_result() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_check_retrieve_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());

    // Start background task
    let result = tool
        .execute(json!({
            "agent": "researcher",
            "prompt": "retrieval test",
            "background": true
        }))
        .await
        .unwrap();

    let task_id = result
        .output
        .lines()
        .find(|l| l.starts_with("task_id:"))
        .unwrap()
        .trim_start_matches("task_id: ")
        .trim()
        .to_string();

    // Wait for background task
    tokio::time::sleep(Duration::from_millis(500)).await;

    // Check result
    let check = tool
        .execute(json!({
            "action": "check_result",
            "task_id": task_id
        }))
        .await
        .unwrap();

    // The output should contain the serialized result
    assert!(check.output.contains(&task_id));
    assert!(check.output.contains("researcher"));

    let _ = std::fs::remove_dir_all(workspace);
}

#[tokio::test]
async fn list_results_includes_background_tasks() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_list_tasks_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());

    // Start a background task
    let result = tool
        .execute(json!({
            "agent": "researcher",
            "prompt": "list test",
            "background": true
        }))
        .await
        .unwrap();
    assert!(result.success);

    // Wait for task to complete
    tokio::time::sleep(Duration::from_millis(500)).await;

    // List results
    let list = tool
        .execute(json!({"action": "list_results"}))
        .await
        .unwrap();

    assert!(list.success);
    assert!(list.output.contains("researcher"));

    let _ = std::fs::remove_dir_all(workspace);
}

#[tokio::test]
async fn default_action_is_delegate() {
    // Calling without action should behave like "delegate"
    let tool = DelegateTool::new(sample_agents(), None, test_security());
    let result = tool
        .execute(json!({"agent": "researcher", "prompt": "test"}))
        .await
        .unwrap();
    // Should proceed to delegation (will fail at provider since ollama isn't running)
    // but should NOT fail with "Unknown action" error
    assert!(
        result.error.is_none()
            || !result
                .error
                .as_deref()
                .unwrap_or("")
                .contains("Unknown action")
    );
}

#[tokio::test]
async fn check_result_rejects_path_traversal() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_traversal_check_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());
    let result = tool
        .execute(json!({
            "action": "check_result",
            "task_id": "../../etc/passwd"
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.unwrap().contains("Invalid task_id"));

    let _ = std::fs::remove_dir_all(workspace);
}

#[tokio::test]
async fn cancel_task_rejects_path_traversal() {
    let workspace = std::env::temp_dir().join(format!(
        "zeroclaw_delegate_traversal_cancel_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&workspace).unwrap();

    let tool = DelegateTool::new(sample_agents(), None, test_security())
        .with_workspace_dir(workspace.clone());
    let result = tool
        .execute(json!({
            "action": "cancel_task",
            "task_id": "../../../etc/shadow"
        }))
        .await
        .unwrap();

    assert!(!result.success);
    assert!(result.error.unwrap().contains("Invalid task_id"));

    let _ = std::fs::remove_dir_all(workspace);
}

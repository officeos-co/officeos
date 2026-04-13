use super::*;
use crate::config::{BrowserConfig, Config, MemoryConfig};
use tempfile::TempDir;

fn test_config(tmp: &TempDir) -> Config {
    Config {
        workspace_dir: tmp.path().join("workspace"),
        config_path: tmp.path().join("config.toml"),
        ..Config::default()
    }
}

#[test]
fn default_tools_has_expected_count() {
    let security = Arc::new(SecurityPolicy::default());
    let tools = default_tools(security);
    assert_eq!(tools.len(), 6);
}

#[test]
fn default_tools_names() {
    let security = Arc::new(SecurityPolicy::default());
    let tools = default_tools(security);
    let names: Vec<&str> = tools.iter().map(|t| t.name()).collect();
    assert!(names.contains(&"shell"));
    assert!(names.contains(&"file_read"));
    assert!(names.contains(&"file_write"));
    assert!(names.contains(&"file_edit"));
    assert!(names.contains(&"glob_search"));
    assert!(names.contains(&"content_search"));
}

#[test]
fn default_tools_all_have_descriptions() {
    let security = Arc::new(SecurityPolicy::default());
    let tools = default_tools(security);
    for tool in &tools {
        assert!(
            !tool.description().is_empty(),
            "Tool {} has empty description",
            tool.name()
        );
    }
}

#[test]
fn default_tools_all_have_schemas() {
    let security = Arc::new(SecurityPolicy::default());
    let tools = default_tools(security);
    for tool in &tools {
        let schema = tool.parameters_schema();
        assert!(
            schema.is_object(),
            "Tool {} schema is not an object",
            tool.name()
        );
        assert!(
            schema["properties"].is_object(),
            "Tool {} schema has no properties",
            tool.name()
        );
    }
}

#[test]
fn tool_spec_generation() {
    let security = Arc::new(SecurityPolicy::default());
    let tools = default_tools(security);
    for tool in &tools {
        let spec = tool.spec();
        assert_eq!(spec.name, tool.name());
        assert_eq!(spec.description, tool.description());
        assert!(spec.parameters.is_object());
    }
}

#[test]
fn tool_result_serde() {
    let result = ToolResult {
        success: true,
        output: "hello".into(),
        error: None,
    };
    let json = serde_json::to_string(&result).unwrap();
    let parsed: ToolResult = serde_json::from_str(&json).unwrap();
    assert!(parsed.success);
    assert_eq!(parsed.output, "hello");
    assert!(parsed.error.is_none());
}

#[test]
fn tool_result_with_error_serde() {
    let result = ToolResult {
        success: false,
        output: String::new(),
        error: Some("boom".into()),
    };
    let json = serde_json::to_string(&result).unwrap();
    let parsed: ToolResult = serde_json::from_str(&json).unwrap();
    assert!(!parsed.success);
    assert_eq!(parsed.error.as_deref(), Some("boom"));
}

#[test]
fn tool_spec_serde() {
    let spec = ToolSpec {
        name: "test".into(),
        description: "A test tool".into(),
        parameters: serde_json::json!({"type": "object"}),
    };
    let json = serde_json::to_string(&spec).unwrap();
    let parsed: ToolSpec = serde_json::from_str(&json).unwrap();
    assert_eq!(parsed.name, "test");
    assert_eq!(parsed.description, "A test tool");
}

#[test]
fn all_tools_includes_delegate_when_agents_configured() {
    let tmp = TempDir::new().unwrap();
    let security = Arc::new(SecurityPolicy::default());
    let mem_cfg = MemoryConfig {
        backend: "markdown".into(),
        ..MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> =
        Arc::from(crate::memory::create_memory(&mem_cfg, tmp.path(), None).unwrap());

    let browser = BrowserConfig::default();
    let http = crate::config::HttpRequestConfig::default();
    let cfg = test_config(&tmp);

    let mut agents = HashMap::new();
    agents.insert(
        "researcher".to_string(),
        DelegateAgentConfig {
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
        },
    );

    let (tools, _, _, _, _, _) = all_tools(
        Arc::new(Config::default()),
        &security,
        mem,
        None,
        None,
        &browser,
        &http,
        &crate::config::WebFetchConfig::default(),
        tmp.path(),
        &agents,
        Some("delegate-test-credential"),
        &cfg,
        None,
    );
    let names: Vec<&str> = tools.iter().map(|t| t.name()).collect();
    assert!(names.contains(&"delegate"));
}

#[test]
fn all_tools_excludes_delegate_when_no_agents() {
    let tmp = TempDir::new().unwrap();
    let security = Arc::new(SecurityPolicy::default());
    let mem_cfg = MemoryConfig {
        backend: "markdown".into(),
        ..MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> =
        Arc::from(crate::memory::create_memory(&mem_cfg, tmp.path(), None).unwrap());

    let browser = BrowserConfig::default();
    let http = crate::config::HttpRequestConfig::default();
    let cfg = test_config(&tmp);

    let (tools, _, _, _, _, _) = all_tools(
        Arc::new(Config::default()),
        &security,
        mem,
        None,
        None,
        &browser,
        &http,
        &crate::config::WebFetchConfig::default(),
        tmp.path(),
        &HashMap::new(),
        None,
        &cfg,
        None,
    );
    let names: Vec<&str> = tools.iter().map(|t| t.name()).collect();
    assert!(!names.contains(&"delegate"));
}

#[test]
fn all_tools_includes_read_skill_in_compact_mode() {
    let tmp = TempDir::new().unwrap();
    let security = Arc::new(SecurityPolicy::default());
    let mem_cfg = MemoryConfig {
        backend: "markdown".into(),
        ..MemoryConfig::default()
    };
    let mem: Arc<dyn Memory> =
        Arc::from(crate::memory::create_memory(&mem_cfg, tmp.path(), None).unwrap());

    let browser = BrowserConfig::default();
    let http = crate::config::HttpRequestConfig::default();
    let cfg = test_config(&tmp);

    let (tools, _, _, _, _, _) = all_tools(
        Arc::new(cfg.clone()),
        &security,
        mem,
        None,
        None,
        &browser,
        &http,
        &crate::config::WebFetchConfig::default(),
        tmp.path(),
        &HashMap::new(),
        None,
        &cfg,
        None,
    );
    let names: Vec<&str> = tools.iter().map(|t| t.name()).collect();
    assert!(
        names.contains(&"read_skill"),
        "read_skill must always be registered for on-demand skill loading"
    );
}

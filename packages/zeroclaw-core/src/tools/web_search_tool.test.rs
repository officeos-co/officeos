use super::*;

#[test]
fn test_tool_name() {
    let tool = WebSearchTool::new("duckduckgo".to_string(), None, 5, 15);
    assert_eq!(tool.name(), "web_search_tool");
}

#[test]
fn test_tool_description() {
    let tool = WebSearchTool::new("duckduckgo".to_string(), None, 5, 15);
    assert!(tool.description().contains("Search the web"));
}

#[test]
fn test_parameters_schema() {
    let tool = WebSearchTool::new("duckduckgo".to_string(), None, 5, 15);
    let schema = tool.parameters_schema();
    assert_eq!(schema["type"], "object");
    assert!(schema["properties"]["query"].is_object());
}

#[test]
fn test_strip_tags() {
    let html = "<b>Hello</b> <i>World</i>";
    assert_eq!(strip_tags(html), "Hello World");
}

#[test]
fn test_parse_duckduckgo_results_empty() {
    let tool = WebSearchTool::new("duckduckgo".to_string(), None, 5, 15);
    let result = tool
        .parse_duckduckgo_results("<html>No results here</html>", "test")
        .unwrap();
    assert!(result.contains("No results found"));
}

#[test]
fn test_parse_duckduckgo_results_with_data() {
    let tool = WebSearchTool::new("duckduckgo".to_string(), None, 5, 15);
    let html = r#"
            <a class="result__a" href="https://example.com">Example Title</a>
            <a class="result__snippet">This is a description</a>
        "#;
    let result = tool.parse_duckduckgo_results(html, "test").unwrap();
    assert!(result.contains("Example Title"));
    assert!(result.contains("https://example.com"));
}

#[test]
fn test_parse_duckduckgo_results_decodes_redirect_url() {
    let tool = WebSearchTool::new("duckduckgo".to_string(), None, 5, 15);
    let html = r#"
            <a class="result__a" href="https://duckduckgo.com/l/?uddg=https%3A%2F%2Fexample.com%2Fpath%3Fa%3D1&amp;rut=test">Example Title</a>
            <a class="result__snippet">This is a description</a>
        "#;
    let result = tool.parse_duckduckgo_results(html, "test").unwrap();
    assert!(result.contains("https://example.com/path?a=1"));
    assert!(!result.contains("rut=test"));
}

#[test]
fn test_constructor_clamps_web_search_limits() {
    let tool = WebSearchTool::new("duckduckgo".to_string(), None, 0, 0);
    let html = r#"
            <a class="result__a" href="https://example.com">Example Title</a>
            <a class="result__snippet">This is a description</a>
        "#;
    let result = tool.parse_duckduckgo_results(html, "test").unwrap();
    assert!(result.contains("Example Title"));
}

#[tokio::test]
async fn test_execute_missing_query() {
    let tool = WebSearchTool::new("duckduckgo".to_string(), None, 5, 15);
    let result = tool.execute(json!({})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn test_execute_empty_query() {
    let tool = WebSearchTool::new("duckduckgo".to_string(), None, 5, 15);
    let result = tool.execute(json!({"query": ""})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn test_execute_brave_without_api_key() {
    let tool = WebSearchTool::new("brave".to_string(), None, 5, 15);
    let result = tool.execute(json!({"query": "test"})).await;
    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("API key"));
}

#[test]
fn test_resolve_brave_api_key_uses_boot_key() {
    let tool = WebSearchTool::new(
        "brave".to_string(),
        Some("sk-plaintext-key".to_string()),
        5,
        15,
    );
    let key = tool.resolve_brave_api_key().unwrap();
    assert_eq!(key, "sk-plaintext-key");
}

#[test]
fn test_resolve_brave_api_key_reloads_from_config() {
    let tmp = tempfile::TempDir::new().unwrap();
    let config_path = tmp.path().join("config.toml");
    std::fs::write(
        &config_path,
        "[web_search]\nbrave_api_key = \"fresh-key-from-disk\"\n",
    )
    .unwrap();

    // No boot key -- forces reload from config
    let tool =
        WebSearchTool::new_with_config("brave".to_string(), None, None, 5, 15, config_path, false);
    let key = tool.resolve_brave_api_key().unwrap();
    assert_eq!(key, "fresh-key-from-disk");
}

#[test]
fn test_resolve_brave_api_key_decrypts_encrypted_key() {
    let tmp = tempfile::TempDir::new().unwrap();
    let store = crate::security::SecretStore::new(tmp.path(), true);
    let encrypted = store.encrypt("brave-secret-key").unwrap();

    let config_path = tmp.path().join("config.toml");
    std::fs::write(
        &config_path,
        format!("[web_search]\nbrave_api_key = \"{}\"\n", encrypted),
    )
    .unwrap();

    // Boot key is the encrypted blob -- should trigger reload + decrypt
    let tool = WebSearchTool::new_with_config(
        "brave".to_string(),
        Some(encrypted),
        None,
        5,
        15,
        config_path,
        true,
    );
    let key = tool.resolve_brave_api_key().unwrap();
    assert_eq!(key, "brave-secret-key");
}

#[tokio::test]
async fn test_execute_searxng_without_instance_url() {
    let tmp = tempfile::TempDir::new().unwrap();
    let config_path = tmp.path().join("config.toml");
    std::fs::write(&config_path, "[web_search]\n").unwrap();

    let tool = WebSearchTool::new_with_config(
        "searxng".to_string(),
        None,
        None,
        5,
        15,
        config_path,
        false,
    );
    let result = tool.execute(json!({"query": "test"})).await;
    assert!(result.is_err());
    assert!(
        result
            .unwrap_err()
            .to_string()
            .contains("SearXNG instance URL not configured")
    );
}

#[test]
fn test_parse_searxng_results_empty() {
    let tool = WebSearchTool::new("searxng".to_string(), None, 5, 15);
    let json = serde_json::json!({"results": []});
    let result = tool.parse_searxng_results(&json, "test").unwrap();
    assert!(result.contains("No results found"));
}

#[test]
fn test_parse_searxng_results_with_data() {
    let tool = WebSearchTool::new("searxng".to_string(), None, 5, 15);
    let json = serde_json::json!({
        "results": [
            {
                "title": "SearXNG Example",
                "url": "https://example.com",
                "content": "A privacy-respecting metasearch engine"
            },
            {
                "title": "Another Result",
                "url": "https://example.org",
                "content": "More information here"
            }
        ]
    });
    let result = tool.parse_searxng_results(&json, "test").unwrap();
    assert!(result.contains("SearXNG Example"));
    assert!(result.contains("https://example.com"));
    assert!(result.contains("A privacy-respecting metasearch engine"));
    assert!(result.contains("via SearXNG"));
}

#[test]
fn test_parse_searxng_results_invalid_response() {
    let tool = WebSearchTool::new("searxng".to_string(), None, 5, 15);
    let json = serde_json::json!({"error": "bad request"});
    let result = tool.parse_searxng_results(&json, "test");
    assert!(result.is_err());
    assert!(
        result
            .unwrap_err()
            .to_string()
            .contains("Invalid SearXNG API response")
    );
}

#[test]
fn test_resolve_searxng_instance_url_from_boot() {
    let tool = WebSearchTool {
        provider: "searxng".to_string(),
        boot_brave_api_key: None,
        searxng_instance_url: Some("https://searx.example.com".to_string()),
        max_results: 5,
        timeout_secs: 15,
        config_path: PathBuf::new(),
        secrets_encrypt: false,
    };
    let url = tool.resolve_searxng_instance_url().unwrap();
    assert_eq!(url, "https://searx.example.com");
}

#[test]
fn test_resolve_searxng_instance_url_reloads_from_config() {
    let tmp = tempfile::TempDir::new().unwrap();
    let config_path = tmp.path().join("config.toml");
    std::fs::write(
        &config_path,
        "[web_search]\nsearxng_instance_url = \"https://search.local\"\n",
    )
    .unwrap();

    let tool = WebSearchTool::new_with_config(
        "searxng".to_string(),
        None,
        None,
        5,
        15,
        config_path,
        false,
    );
    let url = tool.resolve_searxng_instance_url().unwrap();
    assert_eq!(url, "https://search.local");
}

#[test]
fn test_resolve_brave_api_key_picks_up_runtime_update() {
    let tmp = tempfile::TempDir::new().unwrap();
    let config_path = tmp.path().join("config.toml");

    // Start with no key in config
    std::fs::write(&config_path, "[web_search]\n").unwrap();

    let tool = WebSearchTool::new_with_config(
        "brave".to_string(),
        None,
        None,
        5,
        15,
        config_path.clone(),
        false,
    );

    // Key not configured yet -- should fail
    assert!(tool.resolve_brave_api_key().is_err());

    // Simulate runtime config update (e.g. via web_search_config set)
    std::fs::write(
        &config_path,
        "[web_search]\nbrave_api_key = \"runtime-updated-key\"\n",
    )
    .unwrap();

    // Now should succeed with the updated key
    let key = tool.resolve_brave_api_key().unwrap();
    assert_eq!(key, "runtime-updated-key");
}

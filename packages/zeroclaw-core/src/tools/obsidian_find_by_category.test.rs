use super::*;

fn make_tool() -> ObsidianFindByCategoryTool {
    ObsidianFindByCategoryTool::new("http://localhost:3001")
}

#[test]
fn name_is_correct() {
    assert_eq!(make_tool().name(), "obsidian_find_by_category");
}

#[test]
fn description_is_non_empty() {
    assert!(!make_tool().description().is_empty());
}

#[test]
fn schema_requires_category() {
    let schema = make_tool().parameters_schema();
    assert_eq!(schema["type"], "object");
    assert!(schema["properties"]["category"].is_object());
    let required = schema["required"].as_array().unwrap();
    assert!(required.iter().any(|v| v.as_str() == Some("category")));
}

#[test]
fn schema_has_optional_limit() {
    let schema = make_tool().parameters_schema();
    assert!(schema["properties"]["limit"].is_object());
    // limit is not in required
    let required = schema["required"].as_array().unwrap();
    assert!(!required.iter().any(|v| v.as_str() == Some("limit")));
}

#[test]
fn endpoint_uses_skill_runtime_url() {
    let tool = ObsidianFindByCategoryTool::new("http://skill-runtime:3001");
    assert_eq!(
        tool.endpoint(),
        "http://skill-runtime:3001/skills/obsidian/execute"
    );
}

#[tokio::test]
async fn execute_returns_error_when_category_missing() {
    let tool = make_tool();
    let result = tool.execute(serde_json::json!({})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn execute_fails_gracefully_when_runtime_unreachable() {
    let tool = ObsidianFindByCategoryTool::new("http://127.0.0.1:19999");
    let result = tool
        .execute(serde_json::json!({"category": "project"}))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.is_some());
}

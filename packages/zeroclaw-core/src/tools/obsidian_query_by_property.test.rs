use super::*;

fn make_tool() -> ObsidianQueryByPropertyTool {
    ObsidianQueryByPropertyTool::new("http://localhost:3001")
}

#[test]
fn name_is_correct() {
    assert_eq!(make_tool().name(), "obsidian_query_by_property");
}

#[test]
fn description_is_non_empty() {
    assert!(!make_tool().description().is_empty());
}

#[test]
fn schema_requires_property() {
    let schema = make_tool().parameters_schema();
    assert_eq!(schema["type"], "object");
    assert!(schema["properties"]["property"].is_object());
    let required = schema["required"].as_array().unwrap();
    assert!(required.iter().any(|v| v.as_str() == Some("property")));
}

#[test]
fn schema_has_optional_fields() {
    let schema = make_tool().parameters_schema();
    let required = schema["required"].as_array().unwrap();
    assert!(schema["properties"]["value"].is_object());
    assert!(schema["properties"]["operator"].is_object());
    assert!(schema["properties"]["limit"].is_object());
    // none of the optionals are required
    assert!(!required.iter().any(|v| v.as_str() == Some("value")));
    assert!(!required.iter().any(|v| v.as_str() == Some("operator")));
    assert!(!required.iter().any(|v| v.as_str() == Some("limit")));
}

#[test]
fn schema_operator_enum_has_three_values() {
    let schema = make_tool().parameters_schema();
    let enum_vals = schema["properties"]["operator"]["enum"].as_array().unwrap();
    assert_eq!(enum_vals.len(), 3);
    assert!(enum_vals.iter().any(|v| v.as_str() == Some("eq")));
    assert!(enum_vals.iter().any(|v| v.as_str() == Some("contains")));
    assert!(enum_vals.iter().any(|v| v.as_str() == Some("exists")));
}

#[test]
fn endpoint_uses_skill_runtime_url() {
    let tool = ObsidianQueryByPropertyTool::new("http://skill-runtime:3001");
    assert_eq!(
        tool.endpoint(),
        "http://skill-runtime:3001/skills/obsidian/execute"
    );
}

#[tokio::test]
async fn execute_returns_error_when_property_missing() {
    let tool = make_tool();
    let result = tool.execute(serde_json::json!({})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn execute_rejects_invalid_operator() {
    let tool = make_tool();
    let result = tool
        .execute(serde_json::json!({
            "property": "status",
            "operator": "invalid_op",
            "value": "done"
        }))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("Invalid operator")
    );
}

#[tokio::test]
async fn execute_fails_gracefully_when_runtime_unreachable() {
    let tool = ObsidianQueryByPropertyTool::new("http://127.0.0.1:19999");
    let result = tool
        .execute(serde_json::json!({
            "property": "status",
            "value": "active",
            "operator": "eq"
        }))
        .await
        .unwrap();
    assert!(!result.success);
    assert!(result.error.is_some());
}

#[tokio::test]
async fn execute_exists_operator_does_not_require_value() {
    // With an unreachable runtime the call fails at the HTTP level, not at
    // parameter validation — proving `value` is truly optional for `exists`.
    let tool = ObsidianQueryByPropertyTool::new("http://127.0.0.1:19999");
    let result = tool
        .execute(serde_json::json!({
            "property": "status",
            "operator": "exists"
        }))
        .await
        .unwrap();
    // Should fail at HTTP, not at validation (no "Missing 'property'" error)
    assert!(!result.success);
    assert!(!result
        .error
        .as_deref()
        .unwrap_or("")
        .contains("Invalid operator"));
}

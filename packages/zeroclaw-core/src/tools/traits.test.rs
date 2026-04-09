use super::*;

struct DummyTool;

#[async_trait]
impl Tool for DummyTool {
    fn name(&self) -> &str {
        "dummy_tool"
    }

    fn description(&self) -> &str {
        "A deterministic test tool"
    }

    fn parameters_schema(&self) -> serde_json::Value {
        serde_json::json!({
            "type": "object",
            "properties": {
                "value": { "type": "string" }
            }
        })
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        Ok(ToolResult {
            success: true,
            output: args
                .get("value")
                .and_then(serde_json::Value::as_str)
                .unwrap_or_default()
                .to_string(),
            error: None,
        })
    }
}

#[test]
fn spec_uses_tool_metadata_and_schema() {
    let tool = DummyTool;
    let spec = tool.spec();

    assert_eq!(spec.name, "dummy_tool");
    assert_eq!(spec.description, "A deterministic test tool");
    assert_eq!(spec.parameters["type"], "object");
    assert_eq!(spec.parameters["properties"]["value"]["type"], "string");
}

#[tokio::test]
async fn execute_returns_expected_output() {
    let tool = DummyTool;
    let result = tool
        .execute(serde_json::json!({ "value": "hello-tool" }))
        .await
        .unwrap();

    assert!(result.success);
    assert_eq!(result.output, "hello-tool");
    assert!(result.error.is_none());
}

#[test]
fn tool_result_serialization_roundtrip() {
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

use super::*;
use crate::security::{AutonomyLevel, SecurityPolicy};
use crate::skills::SkillTool;

fn test_security() -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        workspace_dir: std::env::temp_dir(),
        ..SecurityPolicy::default()
    })
}

fn sample_skill_tool() -> SkillTool {
    let mut args = HashMap::new();
    args.insert("file".to_string(), "The file to lint".to_string());
    args.insert(
        "format".to_string(),
        "Output format (json|text)".to_string(),
    );

    SkillTool {
        name: "run_lint".to_string(),
        description: "Run the linter on a file".to_string(),
        kind: "shell".to_string(),
        command: "lint --file {{file}} --format {{format}}".to_string(),
        args,
    }
}

#[test]
fn skill_shell_tool_name_is_prefixed() {
    let tool = SkillShellTool::new("my_skill", &sample_skill_tool(), test_security(), &[]);
    assert_eq!(tool.name(), "my_skill.run_lint");
}

#[test]
fn skill_shell_tool_description() {
    let tool = SkillShellTool::new("my_skill", &sample_skill_tool(), test_security(), &[]);
    assert_eq!(tool.description(), "Run the linter on a file");
}

#[test]
fn skill_shell_tool_parameters_schema() {
    let tool = SkillShellTool::new("my_skill", &sample_skill_tool(), test_security(), &[]);
    let schema = tool.parameters_schema();

    assert_eq!(schema["type"], "object");
    assert!(schema["properties"]["file"].is_object());
    assert_eq!(schema["properties"]["file"]["type"], "string");
    assert!(schema["properties"]["format"].is_object());

    let required = schema["required"]
        .as_array()
        .expect("required should be array");
    assert_eq!(required.len(), 2);
}

#[test]
fn skill_shell_tool_substitute_args() {
    let tool = SkillShellTool::new("my_skill", &sample_skill_tool(), test_security(), &[]);
    let result = tool.substitute_args(&serde_json::json!({
        "file": "src/main.rs",
        "format": "json"
    }));
    assert_eq!(result, "lint --file src/main.rs --format json");
}

#[test]
fn skill_shell_tool_substitute_missing_arg() {
    let tool = SkillShellTool::new("my_skill", &sample_skill_tool(), test_security(), &[]);
    let result = tool.substitute_args(&serde_json::json!({"file": "test.rs"}));
    // Missing {{format}} placeholder stays in the command
    assert!(result.contains("{{format}}"));
    assert!(result.contains("test.rs"));
}

#[test]
fn skill_shell_tool_empty_args_schema() {
    let st = SkillTool {
        name: "simple".to_string(),
        description: "Simple tool".to_string(),
        kind: "shell".to_string(),
        command: "echo hello".to_string(),
        args: HashMap::new(),
    };
    let tool = SkillShellTool::new("s", &st, test_security(), &[]);
    let schema = tool.parameters_schema();
    assert_eq!(schema["type"], "object");
    assert!(schema["properties"].as_object().unwrap().is_empty());
    assert!(schema["required"].as_array().unwrap().is_empty());
}

#[tokio::test]
async fn skill_shell_tool_executes_echo() {
    let st = SkillTool {
        name: "hello".to_string(),
        description: "Say hello".to_string(),
        kind: "shell".to_string(),
        command: "echo hello-skill".to_string(),
        args: HashMap::new(),
    };
    let tool = SkillShellTool::new("test", &st, test_security(), &[]);
    let result = tool.execute(serde_json::json!({})).await.unwrap();
    assert!(result.success);
    assert!(result.output.contains("hello-skill"));
}

#[test]
fn skill_shell_tool_spec_roundtrip() {
    let tool = SkillShellTool::new("my_skill", &sample_skill_tool(), test_security(), &[]);
    let spec = tool.spec();
    assert_eq!(spec.name, "my_skill.run_lint");
    assert_eq!(spec.description, "Run the linter on a file");
    assert_eq!(spec.parameters["type"], "object");
}

#[test]
fn env_passthrough_is_stored() {
    let passthrough = vec!["VAULT_HOST".to_string(), "VAULT_PORT".to_string()];
    let tool = SkillShellTool::new("vault", &sample_skill_tool(), test_security(), &passthrough);
    assert_eq!(tool.env_passthrough, passthrough);
}

#[test]
fn empty_env_passthrough_is_default() {
    let tool = SkillShellTool::new("my_skill", &sample_skill_tool(), test_security(), &[]);
    assert!(tool.env_passthrough.is_empty());
}

/// Security policy that allows all commands (for env passthrough tests).
fn permissive_security() -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        workspace_dir: std::env::temp_dir(),
        allowed_commands: vec!["*".into()],
        block_high_risk_commands: false,
        ..SecurityPolicy::default()
    })
}

#[tokio::test]
async fn env_passthrough_vars_are_available_in_shell() {
    // Set a test env var that will be passed through
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::set_var("ZEROCLAW_TEST_PASSTHROUGH", "vault_works") };

    let st = SkillTool {
        name: "print_env".to_string(),
        description: "Print env var".to_string(),
        kind: "shell".to_string(),
        command: "printenv ZEROCLAW_TEST_PASSTHROUGH".to_string(),
        args: HashMap::new(),
    };
    let passthrough = vec!["ZEROCLAW_TEST_PASSTHROUGH".to_string()];
    let tool = SkillShellTool::new("test", &st, permissive_security(), &passthrough);
    let result = tool.execute(serde_json::json!({})).await.unwrap();

    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("ZEROCLAW_TEST_PASSTHROUGH") };

    assert!(
        result.success,
        "execute should succeed, error: {:?}",
        result.error
    );
    assert!(
        result.output.contains("vault_works"),
        "env var should be visible in shell output, got: {}",
        result.output
    );
}

#[tokio::test]
async fn non_passthrough_vars_are_blocked() {
    // Set a test env var that will NOT be in passthrough
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::set_var("ZEROCLAW_TEST_BLOCKED", "secret_value") };

    let st = SkillTool {
        name: "print_env".to_string(),
        description: "Print env var".to_string(),
        kind: "shell".to_string(),
        // printenv returns exit code 1 when the var is not set; || true keeps it success
        command: "printenv ZEROCLAW_TEST_BLOCKED || true".to_string(),
        args: HashMap::new(),
    };
    // Empty passthrough — the var should NOT be available
    let tool = SkillShellTool::new("test", &st, permissive_security(), &[]);
    let result = tool.execute(serde_json::json!({})).await.unwrap();

    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("ZEROCLAW_TEST_BLOCKED") };

    assert!(
        result.success,
        "execute should succeed, error: {:?}",
        result.error
    );
    assert!(
        !result.output.contains("secret_value"),
        "non-passthrough env var should not be visible, got: {}",
        result.output
    );
}

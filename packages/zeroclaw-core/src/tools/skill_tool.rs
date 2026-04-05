//! Shell-based tool derived from a skill's `[[tools]]` section.
//!
//! Each `SkillTool` with `kind = "shell"` or `kind = "script"` is converted
//! into a `SkillShellTool` that implements the `Tool` trait. The tool name is
//! prefixed with the skill name (e.g. `my_skill.run_lint`) to avoid collisions
//! with built-in tools.

use super::traits::{Tool, ToolResult};
use crate::security::SecurityPolicy;
use async_trait::async_trait;
use std::collections::HashMap;
use std::sync::Arc;
use std::time::Duration;

/// Maximum execution time for a skill shell command (seconds).
const SKILL_SHELL_TIMEOUT_SECS: u64 = 60;
/// Maximum output size in bytes (1 MB).
const MAX_OUTPUT_BYTES: usize = 1_048_576;

/// A tool derived from a skill's `[[tools]]` section that executes shell commands.
pub struct SkillShellTool {
    tool_name: String,
    tool_description: String,
    command_template: String,
    args: HashMap<String, String>,
    security: Arc<SecurityPolicy>,
    /// Extra environment variable names to pass through from host to the shell process.
    env_passthrough: Vec<String>,
}

impl SkillShellTool {
    /// Create a new skill shell tool.
    ///
    /// The tool name is prefixed with the skill name (`skill_name.tool_name`)
    /// to prevent collisions with built-in tools.
    pub fn new(
        skill_name: &str,
        tool: &crate::skills::SkillTool,
        security: Arc<SecurityPolicy>,
        env_passthrough: &[String],
    ) -> Self {
        Self {
            tool_name: format!("{}.{}", skill_name, tool.name),
            tool_description: tool.description.clone(),
            command_template: tool.command.clone(),
            args: tool.args.clone(),
            security,
            env_passthrough: env_passthrough.to_vec(),
        }
    }

    fn build_parameters_schema(&self) -> serde_json::Value {
        let mut properties = serde_json::Map::new();
        let mut required = Vec::new();

        for (name, description) in &self.args {
            properties.insert(
                name.clone(),
                serde_json::json!({
                    "type": "string",
                    "description": description
                }),
            );
            required.push(serde_json::Value::String(name.clone()));
        }

        serde_json::json!({
            "type": "object",
            "properties": properties,
            "required": required
        })
    }

    /// Substitute `{{arg_name}}` placeholders in the command template with
    /// the provided argument values. Unknown placeholders are left as-is.
    fn substitute_args(&self, args: &serde_json::Value) -> String {
        let mut command = self.command_template.clone();
        if let Some(obj) = args.as_object() {
            for (key, value) in obj {
                let placeholder = format!("{{{{{}}}}}", key);
                let replacement = value.as_str().unwrap_or_default();
                command = command.replace(&placeholder, replacement);
            }
        }
        command
    }
}

#[async_trait]
impl Tool for SkillShellTool {
    fn name(&self) -> &str {
        &self.tool_name
    }

    fn description(&self) -> &str {
        &self.tool_description
    }

    fn parameters_schema(&self) -> serde_json::Value {
        self.build_parameters_schema()
    }

    async fn execute(&self, args: serde_json::Value) -> anyhow::Result<ToolResult> {
        let command = self.substitute_args(&args);

        // Rate limit check
        if self.security.is_rate_limited() {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some("Rate limit exceeded: too many actions in the last hour".into()),
            });
        }

        // Security validation — always requires explicit approval (approved=true)
        // since skill tools are user-defined and should be treated as medium-risk.
        match self.security.validate_command_execution(&command, true) {
            Ok(_) => {}
            Err(reason) => {
                return Ok(ToolResult {
                    success: false,
                    output: String::new(),
                    error: Some(reason),
                });
            }
        }

        if let Some(path) = self.security.forbidden_path_argument(&command) {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("Path blocked by security policy: {path}")),
            });
        }

        if !self.security.record_action() {
            return Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some("Rate limit exceeded: action budget exhausted".into()),
            });
        }

        // Build and execute the command
        let mut cmd = tokio::process::Command::new("sh");
        cmd.arg("-c").arg(&command);
        cmd.current_dir(&self.security.workspace_dir);
        cmd.env_clear();

        // Only pass safe environment variables
        for var in &[
            "PATH", "HOME", "TERM", "LANG", "LC_ALL", "USER", "SHELL", "TMPDIR",
        ] {
            if let Ok(val) = std::env::var(var) {
                cmd.env(var, val);
            }
        }

        // Pass through skill-declared environment variables
        for var in &self.env_passthrough {
            if let Ok(val) = std::env::var(var) {
                cmd.env(var, val);
            }
        }

        let result =
            tokio::time::timeout(Duration::from_secs(SKILL_SHELL_TIMEOUT_SECS), cmd.output()).await;

        match result {
            Ok(Ok(output)) => {
                let mut stdout = String::from_utf8_lossy(&output.stdout).to_string();
                let mut stderr = String::from_utf8_lossy(&output.stderr).to_string();

                if stdout.len() > MAX_OUTPUT_BYTES {
                    let mut b = MAX_OUTPUT_BYTES.min(stdout.len());
                    while b > 0 && !stdout.is_char_boundary(b) {
                        b -= 1;
                    }
                    stdout.truncate(b);
                    stdout.push_str("\n... [output truncated at 1MB]");
                }
                if stderr.len() > MAX_OUTPUT_BYTES {
                    let mut b = MAX_OUTPUT_BYTES.min(stderr.len());
                    while b > 0 && !stderr.is_char_boundary(b) {
                        b -= 1;
                    }
                    stderr.truncate(b);
                    stderr.push_str("\n... [stderr truncated at 1MB]");
                }

                Ok(ToolResult {
                    success: output.status.success(),
                    output: stdout,
                    error: if stderr.is_empty() {
                        None
                    } else {
                        Some(stderr)
                    },
                })
            }
            Ok(Err(e)) => Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!("Failed to execute command: {e}")),
            }),
            Err(_) => Ok(ToolResult {
                success: false,
                output: String::new(),
                error: Some(format!(
                    "Command timed out after {SKILL_SHELL_TIMEOUT_SECS}s and was killed"
                )),
            }),
        }
    }
}

#[cfg(test)]
mod tests {
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
        let tool = SkillShellTool::new(
            "vault",
            &sample_skill_tool(),
            test_security(),
            &passthrough,
        );
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
}

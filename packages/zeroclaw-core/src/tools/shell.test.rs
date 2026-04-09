use super::*;
use crate::runtime::{NativeRuntime, RuntimeAdapter};
use crate::security::{AutonomyLevel, SecurityPolicy};
use crate::tools::wrappers::{PathGuardedTool, RateLimitedTool};

fn test_security(autonomy: AutonomyLevel) -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy {
        autonomy,
        workspace_dir: std::env::temp_dir(),
        ..SecurityPolicy::default()
    })
}

fn test_runtime() -> Arc<dyn RuntimeAdapter> {
    Arc::new(NativeRuntime::new())
}

/// Returns the fully-wrapped shell tool as it is composed in production:
/// RateLimited(PathGuarded(ShellTool)).  Tests that verify path-blocking or
/// rate-limiting behaviour must use this helper so they exercise the wrappers.
fn wrapped_shell(security: Arc<SecurityPolicy>) -> RateLimitedTool<PathGuardedTool<ShellTool>> {
    RateLimitedTool::new(
        PathGuardedTool::new(
            ShellTool::new(security.clone(), test_runtime()),
            security.clone(),
        ),
        security,
    )
}

#[test]
fn shell_tool_name() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime());
    assert_eq!(tool.name(), "shell");
}

#[test]
fn shell_tool_description() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime());
    assert!(!tool.description().is_empty());
}

#[test]
fn shell_tool_schema_has_command() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime());
    let schema = tool.parameters_schema();
    assert!(schema["properties"]["command"].is_object());
    assert!(
        schema["required"]
            .as_array()
            .expect("schema required field should be an array")
            .contains(&json!("command"))
    );
    assert!(schema["properties"]["approved"].is_object());
}

#[tokio::test]
async fn shell_executes_allowed_command() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime());
    let result = tool
        .execute(json!({"command": "echo hello"}))
        .await
        .expect("echo command execution should succeed");
    assert!(result.success);
    assert!(result.output.trim().contains("hello"));
    assert!(result.error.is_none());
}

#[tokio::test]
async fn shell_blocks_disallowed_command() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime());
    let result = tool
        .execute(json!({"command": "rm -rf /"}))
        .await
        .expect("disallowed command execution should return a result");
    assert!(!result.success);
    let error = result.error.as_deref().unwrap_or("");
    assert!(error.contains("not allowed") || error.contains("high-risk"));
}

#[tokio::test]
async fn shell_blocks_readonly() {
    let tool = ShellTool::new(test_security(AutonomyLevel::ReadOnly), test_runtime());
    let result = tool
        .execute(json!({"command": "ls"}))
        .await
        .expect("readonly command execution should return a result");
    assert!(!result.success);
    assert!(
        result
            .error
            .as_ref()
            .expect("error field should be present for blocked command")
            .contains("not allowed")
    );
}

#[tokio::test]
async fn shell_missing_command_param() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime());
    let result = tool.execute(json!({})).await;
    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("command"));
}

#[tokio::test]
async fn shell_wrong_type_param() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime());
    let result = tool.execute(json!({"command": 123})).await;
    assert!(result.is_err());
}

#[tokio::test]
async fn shell_captures_exit_code() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime());
    let result = tool
        .execute(json!({"command": "ls /nonexistent_dir_xyz"}))
        .await
        .expect("command with nonexistent path should return a result");
    assert!(!result.success);
}

#[tokio::test]
async fn shell_blocks_absolute_path_argument() {
    let tool = wrapped_shell(test_security(AutonomyLevel::Supervised));
    let result = tool
        .execute(json!({"command": "cat /etc/passwd"}))
        .await
        .expect("absolute path argument should be blocked");
    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("Path blocked")
    );
}

#[tokio::test]
async fn shell_blocks_option_assignment_path_argument() {
    let tool = wrapped_shell(test_security(AutonomyLevel::Supervised));
    let result = tool
        .execute(json!({"command": "grep --file=/etc/passwd root ./src"}))
        .await
        .expect("option-assigned forbidden path should be blocked");
    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("Path blocked")
    );
}

#[tokio::test]
async fn shell_blocks_short_option_attached_path_argument() {
    let tool = wrapped_shell(test_security(AutonomyLevel::Supervised));
    let result = tool
        .execute(json!({"command": "grep -f/etc/passwd root ./src"}))
        .await
        .expect("short option attached forbidden path should be blocked");
    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("Path blocked")
    );
}

#[tokio::test]
async fn shell_blocks_tilde_user_path_argument() {
    let tool = wrapped_shell(test_security(AutonomyLevel::Supervised));
    let result = tool
        .execute(json!({"command": "cat ~root/.ssh/id_rsa"}))
        .await
        .expect("tilde-user path should be blocked");
    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("Path blocked")
    );
}

#[tokio::test]
async fn shell_blocks_input_redirection_path_bypass() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime());
    let result = tool
        .execute(json!({"command": "cat </etc/passwd"}))
        .await
        .expect("input redirection bypass should be blocked");
    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("not allowed")
    );
}

fn test_security_with_env_cmd() -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        workspace_dir: std::env::temp_dir(),
        allowed_commands: vec!["env".into(), "echo".into()],
        ..SecurityPolicy::default()
    })
}

fn test_security_with_env_passthrough(vars: &[&str]) -> Arc<SecurityPolicy> {
    Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        workspace_dir: std::env::temp_dir(),
        allowed_commands: vec!["env".into()],
        shell_env_passthrough: vars.iter().map(|v| (*v).to_string()).collect(),
        ..SecurityPolicy::default()
    })
}

/// RAII guard that restores an environment variable to its original state on drop,
/// ensuring cleanup even if the test panics.
struct EnvGuard {
    key: &'static str,
    original: Option<String>,
}

impl EnvGuard {
    fn set(key: &'static str, value: &str) -> Self {
        let original = std::env::var(key).ok();
        // SAFETY: test-only, single-threaded test runner.
        unsafe { std::env::set_var(key, value) };
        Self { key, original }
    }
}

impl Drop for EnvGuard {
    fn drop(&mut self) {
        match &self.original {
            // SAFETY: test-only, single-threaded test runner.
            Some(val) => unsafe { std::env::set_var(self.key, val) },
            // SAFETY: test-only, single-threaded test runner.
            None => unsafe { std::env::remove_var(self.key) },
        }
    }
}

#[tokio::test(flavor = "current_thread")]
async fn shell_does_not_leak_api_key() {
    let _g1 = EnvGuard::set("API_KEY", "sk-test-secret-12345");
    let _g2 = EnvGuard::set("ZEROCLAW_API_KEY", "sk-test-secret-67890");

    let tool = ShellTool::new(test_security_with_env_cmd(), test_runtime());
    let result = tool
        .execute(json!({"command": "env"}))
        .await
        .expect("env command execution should succeed");
    assert!(result.success);
    assert!(
        !result.output.contains("sk-test-secret-12345"),
        "API_KEY leaked to shell command output"
    );
    assert!(
        !result.output.contains("sk-test-secret-67890"),
        "ZEROCLAW_API_KEY leaked to shell command output"
    );
}

#[tokio::test]
async fn shell_preserves_path_and_home_for_env_command() {
    let tool = ShellTool::new(test_security_with_env_cmd(), test_runtime());

    let result = tool
        .execute(json!({"command": "env"}))
        .await
        .expect("env command should succeed");
    assert!(result.success);
    assert!(
        result.output.contains("HOME="),
        "HOME should be available in shell environment"
    );
    assert!(
        result.output.contains("PATH="),
        "PATH should be available in shell environment"
    );
}

#[tokio::test]
async fn shell_blocks_plain_variable_expansion() {
    let tool = ShellTool::new(test_security_with_env_cmd(), test_runtime());
    let result = tool
        .execute(json!({"command": "echo $HOME"}))
        .await
        .expect("plain variable expansion should be blocked");
    assert!(!result.success);
    assert!(
        result
            .error
            .as_deref()
            .unwrap_or("")
            .contains("not allowed")
    );
}

#[tokio::test(flavor = "current_thread")]
async fn shell_allows_configured_env_passthrough() {
    let _guard = EnvGuard::set("ZEROCLAW_TEST_PASSTHROUGH", "db://unit-test");
    let tool = ShellTool::new(
        test_security_with_env_passthrough(&["ZEROCLAW_TEST_PASSTHROUGH"]),
        test_runtime(),
    );

    let result = tool
        .execute(json!({"command": "env"}))
        .await
        .expect("env command execution should succeed");
    assert!(result.success);
    assert!(
        result
            .output
            .contains("ZEROCLAW_TEST_PASSTHROUGH=db://unit-test")
    );
}

#[test]
fn invalid_shell_env_passthrough_names_are_filtered() {
    let security = SecurityPolicy {
        shell_env_passthrough: vec![
            "VALID_NAME".into(),
            "BAD-NAME".into(),
            "1NOPE".into(),
            "ALSO_VALID".into(),
        ],
        ..SecurityPolicy::default()
    };
    let vars = collect_allowed_shell_env_vars(&security);
    assert!(vars.contains(&"VALID_NAME".to_string()));
    assert!(vars.contains(&"ALSO_VALID".to_string()));
    assert!(!vars.contains(&"BAD-NAME".to_string()));
    assert!(!vars.contains(&"1NOPE".to_string()));
}

#[tokio::test]
async fn shell_requires_approval_for_medium_risk_command() {
    let security = Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        allowed_commands: vec!["touch".into()],
        workspace_dir: std::env::temp_dir(),
        ..SecurityPolicy::default()
    });

    let tool = ShellTool::new(security.clone(), test_runtime());
    let denied = tool
        .execute(json!({"command": "touch zeroclaw_shell_approval_test"}))
        .await
        .expect("unapproved command should return a result");
    assert!(!denied.success);
    assert!(
        denied
            .error
            .as_deref()
            .unwrap_or("")
            .contains("explicit approval")
    );

    let allowed = tool
        .execute(json!({
            "command": "touch zeroclaw_shell_approval_test",
            "approved": true
        }))
        .await
        .expect("approved command execution should succeed");
    assert!(allowed.success);

    let _ = tokio::fs::remove_file(std::env::temp_dir().join("zeroclaw_shell_approval_test")).await;
}

// ── shell timeout enforcement tests ─────────────────

#[test]
fn shell_timeout_default_is_reasonable() {
    assert_eq!(
        DEFAULT_SHELL_TIMEOUT_SECS, 60,
        "default shell timeout must be 60 seconds"
    );
}

#[test]
fn shell_timeout_can_be_overridden() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Supervised), test_runtime())
        .with_timeout_secs(120);
    assert_eq!(tool.timeout_secs, 120);
}

#[test]
fn shell_output_limit_is_1mb() {
    assert_eq!(
        MAX_OUTPUT_BYTES, 1_048_576,
        "max output must be 1 MB to prevent OOM"
    );
}

// ── Non-UTF8 binary output tests ────────────────────

#[test]
fn shell_safe_env_vars_excludes_secrets() {
    for var in SAFE_ENV_VARS {
        let lower = var.to_lowercase();
        assert!(
            !lower.contains("key") && !lower.contains("secret") && !lower.contains("token"),
            "SAFE_ENV_VARS must not include sensitive variable: {var}"
        );
    }
}

#[test]
fn shell_safe_env_vars_includes_essentials() {
    assert!(
        SAFE_ENV_VARS.contains(&"PATH"),
        "PATH must be in safe env vars"
    );
    assert!(
        SAFE_ENV_VARS.contains(&"HOME") || SAFE_ENV_VARS.contains(&"USERPROFILE"),
        "HOME or USERPROFILE must be in safe env vars"
    );
    assert!(
        SAFE_ENV_VARS.contains(&"TERM"),
        "TERM must be in safe env vars"
    );
}

#[tokio::test]
async fn shell_blocks_rate_limited() {
    let security = Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        max_actions_per_hour: 0,
        workspace_dir: std::env::temp_dir(),
        ..SecurityPolicy::default()
    });
    let tool = wrapped_shell(security);
    let result = tool
        .execute(json!({"command": "echo test"}))
        .await
        .expect("rate-limited command should return a result");
    assert!(!result.success);
    assert!(result.error.as_deref().unwrap_or("").contains("Rate limit"));
}

#[tokio::test]
async fn shell_handles_nonexistent_command() {
    let security = Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        workspace_dir: std::env::temp_dir(),
        ..SecurityPolicy::default()
    });
    let tool = ShellTool::new(security, test_runtime());
    let result = tool
        .execute(json!({"command": "nonexistent_binary_xyz_12345"}))
        .await
        .unwrap();
    assert!(!result.success);
}

#[tokio::test]
async fn shell_captures_stderr_output() {
    let tool = ShellTool::new(test_security(AutonomyLevel::Full), test_runtime());
    let result = tool
        .execute(json!({"command": "echo error_msg >&2"}))
        .await
        .unwrap();
    assert!(result.error.as_deref().unwrap_or("").contains("error_msg"));
}

#[tokio::test]
async fn shell_record_action_budget_exhaustion() {
    let security = Arc::new(SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        max_actions_per_hour: 1,
        workspace_dir: std::env::temp_dir(),
        ..SecurityPolicy::default()
    });
    let tool = wrapped_shell(security);

    let r1 = tool
        .execute(json!({"command": "echo first"}))
        .await
        .unwrap();
    assert!(r1.success);

    let r2 = tool
        .execute(json!({"command": "echo second"}))
        .await
        .unwrap();
    assert!(!r2.success);
    assert!(
        r2.error.as_deref().unwrap_or("").contains("Rate limit")
            || r2.error.as_deref().unwrap_or("").contains("budget")
    );
}

// ── Sandbox integration tests ────────────────────────

#[test]
fn shell_tool_can_be_constructed_with_sandbox() {
    use crate::security::NoopSandbox;

    let sandbox: Arc<dyn Sandbox> = Arc::new(NoopSandbox);
    let tool = ShellTool::new_with_sandbox(
        test_security(AutonomyLevel::Supervised),
        test_runtime(),
        sandbox,
    );
    assert_eq!(tool.name(), "shell");
}

#[test]
fn noop_sandbox_does_not_modify_command() {
    use crate::security::NoopSandbox;

    let sandbox = NoopSandbox;
    let mut cmd = std::process::Command::new("echo");
    cmd.arg("hello");

    let program_before = cmd.get_program().to_os_string();
    let args_before: Vec<_> = cmd.get_args().map(|a| a.to_os_string()).collect();

    sandbox
        .wrap_command(&mut cmd)
        .expect("wrap_command should succeed");

    assert_eq!(cmd.get_program(), program_before);
    assert_eq!(
        cmd.get_args().map(|a| a.to_os_string()).collect::<Vec<_>>(),
        args_before
    );
}

#[tokio::test]
async fn shell_executes_with_sandbox() {
    use crate::security::NoopSandbox;

    let sandbox: Arc<dyn Sandbox> = Arc::new(NoopSandbox);
    let tool = ShellTool::new_with_sandbox(
        test_security(AutonomyLevel::Supervised),
        test_runtime(),
        sandbox,
    );
    let result = tool
        .execute(json!({"command": "echo sandbox_test"}))
        .await
        .expect("command with sandbox should succeed");
    assert!(result.success);
    assert!(result.output.contains("sandbox_test"));
}

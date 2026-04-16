use super::*;
use tempfile::TempDir;

fn test_config(tmp: &TempDir) -> Config {
    let config = Config {
        workspace_dir: tmp.path().join("workspace"),
        config_path: tmp.path().join("config.toml"),
        ..Config::default()
    };
    std::fs::create_dir_all(&config.workspace_dir).unwrap();
    config
}

fn make_job(config: &Config, expr: &str, tz: Option<&str>, cmd: &str) -> CronJob {
    add_shell_job(
        config,
        None,
        Schedule::Cron {
            expr: expr.into(),
            tz: tz.map(Into::into),
        },
        cmd,
    )
    .unwrap()
}

fn run_update(
    config: &Config,
    id: &str,
    expression: Option<&str>,
    tz: Option<&str>,
    command: Option<&str>,
    name: Option<&str>,
) -> Result<()> {
    handle_command(
        crate::CronCommands::Update {
            id: id.into(),
            expression: expression.map(Into::into),
            tz: tz.map(Into::into),
            command: command.map(Into::into),
            name: name.map(Into::into),
            allowed_tools: vec![],
        },
        config,
    )
}

#[test]
fn update_changes_command_via_handler() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);
    let job = make_job(&config, "*/5 * * * *", None, "echo original");

    run_update(&config, &job.id, None, None, Some("echo updated"), None).unwrap();

    let updated = get_job(&config, &job.id).unwrap();
    assert_eq!(updated.command, "echo updated");
    assert_eq!(updated.id, job.id);
}

#[test]
fn update_changes_expression_via_handler() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);
    let job = make_job(&config, "*/5 * * * *", None, "echo test");

    run_update(&config, &job.id, Some("0 9 * * *"), None, None, None).unwrap();

    let updated = get_job(&config, &job.id).unwrap();
    assert_eq!(updated.expression, "0 9 * * *");
}

#[test]
fn update_changes_name_via_handler() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);
    let job = make_job(&config, "*/5 * * * *", None, "echo test");

    run_update(&config, &job.id, None, None, None, Some("new-name")).unwrap();

    let updated = get_job(&config, &job.id).unwrap();
    assert_eq!(updated.name.as_deref(), Some("new-name"));
}

#[test]
fn update_tz_alone_sets_timezone() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);
    let job = make_job(&config, "*/5 * * * *", None, "echo test");

    run_update(
        &config,
        &job.id,
        None,
        Some("America/Los_Angeles"),
        None,
        None,
    )
    .unwrap();

    let updated = get_job(&config, &job.id).unwrap();
    assert_eq!(
        updated.schedule,
        Schedule::Cron {
            expr: "*/5 * * * *".into(),
            tz: Some("America/Los_Angeles".into()),
        }
    );
}

#[test]
fn update_expression_preserves_existing_tz() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);
    let job = make_job(
        &config,
        "*/5 * * * *",
        Some("America/Los_Angeles"),
        "echo test",
    );

    run_update(&config, &job.id, Some("0 9 * * *"), None, None, None).unwrap();

    let updated = get_job(&config, &job.id).unwrap();
    assert_eq!(
        updated.schedule,
        Schedule::Cron {
            expr: "0 9 * * *".into(),
            tz: Some("America/Los_Angeles".into()),
        }
    );
}

#[test]
fn update_preserves_unchanged_fields() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);
    let job = add_shell_job(
        &config,
        Some("original-name".into()),
        Schedule::Cron {
            expr: "*/5 * * * *".into(),
            tz: None,
        },
        "echo original",
    )
    .unwrap();

    run_update(&config, &job.id, None, None, Some("echo changed"), None).unwrap();

    let updated = get_job(&config, &job.id).unwrap();
    assert_eq!(updated.command, "echo changed");
    assert_eq!(updated.name.as_deref(), Some("original-name"));
    assert_eq!(updated.expression, "*/5 * * * *");
}

#[test]
fn update_no_flags_fails() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);
    let job = make_job(&config, "*/5 * * * *", None, "echo test");

    let result = run_update(&config, &job.id, None, None, None, None);
    assert!(result.is_err());
    assert!(result.unwrap_err().to_string().contains("At least one of"));
}

#[test]
fn update_nonexistent_job_fails() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);

    let result = run_update(
        &config,
        "nonexistent-id",
        None,
        None,
        Some("echo test"),
        None,
    );
    assert!(result.is_err());
}

#[test]
fn update_security_allows_safe_command() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);

    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);
    assert!(security.is_command_allowed("echo safe"));
}

#[test]
fn add_shell_job_requires_explicit_approval_for_medium_risk() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp);
    config.autonomy.allowed_commands = vec!["echo".into(), "touch".into()];

    let denied = add_shell_job(
        &config,
        None,
        Schedule::Cron {
            expr: "*/5 * * * *".into(),
            tz: None,
        },
        "touch cron-medium-risk",
    );
    assert!(denied.is_err());
    assert!(
        denied
            .unwrap_err()
            .to_string()
            .contains("explicit approval")
    );

    let approved = add_shell_job_with_approval(
        &config,
        None,
        Schedule::Cron {
            expr: "*/5 * * * *".into(),
            tz: None,
        },
        "touch cron-medium-risk",
        None,
        true,
    );
    assert!(approved.is_ok(), "{approved:?}");
}

#[test]
fn update_requires_explicit_approval_for_medium_risk() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp);
    config.autonomy.allowed_commands = vec!["echo".into(), "touch".into()];
    let job = make_job(&config, "*/5 * * * *", None, "echo original");

    let denied = update_shell_job_with_approval(
        &config,
        &job.id,
        CronJobPatch {
            command: Some("touch cron-medium-risk-update".into()),
            ..CronJobPatch::default()
        },
        false,
    );
    assert!(denied.is_err());
    assert!(
        denied
            .unwrap_err()
            .to_string()
            .contains("explicit approval")
    );

    let approved = update_shell_job_with_approval(
        &config,
        &job.id,
        CronJobPatch {
            command: Some("touch cron-medium-risk-update".into()),
            ..CronJobPatch::default()
        },
        true,
    )
    .unwrap();
    assert_eq!(approved.command, "touch cron-medium-risk-update");
}

#[test]
fn cli_update_requires_explicit_approval_for_medium_risk() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp);
    config.autonomy.allowed_commands = vec!["echo".into(), "touch".into()];
    let job = make_job(&config, "*/5 * * * *", None, "echo original");

    let result = run_update(
        &config,
        &job.id,
        None,
        None,
        Some("touch cron-cli-medium-risk"),
        None,
    );
    assert!(result.is_err());
    assert!(
        result
            .unwrap_err()
            .to_string()
            .contains("explicit approval")
    );
}

#[test]
fn add_once_validated_creates_one_shot_job() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);

    let job = add_once_validated(&config, "1h", "echo one-shot", false).unwrap();
    assert_eq!(job.command, "echo one-shot");
    assert!(matches!(job.schedule, Schedule::At { .. }));
}

#[test]
fn add_once_validated_blocks_disallowed_command() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp);
    config.autonomy.allowed_commands = vec!["echo".into()];
    config.autonomy.level = crate::security::AutonomyLevel::Supervised;

    let result = add_once_validated(&config, "1h", "curl https://example.com", false);
    assert!(result.is_err());
    assert!(
        result
            .unwrap_err()
            .to_string()
            .contains("blocked by security policy")
    );
}

#[test]
fn add_once_at_validated_creates_one_shot_job() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);
    let at = chrono::Utc::now() + chrono::Duration::hours(1);

    let job = add_once_at_validated(&config, at, "echo at-shot", false).unwrap();
    assert_eq!(job.command, "echo at-shot");
    assert!(matches!(job.schedule, Schedule::At { .. }));
}

#[test]
fn add_once_at_validated_blocks_medium_risk_without_approval() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp);
    config.autonomy.allowed_commands = vec!["echo".into(), "touch".into()];
    let at = chrono::Utc::now() + chrono::Duration::hours(1);

    let denied = add_once_at_validated(&config, at, "touch at-medium", false);
    assert!(denied.is_err());
    assert!(
        denied
            .unwrap_err()
            .to_string()
            .contains("explicit approval")
    );

    let approved = add_once_at_validated(&config, at, "touch at-medium", true);
    assert!(approved.is_ok(), "{approved:?}");
}

#[test]
fn gateway_api_path_validates_shell_command() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp);
    config.autonomy.allowed_commands = vec!["echo".into()];
    config.autonomy.level = crate::security::AutonomyLevel::Supervised;

    // Simulate gateway API path: add_shell_job_with_approval(approved=false)
    let result = add_shell_job_with_approval(
        &config,
        None,
        Schedule::Cron {
            expr: "*/5 * * * *".into(),
            tz: None,
        },
        "curl https://example.com",
        None,
        false,
    );
    assert!(result.is_err());
    assert!(
        result
            .unwrap_err()
            .to_string()
            .contains("blocked by security policy")
    );
}

#[test]
fn scheduler_path_validates_shell_command() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp);
    config.autonomy.allowed_commands = vec!["echo".into()];
    config.autonomy.level = crate::security::AutonomyLevel::Supervised;

    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);
    // Simulate scheduler validation path
    let result = validate_shell_command_with_security(&security, "curl https://example.com", false);
    assert!(result.is_err());
    assert!(
        result
            .unwrap_err()
            .to_string()
            .contains("blocked by security policy")
    );
}

#[test]
fn cli_agent_flag_creates_agent_job() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);

    handle_command(
        crate::CronCommands::Add {
            expression: "*/15 * * * *".into(),
            tz: None,
            agent: true,
            allowed_tools: vec![],
            command: "Check server health: disk space, memory, CPU load".into(),
        },
        &config,
    )
    .unwrap();

    let jobs = list_jobs(&config).unwrap();
    assert_eq!(jobs.len(), 1);
    assert_eq!(jobs[0].job_type, JobType::Agent);
    assert_eq!(
        jobs[0].prompt.as_deref(),
        Some("Check server health: disk space, memory, CPU load")
    );
}

#[test]
fn cli_agent_flag_bypasses_shell_security_validation() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp);
    config.autonomy.allowed_commands = vec!["echo".into()];
    config.autonomy.level = crate::security::AutonomyLevel::Supervised;

    // Without --agent, a natural language string would be blocked by shell
    // security policy. With --agent, it routes to agent job and skips
    // shell validation entirely.
    let result = handle_command(
        crate::CronCommands::Add {
            expression: "*/15 * * * *".into(),
            tz: None,
            agent: true,
            allowed_tools: vec![],
            command: "Check server health: disk space, memory, CPU load".into(),
        },
        &config,
    );
    assert!(result.is_ok());

    let jobs = list_jobs(&config).unwrap();
    assert_eq!(jobs.len(), 1);
    assert_eq!(jobs[0].job_type, JobType::Agent);
}

#[test]
fn cli_agent_allowed_tools_persist() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);

    handle_command(
        crate::CronCommands::Add {
            expression: "*/15 * * * *".into(),
            tz: None,
            agent: true,
            allowed_tools: vec!["file_read".into(), "web_search".into()],
            command: "Check server health".into(),
        },
        &config,
    )
    .unwrap();

    let jobs = list_jobs(&config).unwrap();
    assert_eq!(jobs.len(), 1);
    assert_eq!(
        jobs[0].allowed_tools,
        Some(vec!["file_read".into(), "web_search".into()])
    );
}

#[test]
fn cli_update_agent_allowed_tools_persist() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);
    let job = add_agent_job(
        &config,
        Some("agent".into()),
        Schedule::Cron {
            expr: "*/5 * * * *".into(),
            tz: None,
        },
        "original prompt",
        SessionTarget::Isolated,
        None,
        None,
        false,
        None,
    )
    .unwrap();

    handle_command(
        crate::CronCommands::Update {
            id: job.id.clone(),
            expression: None,
            tz: None,
            command: None,
            name: None,
            allowed_tools: vec!["shell".into()],
        },
        &config,
    )
    .unwrap();

    let updated = get_job(&config, &job.id).unwrap();
    assert_eq!(updated.allowed_tools, Some(vec!["shell".into()]));
}

#[test]
fn cli_without_agent_flag_defaults_to_shell_job() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);

    handle_command(
        crate::CronCommands::Add {
            expression: "*/5 * * * *".into(),
            tz: None,
            agent: false,
            allowed_tools: vec![],
            command: "echo ok".into(),
        },
        &config,
    )
    .unwrap();

    let jobs = list_jobs(&config).unwrap();
    assert_eq!(jobs.len(), 1);
    assert_eq!(jobs[0].job_type, JobType::Shell);
    assert_eq!(jobs[0].command, "echo ok");
}

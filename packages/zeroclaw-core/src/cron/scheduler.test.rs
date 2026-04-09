use super::*;
use crate::config::Config;
use crate::cron::{self, DeliveryConfig};
use crate::security::SecurityPolicy;
use chrono::{Duration as ChronoDuration, Utc};
use tempfile::TempDir;

async fn test_config(tmp: &TempDir) -> Config {
    let config = Config {
        workspace_dir: tmp.path().join("workspace"),
        config_path: tmp.path().join("config.toml"),
        ..Config::default()
    };
    tokio::fs::create_dir_all(&config.workspace_dir)
        .await
        .unwrap();
    config
}

fn test_job(command: &str) -> CronJob {
    CronJob {
        id: "test-job".into(),
        expression: "* * * * *".into(),
        schedule: crate::cron::Schedule::Cron {
            expr: "* * * * *".into(),
            tz: None,
        },
        command: command.into(),
        prompt: None,
        name: None,
        job_type: JobType::Shell,
        session_target: SessionTarget::Isolated,
        model: None,
        enabled: true,
        delivery: DeliveryConfig::default(),
        delete_after_run: false,
        allowed_tools: None,
        source: "imperative".into(),
        created_at: Utc::now(),
        next_run: Utc::now(),
        last_run: None,
        last_status: None,
        last_output: None,
    }
}

fn unique_component(prefix: &str) -> String {
    format!("{prefix}-{}", uuid::Uuid::new_v4())
}

#[tokio::test]
async fn run_job_command_success() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = test_job("echo scheduler-ok");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(success);
    assert!(output.contains("scheduler-ok"));
    assert!(output.contains("status=exit status: 0"));
}

#[tokio::test]
async fn run_job_command_failure() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = test_job("ls definitely_missing_file_for_scheduler_test");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(!success);
    assert!(output.contains("definitely_missing_file_for_scheduler_test"));
    assert!(output.contains("status=exit status:"));
}

#[tokio::test]
async fn run_job_command_times_out() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.allowed_commands = vec!["sleep".into()];
    let job = test_job("sleep 1");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) =
        run_job_command_with_timeout(&config, &security, &job, Duration::from_millis(50)).await;
    assert!(!success);
    assert!(output.contains("job timed out after"));
}

#[tokio::test]
async fn run_job_command_blocks_disallowed_command() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.allowed_commands = vec!["echo".into()];
    let job = test_job("curl https://evil.example");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.to_lowercase().contains("not allowed"));
}

#[tokio::test]
async fn run_job_command_blocks_forbidden_path_argument() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.allowed_commands = vec!["cat".into()];
    let job = test_job("cat /etc/passwd");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.contains("forbidden path argument"));
    assert!(output.contains("/etc/passwd"));
}

#[tokio::test]
async fn run_job_command_blocks_forbidden_option_assignment_path_argument() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.allowed_commands = vec!["grep".into()];
    let job = test_job("grep --file=/etc/passwd root ./src");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.contains("forbidden path argument"));
    assert!(output.contains("/etc/passwd"));
}

#[tokio::test]
async fn run_job_command_blocks_forbidden_short_option_attached_path_argument() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.allowed_commands = vec!["grep".into()];
    let job = test_job("grep -f/etc/passwd root ./src");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.contains("forbidden path argument"));
    assert!(output.contains("/etc/passwd"));
}

#[tokio::test]
async fn run_job_command_blocks_tilde_user_path_argument() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.allowed_commands = vec!["cat".into()];
    let job = test_job("cat ~root/.ssh/id_rsa");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.contains("forbidden path argument"));
    assert!(output.contains("~root/.ssh/id_rsa"));
}

#[tokio::test]
async fn run_job_command_blocks_input_redirection_path_bypass() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.allowed_commands = vec!["cat".into()];
    let job = test_job("cat </etc/passwd");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.to_lowercase().contains("not allowed"));
}

#[tokio::test]
async fn run_job_command_blocks_readonly_mode() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.level = crate::security::AutonomyLevel::ReadOnly;
    let job = test_job("echo should-not-run");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.contains("read-only"));
}

#[tokio::test]
async fn run_job_command_blocks_rate_limited() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.max_actions_per_hour = 0;
    let job = test_job("echo should-not-run");
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = run_job_command(&config, &security, &job).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.contains("rate limit exceeded"));
}

#[tokio::test]
async fn execute_job_with_retry_recovers_after_first_failure() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.reliability.scheduler_retries = 1;
    config.reliability.provider_backoff_ms = 1;
    config.autonomy.allowed_commands = vec!["sh".into()];
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    tokio::fs::write(
            config.workspace_dir.join("retry-once.sh"),
            "#!/bin/sh\nif [ -f retry-ok.flag ]; then\n  echo recovered\n  exit 0\nfi\ntouch retry-ok.flag\nexit 1\n",
        )
        .await
        .unwrap();
    let job = test_job("sh ./retry-once.sh");

    let (success, output) = Box::pin(execute_job_with_retry(&config, &security, &job)).await;
    assert!(success);
    assert!(output.contains("recovered"));
}

#[tokio::test]
async fn execute_job_with_retry_exhausts_attempts() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.reliability.scheduler_retries = 1;
    config.reliability.provider_backoff_ms = 1;
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let job = test_job("ls always_missing_for_retry_test");

    let (success, output) = Box::pin(execute_job_with_retry(&config, &security, &job)).await;
    assert!(!success);
    assert!(output.contains("always_missing_for_retry_test"));
}

#[tokio::test]
async fn run_agent_job_returns_error_without_provider_key() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let mut job = test_job("");
    job.job_type = JobType::Agent;
    job.prompt = Some("Say hello".into());
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = Box::pin(run_agent_job(&config, &security, &job)).await;
    assert!(!success);
    assert!(output.contains("agent job failed:"));
}

#[tokio::test]
async fn run_agent_job_blocks_readonly_mode() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.level = crate::security::AutonomyLevel::ReadOnly;
    let mut job = test_job("");
    job.job_type = JobType::Agent;
    job.prompt = Some("Say hello".into());
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = Box::pin(run_agent_job(&config, &security, &job)).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.contains("read-only"));
}

#[tokio::test]
async fn run_agent_job_blocks_rate_limited() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.autonomy.max_actions_per_hour = 0;
    let mut job = test_job("");
    job.job_type = JobType::Agent;
    job.prompt = Some("Say hello".into());
    let security = SecurityPolicy::from_config(&config.autonomy, &config.workspace_dir);

    let (success, output) = Box::pin(run_agent_job(&config, &security, &job)).await;
    assert!(!success);
    assert!(output.contains("blocked by security policy"));
    assert!(output.contains("rate limit exceeded"));
}

#[tokio::test]
async fn process_due_jobs_marks_component_ok_even_when_idle() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let security = Arc::new(SecurityPolicy::from_config(
        &config.autonomy,
        &config.workspace_dir,
    ));
    let component = unique_component("scheduler-idle");

    crate::health::mark_component_error(&component, "pre-existing error");
    process_due_jobs(&config, &security, Vec::new(), &component, &None).await;

    let snapshot = crate::health::snapshot_json();
    let entry = &snapshot["components"][component.as_str()];
    assert_eq!(entry["status"], "ok");
    assert!(entry["last_ok"].as_str().is_some());
    assert!(entry["last_error"].is_null());
}

#[tokio::test]
async fn process_due_jobs_failure_does_not_mark_component_unhealthy() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = test_job("ls definitely_missing_file_for_scheduler_component_health_test");
    let security = Arc::new(SecurityPolicy::from_config(
        &config.autonomy,
        &config.workspace_dir,
    ));
    let component = unique_component("scheduler-fail");

    crate::health::mark_component_ok(&component);
    process_due_jobs(&config, &security, vec![job], &component, &None).await;

    let snapshot = crate::health::snapshot_json();
    let entry = &snapshot["components"][component.as_str()];
    assert_eq!(entry["status"], "ok");
}

#[tokio::test]
async fn persist_job_result_records_run_and_reschedules_shell_job() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = cron::add_job(&config, "*/5 * * * *", "echo ok").unwrap();
    let started = Utc::now();
    let finished = started + ChronoDuration::milliseconds(10);

    let success = persist_job_result(&config, &job, true, "ok", started, finished).await;
    assert!(success);

    let runs = cron::list_runs(&config, &job.id, 10).unwrap();
    assert_eq!(runs.len(), 1);
    let updated = cron::get_job(&config, &job.id).unwrap();
    assert_eq!(updated.last_status.as_deref(), Some("ok"));
}

#[tokio::test]
async fn persist_job_result_success_deletes_one_shot() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let at = Utc::now() + ChronoDuration::minutes(10);
    let job = cron::add_agent_job(
        &config,
        Some("one-shot".into()),
        crate::cron::Schedule::At { at },
        "Hello",
        SessionTarget::Isolated,
        None,
        None,
        true,
        None,
    )
    .unwrap();
    let started = Utc::now();
    let finished = started + ChronoDuration::milliseconds(10);

    let success = persist_job_result(&config, &job, true, "ok", started, finished).await;
    assert!(success);
    let lookup = cron::get_job(&config, &job.id);
    assert!(lookup.is_err());
}

#[tokio::test]
async fn persist_job_result_failure_disables_one_shot() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let at = Utc::now() + ChronoDuration::minutes(10);
    let job = cron::add_agent_job(
        &config,
        Some("one-shot".into()),
        crate::cron::Schedule::At { at },
        "Hello",
        SessionTarget::Isolated,
        None,
        None,
        true,
        None,
    )
    .unwrap();
    let started = Utc::now();
    let finished = started + ChronoDuration::milliseconds(10);

    let success = persist_job_result(&config, &job, false, "boom", started, finished).await;
    assert!(!success);
    let updated = cron::get_job(&config, &job.id).unwrap();
    assert!(!updated.enabled);
    assert_eq!(updated.last_status.as_deref(), Some("error"));
}

#[tokio::test]
async fn persist_job_result_success_deletes_one_shot_shell_job() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let at = Utc::now() + ChronoDuration::minutes(10);
    let job = cron::add_once_at(&config, at, "echo one-shot-shell").unwrap();
    assert!(job.delete_after_run);
    let started = Utc::now();
    let finished = started + ChronoDuration::milliseconds(10);

    let success = persist_job_result(&config, &job, true, "ok", started, finished).await;
    assert!(success);
    let lookup = cron::get_job(&config, &job.id);
    assert!(lookup.is_err());
}

#[tokio::test]
async fn persist_job_result_failure_disables_one_shot_shell_job() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let at = Utc::now() + ChronoDuration::minutes(10);
    let job = cron::add_once_at(&config, at, "echo one-shot-shell").unwrap();
    assert!(job.delete_after_run);
    let started = Utc::now();
    let finished = started + ChronoDuration::milliseconds(10);

    let success = persist_job_result(&config, &job, false, "boom", started, finished).await;
    assert!(!success);
    let updated = cron::get_job(&config, &job.id).unwrap();
    assert!(!updated.enabled);
    assert_eq!(updated.last_status.as_deref(), Some("error"));
}

#[tokio::test]
async fn persist_job_result_delivery_failure_non_best_effort_marks_error() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = cron::add_agent_job(
        &config,
        Some("announce-job".into()),
        crate::cron::Schedule::Cron {
            expr: "*/5 * * * *".into(),
            tz: None,
        },
        "deliver this",
        SessionTarget::Isolated,
        None,
        Some(DeliveryConfig {
            mode: "announce".into(),
            channel: Some("telegram".into()),
            to: Some("123456".into()),
            best_effort: false,
        }),
        false,
        None,
    )
    .unwrap();
    let started = Utc::now();
    let finished = started + ChronoDuration::milliseconds(10);

    let success = persist_job_result(&config, &job, true, "ok", started, finished).await;
    assert!(!success);

    let updated = cron::get_job(&config, &job.id).unwrap();
    assert!(updated.enabled);
    assert_eq!(updated.last_status.as_deref(), Some("error"));

    let runs = cron::list_runs(&config, &job.id, 10).unwrap();
    assert_eq!(runs.len(), 1);
    assert_eq!(runs[0].status, "error");
}

#[tokio::test]
async fn persist_job_result_delivery_failure_best_effort_keeps_success() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = cron::add_agent_job(
        &config,
        Some("announce-job-best-effort".into()),
        crate::cron::Schedule::Cron {
            expr: "*/5 * * * *".into(),
            tz: None,
        },
        "deliver this",
        SessionTarget::Isolated,
        None,
        Some(DeliveryConfig {
            mode: "announce".into(),
            channel: Some("telegram".into()),
            to: Some("123456".into()),
            best_effort: true,
        }),
        false,
        None,
    )
    .unwrap();
    let started = Utc::now();
    let finished = started + ChronoDuration::milliseconds(10);

    let success = persist_job_result(&config, &job, true, "ok", started, finished).await;
    assert!(success);

    let updated = cron::get_job(&config, &job.id).unwrap();
    assert!(updated.enabled);
    assert_eq!(updated.last_status.as_deref(), Some("ok"));

    let runs = cron::list_runs(&config, &job.id, 10).unwrap();
    assert_eq!(runs.len(), 1);
    assert_eq!(runs[0].status, "ok");
}

#[tokio::test]
async fn persist_job_result_at_schedule_without_delete_after_run_is_disabled() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let at = Utc::now() + ChronoDuration::minutes(10);
    let job = cron::add_agent_job(
        &config,
        Some("at-no-autodelete".into()),
        crate::cron::Schedule::At { at },
        "Hello",
        SessionTarget::Isolated,
        None,
        None,
        false,
        None,
    )
    .unwrap();
    assert!(!job.delete_after_run);

    let started = Utc::now();
    let finished = started + ChronoDuration::milliseconds(10);
    let success = persist_job_result(&config, &job, true, "ok", started, finished).await;
    assert!(success);

    // After reschedule_after_run, At schedule jobs should be disabled
    // to prevent re-execution with a past next_run timestamp.
    let updated = cron::get_job(&config, &job.id).unwrap();
    assert!(
        !updated.enabled,
        "At schedule job should be disabled after execution via reschedule"
    );
    assert_eq!(updated.last_status.as_deref(), Some("ok"));
}

#[tokio::test]
async fn deliver_if_configured_handles_none_and_invalid_channel() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let mut job = test_job("echo ok");

    assert!(deliver_if_configured(&config, &job, "x").await.is_ok());

    job.delivery = DeliveryConfig {
        mode: "announce".into(),
        channel: Some("invalid".into()),
        to: Some("target".into()),
        best_effort: true,
    };
    let err = deliver_if_configured(&config, &job, "x").await.unwrap_err();
    assert!(err.to_string().contains("unsupported delivery channel"));
}

#[test]
fn resolve_matrix_delivery_room_prefers_target_when_present() {
    assert_eq!(
        resolve_matrix_delivery_room("!default:matrix.org", "  !ops:matrix.org  "),
        "!ops:matrix.org"
    );
}

#[test]
fn resolve_matrix_delivery_room_falls_back_to_configured_room() {
    assert_eq!(
        resolve_matrix_delivery_room("  !default:matrix.org  ", "   "),
        "!default:matrix.org"
    );
}

#[test]
fn build_cron_shell_command_uses_sh_non_login() {
    let workspace = std::env::temp_dir();
    let cmd = build_cron_shell_command("echo cron-test", &workspace).unwrap();
    let debug = format!("{cmd:?}");
    assert!(debug.contains("echo cron-test"));
    assert!(debug.contains("\"sh\""), "should use sh: {debug}");
    // Must NOT use login shell (-l) — login shells load full profile
    // and are slow/unpredictable for cron jobs.
    assert!(
        !debug.contains("\"-lc\""),
        "must not use login shell: {debug}"
    );
}

#[tokio::test]
async fn build_cron_shell_command_executes_successfully() {
    let workspace = std::env::temp_dir();
    let mut cmd = build_cron_shell_command("echo cron-ok", &workspace).unwrap();
    let output = cmd.output().await.unwrap();
    assert!(output.status.success());
    let stdout = String::from_utf8_lossy(&output.stdout);
    assert!(stdout.contains("cron-ok"));
}

#[tokio::test]
async fn catch_up_queries_all_overdue_jobs_ignoring_max_tasks() {
    let tmp = TempDir::new().unwrap();
    let mut config = test_config(&tmp).await;
    config.scheduler.max_tasks = 1; // limit normal polling to 1

    // Create 3 jobs with "every minute" schedule
    for i in 0..3 {
        let _ = cron::add_job(&config, "* * * * *", &format!("echo catchup-{i}")).unwrap();
    }

    // Verify normal due_jobs is limited to max_tasks=1
    let far_future = Utc::now() + ChronoDuration::days(1);
    let due = cron::due_jobs(&config, far_future).unwrap();
    assert_eq!(due.len(), 1, "due_jobs must respect max_tasks");

    // all_overdue_jobs ignores the limit
    let overdue = cron::all_overdue_jobs(&config, far_future).unwrap();
    assert_eq!(overdue.len(), 3, "all_overdue_jobs must return all");
}

#[test]
fn scan_and_redact_output_redacts_credentials() {
    let leaked_output = "Deployment key: sk_test_FAKE1234567890abcdefgh"; // gitleaks:allow

    let redacted = scan_and_redact_output("telegram", "123456", leaked_output);

    assert!(
        !redacted.as_str().contains("sk_test_FAKE1234567890abcdefgh"), // gitleaks:allow
        "credentials must be redacted"
    );
    assert!(redacted.as_str().contains("[REDACTED"));
}

#[test]
fn scan_and_redact_output_preserves_clean_output() {
    let clean_output = "Deployment completed successfully at 2024-03-15 10:00:00";

    let redacted = scan_and_redact_output("telegram", "123456", clean_output);

    assert_eq!(redacted.as_str(), clean_output);
}

// ── Broadcast / EventBroadcast tests ─────────────────────────────

#[tokio::test]
async fn broadcast_sends_cron_result_on_success() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = test_job("echo broadcast-ok");
    let security = Arc::new(SecurityPolicy::from_config(
        &config.autonomy,
        &config.workspace_dir,
    ));
    let component = unique_component("broadcast-ok");

    let (tx, mut rx) = tokio::sync::broadcast::channel::<serde_json::Value>(16);
    let event_tx: EventBroadcast = Some(tx);

    process_due_jobs(&config, &security, vec![job], &component, &event_tx).await;

    let event = rx.try_recv().expect("should receive a broadcast event");
    assert_eq!(event["type"], "cron_result");
    assert_eq!(event["job_id"], "test-job");
    assert_eq!(event["success"], true);
    assert!(event["output"].as_str().unwrap().contains("broadcast-ok"));
    assert!(event["timestamp"].as_str().is_some());
}

#[tokio::test]
async fn broadcast_sends_cron_result_on_failure() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = test_job("ls definitely_missing_file_for_broadcast_fail_test");
    let security = Arc::new(SecurityPolicy::from_config(
        &config.autonomy,
        &config.workspace_dir,
    ));
    let component = unique_component("broadcast-fail");

    let (tx, mut rx) = tokio::sync::broadcast::channel::<serde_json::Value>(16);
    let event_tx: EventBroadcast = Some(tx);

    process_due_jobs(&config, &security, vec![job], &component, &event_tx).await;

    let event = rx.try_recv().expect("should receive a broadcast event");
    assert_eq!(event["type"], "cron_result");
    assert_eq!(event["job_id"], "test-job");
    assert_eq!(event["success"], false);
    assert!(event["timestamp"].as_str().is_some());
}

#[tokio::test]
async fn broadcast_none_skips_without_error() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = test_job("echo no-broadcast");
    let security = Arc::new(SecurityPolicy::from_config(
        &config.autonomy,
        &config.workspace_dir,
    ));
    let component = unique_component("broadcast-none");

    // event_tx = None — should complete without panic.
    process_due_jobs(&config, &security, vec![job], &component, &None).await;
}

#[tokio::test]
async fn broadcast_handles_no_subscribers() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp).await;
    let job = test_job("echo no-subscribers");
    let security = Arc::new(SecurityPolicy::from_config(
        &config.autonomy,
        &config.workspace_dir,
    ));
    let component = unique_component("broadcast-no-sub");

    let (tx, _) = tokio::sync::broadcast::channel::<serde_json::Value>(16);
    // Drop the only receiver immediately — `let _ = tx.send(...)` in
    // process_due_jobs must not panic when there are no subscribers.
    let event_tx: EventBroadcast = Some(tx);

    process_due_jobs(&config, &security, vec![job], &component, &event_tx).await;
    // If we got here without panic, the test passes.
}

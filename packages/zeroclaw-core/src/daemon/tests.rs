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

#[test]
fn state_file_path_uses_config_directory() {
    let tmp = TempDir::new().unwrap();
    let config = test_config(&tmp);

    let path = state_file_path(&config);
    assert_eq!(path, tmp.path().join("daemon_state.json"));
}

#[tokio::test]
async fn supervisor_marks_error_and_restart_on_failure() {
    let handle =
        spawn_component_supervisor("daemon-test-fail", 1, 1, || async { anyhow::bail!("boom") });

    tokio::time::sleep(Duration::from_millis(50)).await;
    handle.abort();
    let _ = handle.await;

    let snapshot = crate::health::snapshot_json();
    let component = &snapshot["components"]["daemon-test-fail"];
    assert_eq!(component["status"], "error");
    assert!(component["restart_count"].as_u64().unwrap_or(0) >= 1);
    assert!(
        component["last_error"]
            .as_str()
            .unwrap_or("")
            .contains("boom")
    );
}

#[tokio::test]
async fn supervisor_marks_unexpected_exit_as_error() {
    let handle = spawn_component_supervisor("daemon-test-exit", 1, 1, || async { Ok(()) });

    tokio::time::sleep(Duration::from_millis(50)).await;
    handle.abort();
    let _ = handle.await;

    let snapshot = crate::health::snapshot_json();
    let component = &snapshot["components"]["daemon-test-exit"];
    assert_eq!(component["status"], "error");
    assert!(component["restart_count"].as_u64().unwrap_or(0) >= 1);
    assert!(
        component["last_error"]
            .as_str()
            .unwrap_or("")
            .contains("component exited unexpectedly")
    );
}

#[test]
fn resolve_delivery_none_when_unset() {
    let config = Config::default();
    let target = resolve_heartbeat_delivery(&config).unwrap();
    assert!(target.is_none());
}

#[test]
fn resolve_delivery_requires_to_field() {
    let mut config = Config::default();
    config.heartbeat.target = Some("telegram".into());
    let err = resolve_heartbeat_delivery(&config).unwrap_err();
    assert!(
        err.to_string()
            .contains("heartbeat.to is required when heartbeat.target is set")
    );
}

#[test]
fn resolve_delivery_requires_target_field() {
    let mut config = Config::default();
    config.heartbeat.to = Some("123456".into());
    let err = resolve_heartbeat_delivery(&config).unwrap_err();
    assert!(
        err.to_string()
            .contains("heartbeat.target is required when heartbeat.to is set")
    );
}

#[test]
fn resolve_delivery_rejects_unsupported_channel() {
    let mut config = Config::default();
    config.heartbeat.target = Some("email".into());
    config.heartbeat.to = Some("ops@example.com".into());
    let err = resolve_heartbeat_delivery(&config).unwrap_err();
    assert!(
        err.to_string()
            .contains("unsupported heartbeat.target channel")
    );
}

#[test]
fn resolve_delivery_requires_channel_configuration() {
    let mut config = Config::default();
    config.heartbeat.target = Some("telegram".into());
    config.heartbeat.to = Some("123456".into());
    let err = resolve_heartbeat_delivery(&config).unwrap_err();
    assert!(
        err.to_string()
            .contains("channels_config.telegram is not configured")
    );
}

#[test]
fn resolve_delivery_accepts_telegram_configuration() {
    let mut config = Config::default();
    config.heartbeat.target = Some("telegram".into());
    config.heartbeat.to = Some("123456".into());
    config.channels_config.telegram = Some(crate::config::TelegramConfig {
        bot_token: "bot-token".into(),
        allowed_users: vec![],
        stream_mode: crate::config::StreamMode::default(),
        draft_update_interval_ms: 1000,
        interrupt_on_new_message: false,
        mention_only: false,
        ack_reactions: None,
        proxy_url: None,
    });

    let target = resolve_heartbeat_delivery(&config).unwrap();
    assert_eq!(target, Some(("telegram".to_string(), "123456".to_string())));
}

#[test]
fn auto_detect_telegram_when_configured() {
    let mut config = Config::default();
    config.channels_config.telegram = Some(crate::config::TelegramConfig {
        bot_token: "bot-token".into(),
        allowed_users: vec!["user123".into()],
        stream_mode: crate::config::StreamMode::default(),
        draft_update_interval_ms: 1000,
        interrupt_on_new_message: false,
        mention_only: false,
        ack_reactions: None,
        proxy_url: None,
    });

    let target = resolve_heartbeat_delivery(&config).unwrap();
    assert_eq!(
        target,
        Some(("telegram".to_string(), "user123".to_string()))
    );
}

#[test]
fn auto_detect_none_when_no_channels() {
    let config = Config::default();
    let target = auto_detect_heartbeat_channel(&config);
    assert!(target.is_none());
}

/// Verify that SIGHUP does not cause shutdown — the daemon should ignore it
/// and only terminate on SIGINT or SIGTERM.
#[cfg(unix)]
#[tokio::test]
async fn sighup_does_not_shut_down_daemon() {
    use libc;
    use tokio::time::{Duration, timeout};

    let handle = tokio::spawn(wait_for_shutdown_signal());

    // Give the signal handler time to register
    tokio::time::sleep(Duration::from_millis(50)).await;

    // Send SIGHUP to ourselves — should be ignored by the handler
    unsafe { libc::raise(libc::SIGHUP) };

    // The future should NOT complete within a short window
    let result = timeout(Duration::from_millis(200), handle).await;
    assert!(
        result.is_err(),
        "wait_for_shutdown_signal should not return after SIGHUP"
    );
}

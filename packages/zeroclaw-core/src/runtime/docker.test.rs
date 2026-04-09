use super::*;

#[test]
fn docker_runtime_name() {
    let runtime = DockerRuntime::new(DockerRuntimeConfig::default());
    assert_eq!(runtime.name(), "docker");
}

#[test]
fn docker_runtime_memory_budget() {
    let mut cfg = DockerRuntimeConfig::default();
    cfg.memory_limit_mb = Some(256);
    let runtime = DockerRuntime::new(cfg);
    assert_eq!(runtime.memory_budget(), 256 * 1024 * 1024);
}

#[test]
fn docker_build_shell_command_includes_runtime_flags() {
    let cfg = DockerRuntimeConfig {
        image: "alpine:3.20".into(),
        network: "none".into(),
        memory_limit_mb: Some(128),
        cpu_limit: Some(1.5),
        read_only_rootfs: true,
        mount_workspace: true,
        allowed_workspace_roots: Vec::new(),
    };
    let runtime = DockerRuntime::new(cfg);

    let workspace = std::env::temp_dir();
    let command = runtime
        .build_shell_command("echo hello", &workspace)
        .unwrap();
    let debug = format!("{command:?}");

    assert!(debug.contains("docker"));
    assert!(debug.contains("--memory"));
    assert!(debug.contains("128m"));
    assert!(debug.contains("--cpus"));
    assert!(debug.contains("1.5"));
    assert!(debug.contains("--workdir"));
    assert!(debug.contains("echo hello"));
}

#[test]
fn docker_workspace_allowlist_blocks_outside_paths() {
    let cfg = DockerRuntimeConfig {
        allowed_workspace_roots: vec!["/tmp/allowed".into()],
        ..DockerRuntimeConfig::default()
    };
    let runtime = DockerRuntime::new(cfg);

    let outside = PathBuf::from("/tmp/blocked_workspace");
    let result = runtime.build_shell_command("echo test", &outside);

    assert!(result.is_err());
}

// ── §3.3 / §3.4 Docker mount & network isolation tests ──

#[test]
fn docker_build_shell_command_includes_network_flag() {
    let cfg = DockerRuntimeConfig {
        network: "none".into(),
        ..DockerRuntimeConfig::default()
    };
    let runtime = DockerRuntime::new(cfg);
    let workspace = std::env::temp_dir();
    let cmd = runtime
        .build_shell_command("echo hello", &workspace)
        .unwrap();
    let debug = format!("{cmd:?}");
    assert!(
        debug.contains("--network") && debug.contains("none"),
        "must include --network none for isolation"
    );
}

#[test]
fn docker_build_shell_command_includes_read_only_flag() {
    let cfg = DockerRuntimeConfig {
        read_only_rootfs: true,
        ..DockerRuntimeConfig::default()
    };
    let runtime = DockerRuntime::new(cfg);
    let workspace = std::env::temp_dir();
    let cmd = runtime
        .build_shell_command("echo hello", &workspace)
        .unwrap();
    let debug = format!("{cmd:?}");
    assert!(
        debug.contains("--read-only"),
        "must include --read-only flag when read_only_rootfs is set"
    );
}

#[cfg(unix)]
#[test]
fn docker_refuses_root_mount() {
    let cfg = DockerRuntimeConfig {
        mount_workspace: true,
        ..DockerRuntimeConfig::default()
    };
    let runtime = DockerRuntime::new(cfg);
    let result = runtime.build_shell_command("echo test", Path::new("/"));
    assert!(
        result.is_err(),
        "mounting filesystem root (/) must be refused"
    );
    let error_chain = format!("{:#}", result.unwrap_err());
    assert!(
        error_chain.contains("root"),
        "expected root-mount error chain, got: {error_chain}"
    );
}

#[test]
fn docker_no_memory_flag_when_not_configured() {
    let cfg = DockerRuntimeConfig {
        memory_limit_mb: None,
        ..DockerRuntimeConfig::default()
    };
    let runtime = DockerRuntime::new(cfg);
    let workspace = std::env::temp_dir();
    let cmd = runtime
        .build_shell_command("echo hello", &workspace)
        .unwrap();
    let debug = format!("{cmd:?}");
    assert!(
        !debug.contains("--memory"),
        "should not include --memory when not configured"
    );
}

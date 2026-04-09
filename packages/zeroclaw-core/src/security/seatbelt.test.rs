use super::*;

#[test]
fn seatbelt_sandbox_name() {
    let sandbox = SeatbeltSandbox {
        policy_dir: PathBuf::from("/tmp/test-seatbelt"),
        policy_path: PathBuf::from("/tmp/test-seatbelt/test.sb"),
    };
    assert_eq!(sandbox.name(), "sandbox-exec");
}

#[test]
fn seatbelt_description_mentions_macos() {
    let sandbox = SeatbeltSandbox {
        policy_dir: PathBuf::from("/tmp/test-seatbelt"),
        policy_path: PathBuf::from("/tmp/test-seatbelt/test.sb"),
    };
    assert!(sandbox.description().contains("macOS"));
    assert!(sandbox.description().contains("Seatbelt"));
}

#[test]
fn generate_policy_contains_workspace_path() {
    let workspace = PathBuf::from("/Users/test/project");
    let policy = generate_policy(&workspace);
    assert!(policy.contains("/Users/test/project"));
}

#[test]
fn generate_policy_denies_by_default() {
    let workspace = PathBuf::from("/tmp/workspace");
    let policy = generate_policy(&workspace);
    assert!(policy.contains("(deny default)"));
}

#[test]
fn generate_policy_allows_workspace_writes() {
    let workspace = PathBuf::from("/home/user/code");
    let policy = generate_policy(&workspace);
    assert!(policy.contains("(allow file-write*"));
    assert!(policy.contains("/home/user/code"));
}

#[test]
fn generate_policy_restricts_network() {
    let workspace = PathBuf::from("/tmp/workspace");
    let policy = generate_policy(&workspace);
    assert!(policy.contains("localhost"));
    assert!(!policy.contains("127.0.0.1"));
    assert!(!policy.contains("(allow network*)"));
}

#[test]
fn generate_policy_allows_system_reads() {
    let workspace = PathBuf::from("/tmp/workspace");
    let policy = generate_policy(&workspace);
    assert!(policy.contains("(subpath \"/usr\")"));
    assert!(policy.contains("(subpath \"/bin\")"));
    assert!(policy.contains("(subpath \"/System\")"));
}

#[test]
fn generate_policy_allows_process_execution() {
    let workspace = PathBuf::from("/tmp/workspace");
    let policy = generate_policy(&workspace);
    assert!(policy.contains("(allow process-exec)"));
    assert!(policy.contains("(allow process-fork)"));
}

#[test]
fn seatbelt_wrap_command_prepends_sandbox_exec() {
    let dir = tempfile::tempdir().unwrap();
    let policy_path = dir.path().join("test.sb");
    std::fs::write(&policy_path, "(version 1)\n(deny default)").unwrap();

    let sandbox = SeatbeltSandbox {
        policy_dir: dir.path().to_path_buf(),
        policy_path: policy_path.clone(),
    };

    let mut cmd = Command::new("echo");
    cmd.arg("hello");
    sandbox.wrap_command(&mut cmd).unwrap();

    assert_eq!(cmd.get_program().to_string_lossy(), "sandbox-exec");
    let args: Vec<String> = cmd
        .get_args()
        .map(|s| s.to_string_lossy().to_string())
        .collect();
    assert!(args.contains(&"-f".to_string()));
    assert!(args.contains(&policy_path.to_string_lossy().to_string()));
    assert!(args.contains(&"echo".to_string()));
    assert!(args.contains(&"hello".to_string()));
}

#[test]
fn seatbelt_wrap_command_preserves_original_args() {
    let dir = tempfile::tempdir().unwrap();
    let policy_path = dir.path().join("test.sb");
    std::fs::write(&policy_path, "(version 1)").unwrap();

    let sandbox = SeatbeltSandbox {
        policy_dir: dir.path().to_path_buf(),
        policy_path,
    };

    let mut cmd = Command::new("ls");
    cmd.arg("-la");
    cmd.arg("/workspace");
    sandbox.wrap_command(&mut cmd).unwrap();

    let args: Vec<String> = cmd
        .get_args()
        .map(|s| s.to_string_lossy().to_string())
        .collect();

    assert!(
        args.contains(&"ls".to_string()),
        "original program must be passed as argument"
    );
    assert!(
        args.contains(&"-la".to_string()),
        "original args must be preserved"
    );
    assert!(
        args.contains(&"/workspace".to_string()),
        "original args must be preserved"
    );
}

#[test]
fn seatbelt_policy_file_cleanup_on_drop() {
    let dir = tempfile::tempdir().unwrap();
    let policy_path = dir.path().join("session.sb");
    std::fs::write(&policy_path, "(version 1)").unwrap();
    assert!(policy_path.exists());

    {
        let _sandbox = SeatbeltSandbox {
            policy_dir: dir.path().to_path_buf(),
            policy_path: policy_path.clone(),
        };
    }

    assert!(
        !policy_path.exists(),
        "policy file should be cleaned up on drop"
    );
}

#[test]
fn seatbelt_new_fails_if_not_installed() {
    let result = SeatbeltSandbox::new();
    match result {
        Ok(sandbox) => {
            assert_eq!(sandbox.name(), "sandbox-exec");
            assert!(sandbox.policy_path().exists());
        }
        Err(e) => {
            assert!(
                e.kind() == std::io::ErrorKind::NotFound
                    || e.kind() == std::io::ErrorKind::PermissionDenied
            );
        }
    }
}

#[test]
fn seatbelt_is_available_checks_policy_file() {
    let dir = tempfile::tempdir().unwrap();
    let policy_path = dir.path().join("test.sb");

    let sandbox = SeatbeltSandbox {
        policy_dir: dir.path().to_path_buf(),
        policy_path: policy_path.clone(),
    };

    if Path::new("/usr/bin/sandbox-exec").exists() {
        assert!(
            !sandbox.is_available(),
            "should be false without policy file"
        );
    }

    std::fs::write(&policy_path, "(version 1)").unwrap();
    if Path::new("/usr/bin/sandbox-exec").exists() {
        assert!(sandbox.is_available(), "should be true with policy file");
    }
}

#[test]
fn generate_policy_is_valid_sb_format() {
    let workspace = PathBuf::from("/tmp/workspace");
    let policy = generate_policy(&workspace);
    assert!(policy.starts_with("(version 1)"));
    let open = policy.chars().filter(|c| *c == '(').count();
    let close = policy.chars().filter(|c| *c == ')').count();
    assert_eq!(open, close, "parentheses must be balanced in .sb policy");
}

use super::*;

#[test]
fn builtin_playbooks_are_valid() {
    let playbooks = builtin_playbooks();
    assert_eq!(playbooks.len(), 4);

    let names: Vec<&str> = playbooks.iter().map(|p| p.name.as_str()).collect();
    assert!(names.contains(&"suspicious_login"));
    assert!(names.contains(&"malware_detected"));
    assert!(names.contains(&"data_exfiltration_attempt"));
    assert!(names.contains(&"brute_force"));

    for pb in &playbooks {
        assert!(!pb.steps.is_empty(), "Playbook {} has no steps", pb.name);
        assert!(!pb.description.is_empty());
    }
}

#[test]
fn severity_level_ordering() {
    assert!(severity_level("low") < severity_level("medium"));
    assert!(severity_level("medium") < severity_level("high"));
    assert!(severity_level("high") < severity_level("critical"));
    assert_eq!(severity_level("unknown"), u8::MAX);
}

#[test]
fn auto_approve_respects_severity_cap() {
    let pb = &builtin_playbooks()[0]; // suspicious_login

    // Step 0 is in auto_approve_steps
    assert!(can_auto_approve(pb, 0, "low", "low"));
    assert!(can_auto_approve(pb, 0, "low", "medium"));

    // Alert severity exceeds max -> cannot auto-approve
    assert!(!can_auto_approve(pb, 0, "high", "low"));
    assert!(!can_auto_approve(pb, 0, "critical", "medium"));

    // Step 2 is NOT in auto_approve_steps
    assert!(!can_auto_approve(pb, 2, "low", "critical"));
}

#[test]
fn evaluate_step_requires_approval() {
    let pb = &builtin_playbooks()[0]; // suspicious_login

    // Step 2 (notify_user) requires approval, high severity, max=low -> pending
    let result = evaluate_step(pb, 2, "high", "low", true);
    assert_eq!(result.status, StepStatus::PendingApproval);
    assert_eq!(result.action, "notify_user");

    // Step 0 (gather_login_context) does NOT require approval -> completed
    let result = evaluate_step(pb, 0, "high", "low", true);
    assert_eq!(result.status, StepStatus::Completed);
}

#[test]
fn evaluate_step_out_of_range() {
    let pb = &builtin_playbooks()[0];
    let result = evaluate_step(pb, 99, "low", "low", true);
    assert_eq!(result.status, StepStatus::Failed);
}

#[test]
fn playbook_json_roundtrip() {
    let pb = &builtin_playbooks()[0];
    let json = serde_json::to_string(pb).unwrap();
    let parsed: Playbook = serde_json::from_str(&json).unwrap();
    assert_eq!(parsed, *pb);
}

#[test]
fn load_playbooks_from_nonexistent_dir_returns_builtins() {
    let playbooks = load_playbooks(Path::new("/nonexistent/dir"));
    assert_eq!(playbooks.len(), 4);
}

#[test]
fn load_playbooks_merges_custom_and_builtin() {
    let dir = tempfile::tempdir().unwrap();
    let custom = Playbook {
        name: "custom_playbook".into(),
        description: "A custom playbook".into(),
        steps: vec![PlaybookStep {
            action: "custom_action".into(),
            description: "Do something custom".into(),
            requires_approval: true,
            timeout_secs: 60,
        }],
        severity_filter: "low".into(),
        auto_approve_steps: vec![],
    };
    let json = serde_json::to_string(&custom).unwrap();
    std::fs::write(dir.path().join("custom.json"), json).unwrap();

    let playbooks = load_playbooks(dir.path());
    // 4 builtins + 1 custom
    assert_eq!(playbooks.len(), 5);
    assert!(playbooks.iter().any(|p| p.name == "custom_playbook"));
}

#[test]
fn load_playbooks_custom_overrides_builtin() {
    let dir = tempfile::tempdir().unwrap();
    let override_pb = Playbook {
        name: "suspicious_login".into(),
        description: "Custom override".into(),
        steps: vec![PlaybookStep {
            action: "custom_step".into(),
            description: "Overridden step".into(),
            requires_approval: false,
            timeout_secs: 30,
        }],
        severity_filter: "low".into(),
        auto_approve_steps: vec![0],
    };
    let json = serde_json::to_string(&override_pb).unwrap();
    std::fs::write(dir.path().join("suspicious_login.json"), json).unwrap();

    let playbooks = load_playbooks(dir.path());
    // 3 remaining builtins + 1 overridden = 4
    assert_eq!(playbooks.len(), 4);
    let sl = playbooks
        .iter()
        .find(|p| p.name == "suspicious_login")
        .unwrap();
    assert_eq!(sl.description, "Custom override");
}

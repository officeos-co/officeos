use super::*;

fn setup_workspace(files: &[(&str, &str)]) -> PathBuf {
    let dir = std::env::temp_dir().join(format!(
        "zeroclaw_personality_test_{}",
        uuid::Uuid::new_v4()
    ));
    std::fs::create_dir_all(&dir).unwrap();
    for (name, content) in files {
        std::fs::write(dir.join(name), content).unwrap();
    }
    dir
}

#[test]
fn load_personality_reads_existing_files() {
    let ws = setup_workspace(&[
        ("SOUL.md", "I am a helpful assistant."),
        ("IDENTITY.md", "Name: Nova"),
    ]);

    let profile = load_personality(&ws);
    assert_eq!(profile.files.len(), 2);
    assert_eq!(profile.get("SOUL.md").unwrap(), "I am a helpful assistant.");
    assert_eq!(profile.get("IDENTITY.md").unwrap(), "Name: Nova");
    assert!(!profile.is_empty());

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn load_personality_records_missing_files() {
    let ws = setup_workspace(&[("SOUL.md", "soul content")]);

    let profile = load_personality(&ws);
    assert_eq!(profile.files.len(), 1);
    assert!(profile.missing.contains(&"IDENTITY.md".to_string()));
    assert!(profile.missing.contains(&"USER.md".to_string()));

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn load_personality_treats_empty_files_as_missing() {
    let ws = setup_workspace(&[("SOUL.md", "   \n  ")]);

    let profile = load_personality(&ws);
    assert!(profile.is_empty());
    assert!(profile.missing.contains(&"SOUL.md".to_string()));

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn load_personality_truncates_large_files() {
    let large = "x".repeat(MAX_FILE_CHARS + 500);
    let ws = setup_workspace(&[("SOUL.md", &large)]);

    let profile = load_personality(&ws);
    let soul = profile.files.iter().find(|f| f.name == "SOUL.md").unwrap();
    assert!(soul.truncated);
    assert_eq!(soul.content.chars().count(), MAX_FILE_CHARS);

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn render_produces_markdown_sections() {
    let ws = setup_workspace(&[("SOUL.md", "Be kind."), ("IDENTITY.md", "Name: Nova")]);

    let profile = load_personality(&ws);
    let rendered = profile.render();
    assert!(rendered.contains("### SOUL.md"));
    assert!(rendered.contains("Be kind."));
    assert!(rendered.contains("### IDENTITY.md"));
    assert!(rendered.contains("Name: Nova"));

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn render_truncated_file_shows_notice() {
    let large = "y".repeat(MAX_FILE_CHARS + 100);
    let ws = setup_workspace(&[("SOUL.md", &large)]);

    let profile = load_personality(&ws);
    let rendered = profile.render();
    assert!(rendered.contains("[... truncated at"));

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn get_returns_none_for_missing_file() {
    let ws = setup_workspace(&[]);
    let profile = load_personality(&ws);
    assert!(profile.get("SOUL.md").is_none());
    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn load_personality_files_custom_subset() {
    let ws = setup_workspace(&[("SOUL.md", "soul"), ("USER.md", "user")]);

    let profile = load_personality_files(&ws, &["SOUL.md", "USER.md"]);
    assert_eq!(profile.files.len(), 2);
    assert!(profile.missing.is_empty());

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn empty_workspace_yields_empty_profile() {
    let ws = setup_workspace(&[]);
    let profile = load_personality(&ws);
    assert!(profile.is_empty());
    assert!(!profile.missing.is_empty());
    let _ = std::fs::remove_dir_all(ws);
}

// ────────────────────────────────────────────────────────────────────
// load_personality_strict — Phase 3 boot path
// ────────────────────────────────────────────────────────────────────

fn all_required<'a>(extra: &[(&'a str, &'a str)]) -> Vec<(&'a str, &'a str)> {
    let mut files: Vec<(&'a str, &'a str)> = vec![
        ("SOUL.md", "I am the agent's soul."),
        ("IDENTITY.md", "Name: Nova"),
        ("AGENTS.md", "Coordinate with the orchestrator."),
    ];
    files.extend_from_slice(extra);
    files
}

#[test]
fn strict_loads_when_all_required_files_present() {
    let ws = setup_workspace(&all_required(&[]));

    let profile = load_personality_strict(&ws).expect("strict load should succeed");
    assert_eq!(profile.files.len(), 3);
    assert!(profile.get("SOUL.md").is_some());
    assert!(profile.get("IDENTITY.md").is_some());
    assert!(profile.get("AGENTS.md").is_some());

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn strict_includes_optional_files_when_present() {
    let ws = setup_workspace(&all_required(&[
        ("USER.md", "User: harrokrog"),
        ("HEARTBEAT.md", "Beat regularly."),
    ]));

    let profile = load_personality_strict(&ws).expect("strict load should succeed");
    assert!(profile.get("USER.md").is_some());
    assert!(profile.get("HEARTBEAT.md").is_some());
    assert_eq!(profile.files.len(), 5);

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn strict_records_optional_missing_in_profile() {
    let ws = setup_workspace(&all_required(&[]));

    let profile = load_personality_strict(&ws).expect("strict load should succeed");
    // The optional files (USER.md, TOOLS.md, HEARTBEAT.md, BOOTSTRAP.md, MEMORY.md)
    // are absent and should be tracked in `missing`.
    for optional in &[
        "USER.md",
        "TOOLS.md",
        "HEARTBEAT.md",
        "BOOTSTRAP.md",
        "MEMORY.md",
    ] {
        assert!(
            profile.missing.contains(&optional.to_string()),
            "expected {optional} in missing list"
        );
    }
    // Required files must NOT appear in the missing list.
    for required in REQUIRED_PERSONALITY_FILES {
        assert!(
            !profile.missing.contains(&required.to_string()),
            "{required} should not be in missing list"
        );
    }

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn strict_fails_when_soul_md_missing() {
    let ws = setup_workspace(&[
        ("IDENTITY.md", "Name: Nova"),
        ("AGENTS.md", "Coordinate with the orchestrator."),
    ]);

    let err = load_personality_strict(&ws).unwrap_err();
    match err {
        PersonalityError::Missing(name) => assert_eq!(name, "SOUL.md"),
        other => panic!("expected Missing(SOUL.md), got {other:?}"),
    }

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn strict_fails_when_identity_md_missing() {
    let ws = setup_workspace(&[
        ("SOUL.md", "I am the agent's soul."),
        ("AGENTS.md", "Coordinate with the orchestrator."),
    ]);

    let err = load_personality_strict(&ws).unwrap_err();
    match err {
        PersonalityError::Missing(name) => assert_eq!(name, "IDENTITY.md"),
        other => panic!("expected Missing(IDENTITY.md), got {other:?}"),
    }

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn strict_fails_when_agents_md_missing() {
    let ws = setup_workspace(&[
        ("SOUL.md", "I am the agent's soul."),
        ("IDENTITY.md", "Name: Nova"),
    ]);

    let err = load_personality_strict(&ws).unwrap_err();
    match err {
        PersonalityError::Missing(name) => assert_eq!(name, "AGENTS.md"),
        other => panic!("expected Missing(AGENTS.md), got {other:?}"),
    }

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn strict_fails_when_required_file_is_empty() {
    let ws = setup_workspace(&[
        ("SOUL.md", "   \n\t  "),
        ("IDENTITY.md", "Name: Nova"),
        ("AGENTS.md", "Coordinate."),
    ]);

    let err = load_personality_strict(&ws).unwrap_err();
    match err {
        PersonalityError::Empty(name) => assert_eq!(name, "SOUL.md"),
        other => panic!("expected Empty(SOUL.md), got {other:?}"),
    }

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn strict_fails_with_empty_workspace() {
    let ws = setup_workspace(&[]);

    let err = load_personality_strict(&ws).unwrap_err();
    // First required file checked is SOUL.md.
    match err {
        PersonalityError::Missing(name) => assert_eq!(name, "SOUL.md"),
        other => panic!("expected Missing(SOUL.md), got {other:?}"),
    }

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn strict_optional_empty_files_become_missing_not_error() {
    // USER.md is optional; an empty USER.md should not abort the boot.
    let ws = setup_workspace(&[
        ("SOUL.md", "soul"),
        ("IDENTITY.md", "ident"),
        ("AGENTS.md", "agents"),
        ("USER.md", "   "),
    ]);

    let profile = load_personality_strict(&ws).expect("strict load should succeed");
    assert!(profile.missing.contains(&"USER.md".to_string()));
    // The required files are still loaded.
    assert_eq!(profile.files.len(), 3);

    let _ = std::fs::remove_dir_all(ws);
}

#[test]
fn personality_error_display_messages_are_descriptive() {
    let missing = PersonalityError::Missing("SOUL.md".into());
    assert!(format!("{missing}").contains("SOUL.md"));
    assert!(format!("{missing}").contains("missing"));

    let empty = PersonalityError::Empty("IDENTITY.md".into());
    assert!(format!("{empty}").contains("IDENTITY.md"));
    assert!(format!("{empty}").contains("empty"));
}

#[test]
fn required_files_subset_of_personality_files() {
    // Sanity check: every required file must also be in the full roster.
    for required in REQUIRED_PERSONALITY_FILES {
        assert!(
            PERSONALITY_FILES.contains(required),
            "{required} is required but not in PERSONALITY_FILES"
        );
    }
}

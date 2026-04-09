use super::*;

// ── Validation ──────────────────────────────────────────

#[test]
fn validate_empty_content_rejected() {
    assert!(validate_skill_content("").is_err());
    assert!(validate_skill_content("   \n  ").is_err());
}

#[test]
fn validate_malformed_toml_rejected() {
    assert!(validate_skill_content("not valid toml {{").is_err());
}

#[test]
fn validate_missing_name_rejected() {
    let content = r#"
[skill]
description = "no name field"
version = "0.1.0"
"#;
    assert!(validate_skill_content(content).is_err());
}

#[test]
fn validate_valid_content_accepted() {
    let content = r#"
[skill]
name = "test-skill"
description = "A test skill"
version = "0.1.0"
"#;
    assert!(validate_skill_content(content).is_ok());
}

// ── Cooldown enforcement ────────────────────────────────

#[test]
fn cooldown_allows_first_improvement() {
    let improver = SkillImprover::new(
        PathBuf::from("/tmp/test"),
        SkillImprovementConfig {
            enabled: true,
            cooldown_secs: 3600,
        },
    );
    assert!(improver.should_improve_skill("test-skill"));
}

#[test]
fn cooldown_blocks_recent_improvement() {
    let mut improver = SkillImprover::new(
        PathBuf::from("/tmp/test"),
        SkillImprovementConfig {
            enabled: true,
            cooldown_secs: 3600,
        },
    );
    improver
        .cooldowns
        .insert("test-skill".to_string(), Instant::now());
    assert!(!improver.should_improve_skill("test-skill"));
}

#[test]
fn cooldown_disabled_blocks_all() {
    let improver = SkillImprover::new(
        PathBuf::from("/tmp/test"),
        SkillImprovementConfig {
            enabled: false,
            cooldown_secs: 0,
        },
    );
    assert!(!improver.should_improve_skill("test-skill"));
}

// ── Atomic write ────────────────────────────────────────

#[tokio::test]
async fn improve_skill_atomic_write() {
    let dir = tempfile::tempdir().unwrap();
    let skill_dir = dir.path().join("skills").join("test-skill");
    tokio::fs::create_dir_all(&skill_dir).await.unwrap();

    let original = r#"[skill]
name = "test-skill"
description = "Original description"
version = "0.1.0"
author = "zeroclaw-auto"
tags = ["auto-generated"]
"#;
    tokio::fs::write(skill_dir.join("SKILL.toml"), original)
        .await
        .unwrap();

    let mut improver = SkillImprover::new(
        dir.path().to_path_buf(),
        SkillImprovementConfig {
            enabled: true,
            cooldown_secs: 0,
        },
    );

    let improved = r#"[skill]
name = "test-skill"
description = "Improved description with better steps"
version = "0.1.1"
author = "zeroclaw-auto"
tags = ["auto-generated", "improved"]
"#;

    let result = improver
        .improve_skill("test-skill", improved, "Added better step descriptions")
        .await
        .unwrap();
    assert_eq!(result, Some("test-skill".to_string()));

    // Verify the file was updated.
    let content = tokio::fs::read_to_string(skill_dir.join("SKILL.toml"))
        .await
        .unwrap();
    assert!(content.contains("Improved description"));
    assert!(content.contains("updated_at"));
    assert!(content.contains("improvement_reason"));

    // Verify temp file was cleaned up.
    assert!(!skill_dir.join(".SKILL.toml.tmp").exists());
}

#[tokio::test]
async fn improve_skill_invalid_content_aborts() {
    let dir = tempfile::tempdir().unwrap();
    let skill_dir = dir.path().join("skills").join("test-skill");
    tokio::fs::create_dir_all(&skill_dir).await.unwrap();

    let original = r#"[skill]
name = "test-skill"
description = "Original"
version = "0.1.0"
"#;
    tokio::fs::write(skill_dir.join("SKILL.toml"), original)
        .await
        .unwrap();

    let mut improver = SkillImprover::new(
        dir.path().to_path_buf(),
        SkillImprovementConfig {
            enabled: true,
            cooldown_secs: 0,
        },
    );

    // Empty content should fail validation.
    let result = improver
        .improve_skill("test-skill", "", "bad improvement")
        .await;
    assert!(result.is_err());

    // Original file should be untouched.
    let content = tokio::fs::read_to_string(skill_dir.join("SKILL.toml"))
        .await
        .unwrap();
    assert!(content.contains("Original"));
}

#[tokio::test]
async fn improve_skill_cooldown_returns_none() {
    let dir = tempfile::tempdir().unwrap();
    let skill_dir = dir.path().join("skills").join("test-skill");
    tokio::fs::create_dir_all(&skill_dir).await.unwrap();
    tokio::fs::write(
        skill_dir.join("SKILL.toml"),
        "[skill]\nname = \"test-skill\"\n",
    )
    .await
    .unwrap();

    let mut improver = SkillImprover::new(
        dir.path().to_path_buf(),
        SkillImprovementConfig {
            enabled: true,
            cooldown_secs: 9999,
        },
    );
    // Record a recent cooldown.
    improver
        .cooldowns
        .insert("test-skill".to_string(), Instant::now());

    let result = improver
        .improve_skill(
            "test-skill",
            "[skill]\nname = \"test-skill\"\ndescription = \"better\"\n",
            "test",
        )
        .await
        .unwrap();
    assert!(result.is_none());
}

// ── Metadata appending ──────────────────────────────────

#[test]
fn append_metadata_adds_fields() {
    let content = r#"[skill]
name = "test"
description = "A skill"
version = "0.1.0"
"#;
    let result = append_improvement_metadata(content, "2026-01-01T00:00:00Z", "Better steps");
    assert!(result.contains("updated_at = \"2026-01-01T00:00:00Z\""));
    assert!(result.contains("improvement_reason = \"Better steps\""));
}

#[test]
fn append_metadata_preserves_tools() {
    let content = r#"[skill]
name = "test"
description = "A skill"
version = "0.1.0"

[[tools]]
name = "action"
kind = "shell"
command = "echo hello"
"#;
    let result = append_improvement_metadata(content, "2026-01-01T00:00:00Z", "Improved");
    assert!(result.contains("[[tools]]"));
    assert!(result.contains("echo hello"));
}

// ── Audit trail extraction ──────────────────────────────

#[test]
fn extract_audit_trail_from_content() {
    let content = r#"[skill]
name = "test"
# Improvement: 2026-01-01T00:00:00Z
# Reason: First improvement
# Improvement: 2026-02-01T00:00:00Z
# Reason: Second improvement
"#;
    let trail = extract_audit_trail(content);
    assert!(trail.contains("First improvement"));
    assert!(trail.contains("Second improvement"));
    assert_eq!(trail.lines().count(), 4);
}

#[test]
fn extract_audit_trail_empty_when_none() {
    let content = "[skill]\nname = \"test\"\n";
    let trail = extract_audit_trail(content);
    assert!(trail.is_empty());
}

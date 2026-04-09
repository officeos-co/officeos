use super::*;
use crate::memory::embeddings::{EmbeddingProvider, NoopEmbedding};
use async_trait::async_trait;

// ── Slug generation ──────────────────────────────────────────

#[test]
fn slug_basic() {
    assert_eq!(
        SkillCreator::generate_slug("Deploy to production"),
        "deploy-to-production"
    );
}

#[test]
fn slug_special_characters() {
    assert_eq!(
        SkillCreator::generate_slug("Build & test (CI/CD) pipeline!"),
        "build-test-ci-cd-pipeline"
    );
}

#[test]
fn slug_max_length() {
    let long_desc = "a".repeat(100);
    let slug = SkillCreator::generate_slug(&long_desc);
    assert!(slug.len() <= 64);
}

#[test]
fn slug_leading_trailing_hyphens() {
    let slug = SkillCreator::generate_slug("---hello world---");
    assert!(!slug.starts_with('-'));
    assert!(!slug.ends_with('-'));
}

#[test]
fn slug_consecutive_spaces() {
    assert_eq!(SkillCreator::generate_slug("hello    world"), "hello-world");
}

#[test]
fn slug_empty_input() {
    let slug = SkillCreator::generate_slug("");
    assert!(slug.is_empty());
}

#[test]
fn slug_only_symbols() {
    let slug = SkillCreator::generate_slug("!@#$%^&*()");
    assert!(slug.is_empty());
}

#[test]
fn slug_unicode() {
    let slug = SkillCreator::generate_slug("Deploy cafe app");
    assert_eq!(slug, "deploy-cafe-app");
}

// ── Slug validation ──────────────────────────────────────────

#[test]
fn validate_slug_valid() {
    assert!(SkillCreator::validate_slug("deploy-to-production"));
    assert!(SkillCreator::validate_slug("a"));
    assert!(SkillCreator::validate_slug("abc123"));
}

#[test]
fn validate_slug_invalid() {
    assert!(!SkillCreator::validate_slug(""));
    assert!(!SkillCreator::validate_slug("-starts-with-hyphen"));
    assert!(!SkillCreator::validate_slug("ends-with-hyphen-"));
    assert!(!SkillCreator::validate_slug("has spaces"));
    assert!(!SkillCreator::validate_slug("has_underscores"));
    assert!(!SkillCreator::validate_slug(&"a".repeat(65)));
}

// ── TOML generation ──────────────────────────────────────────

#[test]
fn toml_generation_valid_format() {
    let calls = vec![
        ToolCallRecord {
            name: "shell".into(),
            args: serde_json::json!({"command": "cargo build"}),
        },
        ToolCallRecord {
            name: "shell".into(),
            args: serde_json::json!({"command": "cargo test"}),
        },
    ];
    let toml_str =
        SkillCreator::generate_skill_toml("build-and-test", "Build and test the project", &calls);

    // Should parse as valid TOML.
    let parsed: toml::Value = toml::from_str(&toml_str).expect("Generated TOML should be valid");
    let skill = parsed.get("skill").expect("Should have [skill] section");
    assert_eq!(
        skill.get("name").and_then(toml::Value::as_str),
        Some("build-and-test")
    );
    assert_eq!(
        skill.get("author").and_then(toml::Value::as_str),
        Some("zeroclaw-auto")
    );
    assert_eq!(
        skill.get("version").and_then(toml::Value::as_str),
        Some("0.1.0")
    );

    let tools = parsed.get("tools").and_then(toml::Value::as_array).unwrap();
    assert_eq!(tools.len(), 2);
    assert_eq!(
        tools[0].get("command").and_then(toml::Value::as_str),
        Some("cargo build")
    );
}

#[test]
fn toml_generation_escapes_quotes() {
    let calls = vec![ToolCallRecord {
        name: "shell".into(),
        args: serde_json::json!({"command": "echo \"hello\""}),
    }];
    let toml_str =
        SkillCreator::generate_skill_toml("echo-test", "Test \"quoted\" description", &calls);
    let parsed: toml::Value = toml::from_str(&toml_str).expect("TOML with quotes should be valid");
    let desc = parsed
        .get("skill")
        .and_then(|s| s.get("description"))
        .and_then(toml::Value::as_str)
        .unwrap();
    assert!(desc.contains("quoted"));
}

#[test]
fn toml_generation_no_command_arg() {
    let calls = vec![ToolCallRecord {
        name: "memory_store".into(),
        args: serde_json::json!({"key": "foo", "value": "bar"}),
    }];
    let toml_str = SkillCreator::generate_skill_toml("memory-op", "Store to memory", &calls);
    let parsed: toml::Value = toml::from_str(&toml_str).expect("TOML should be valid");
    let tools = parsed.get("tools").and_then(toml::Value::as_array).unwrap();
    // When no "command" arg exists, falls back to tool name.
    assert_eq!(
        tools[0].get("command").and_then(toml::Value::as_str),
        Some("memory_store")
    );
}

// ── TOML description extraction ──────────────────────────────

#[test]
fn extract_description_from_valid_toml() {
    let content = r#"
[skill]
name = "test"
description = "Auto-generated: Build project"
version = "0.1.0"
"#;
    assert_eq!(
        extract_description_from_toml(content),
        Some("Auto-generated: Build project".into())
    );
}

#[test]
fn extract_description_from_invalid_toml() {
    assert_eq!(extract_description_from_toml("not valid toml {{"), None);
}

// ── Deduplication ────────────────────────────────────────────

/// A mock embedding provider that returns deterministic embeddings.
///
/// The "new" description (first text embedded) always gets `[1, 0, 0]`.
/// The "existing" skill description (second text embedded) gets a vector
/// whose cosine similarity with `[1, 0, 0]` equals `self.similarity`.
struct MockEmbeddingProvider {
    similarity: f32,
    call_count: std::sync::atomic::AtomicUsize,
}

impl MockEmbeddingProvider {
    fn new(similarity: f32) -> Self {
        Self {
            similarity,
            call_count: std::sync::atomic::AtomicUsize::new(0),
        }
    }
}

#[async_trait]
impl EmbeddingProvider for MockEmbeddingProvider {
    fn name(&self) -> &str {
        "mock"
    }
    fn dimensions(&self) -> usize {
        3
    }
    async fn embed(&self, texts: &[&str]) -> anyhow::Result<Vec<Vec<f32>>> {
        Ok(texts
            .iter()
            .map(|_| {
                let call = self
                    .call_count
                    .fetch_add(1, std::sync::atomic::Ordering::Relaxed);
                if call == 0 {
                    // First call: the "new" description.
                    vec![1.0, 0.0, 0.0]
                } else {
                    // Subsequent calls: existing skill descriptions.
                    // Produce a vector with the configured cosine similarity to [1,0,0].
                    vec![
                        self.similarity,
                        (1.0 - self.similarity * self.similarity).sqrt(),
                        0.0,
                    ]
                }
            })
            .collect())
    }
}

#[tokio::test]
async fn dedup_skips_similar_descriptions() {
    let dir = tempfile::tempdir().unwrap();
    let skills_dir = dir.path().join("skills").join("existing-skill");
    tokio::fs::create_dir_all(&skills_dir).await.unwrap();
    tokio::fs::write(
        skills_dir.join("SKILL.toml"),
        r#"
[skill]
name = "existing-skill"
description = "Auto-generated: Build the project"
version = "0.1.0"
author = "zeroclaw-auto"
tags = ["auto-generated"]
"#,
    )
    .await
    .unwrap();

    let config = SkillCreationConfig {
        enabled: true,
        max_skills: 500,
        similarity_threshold: 0.85,
    };

    // High similarity provider -> should detect as duplicate.
    let provider = MockEmbeddingProvider::new(0.95);
    let creator = SkillCreator::new(dir.path().to_path_buf(), config.clone());
    assert!(
        creator
            .is_duplicate("Build the project", &provider)
            .await
            .unwrap()
    );

    // Low similarity provider -> not a duplicate.
    let provider_low = MockEmbeddingProvider::new(0.3);
    let creator2 = SkillCreator::new(dir.path().to_path_buf(), config);
    assert!(
        !creator2
            .is_duplicate("Completely different task", &provider_low)
            .await
            .unwrap()
    );
}

// ── LRU eviction ─────────────────────────────────────────────

#[tokio::test]
async fn lru_eviction_removes_oldest() {
    let dir = tempfile::tempdir().unwrap();
    let config = SkillCreationConfig {
        enabled: true,
        max_skills: 2,
        similarity_threshold: 0.85,
    };

    let skills_dir = dir.path().join("skills");

    // Create two auto-generated skills with different timestamps.
    for (i, name) in ["old-skill", "new-skill"].iter().enumerate() {
        let skill_dir = skills_dir.join(name);
        tokio::fs::create_dir_all(&skill_dir).await.unwrap();
        tokio::fs::write(
            skill_dir.join("SKILL.toml"),
            format!(
                r#"[skill]
name = "{name}"
description = "Auto-generated: Skill {i}"
version = "0.1.0"
author = "zeroclaw-auto"
tags = ["auto-generated"]
"#
            ),
        )
        .await
        .unwrap();
        // Small delay to ensure different timestamps.
        tokio::time::sleep(std::time::Duration::from_millis(50)).await;
    }

    let creator = SkillCreator::new(dir.path().to_path_buf(), config);
    creator.enforce_lru_limit().await.unwrap();

    // The oldest skill should have been removed.
    assert!(!skills_dir.join("old-skill").exists());
    assert!(skills_dir.join("new-skill").exists());
}

// ── End-to-end: create_from_execution ────────────────────────

#[tokio::test]
async fn create_from_execution_disabled() {
    let dir = tempfile::tempdir().unwrap();
    let config = SkillCreationConfig {
        enabled: false,
        ..Default::default()
    };
    let creator = SkillCreator::new(dir.path().to_path_buf(), config);
    let calls = vec![
        ToolCallRecord {
            name: "shell".into(),
            args: serde_json::json!({"command": "ls"}),
        },
        ToolCallRecord {
            name: "shell".into(),
            args: serde_json::json!({"command": "pwd"}),
        },
    ];
    let result = creator
        .create_from_execution("List files", &calls, None)
        .await
        .unwrap();
    assert!(result.is_none());
}

#[tokio::test]
async fn create_from_execution_insufficient_steps() {
    let dir = tempfile::tempdir().unwrap();
    let config = SkillCreationConfig {
        enabled: true,
        ..Default::default()
    };
    let creator = SkillCreator::new(dir.path().to_path_buf(), config);
    let calls = vec![ToolCallRecord {
        name: "shell".into(),
        args: serde_json::json!({"command": "ls"}),
    }];
    let result = creator
        .create_from_execution("List files", &calls, None)
        .await
        .unwrap();
    assert!(result.is_none());
}

#[tokio::test]
async fn create_from_execution_success() {
    let dir = tempfile::tempdir().unwrap();
    let config = SkillCreationConfig {
        enabled: true,
        max_skills: 500,
        similarity_threshold: 0.85,
    };
    let creator = SkillCreator::new(dir.path().to_path_buf(), config);
    let calls = vec![
        ToolCallRecord {
            name: "shell".into(),
            args: serde_json::json!({"command": "cargo build"}),
        },
        ToolCallRecord {
            name: "shell".into(),
            args: serde_json::json!({"command": "cargo test"}),
        },
    ];

    // Use noop embedding (no deduplication).
    let noop = NoopEmbedding;
    let result = creator
        .create_from_execution("Build and test", &calls, Some(&noop))
        .await
        .unwrap();
    assert_eq!(result, Some("build-and-test".into()));

    // Verify the skill directory and TOML were created.
    let skill_dir = dir.path().join("skills").join("build-and-test");
    assert!(skill_dir.exists());
    let toml_content = tokio::fs::read_to_string(skill_dir.join("SKILL.toml"))
        .await
        .unwrap();
    assert!(toml_content.contains("build-and-test"));
    assert!(toml_content.contains("zeroclaw-auto"));
}

#[tokio::test]
async fn create_from_execution_with_dedup() {
    let dir = tempfile::tempdir().unwrap();
    let config = SkillCreationConfig {
        enabled: true,
        max_skills: 500,
        similarity_threshold: 0.85,
    };

    // First, create an existing skill.
    let skills_dir = dir.path().join("skills").join("existing");
    tokio::fs::create_dir_all(&skills_dir).await.unwrap();
    tokio::fs::write(
        skills_dir.join("SKILL.toml"),
        r#"[skill]
name = "existing"
description = "Auto-generated: Build and test"
version = "0.1.0"
author = "zeroclaw-auto"
tags = ["auto-generated"]
"#,
    )
    .await
    .unwrap();

    // High similarity provider -> should skip.
    let provider = MockEmbeddingProvider::new(0.95);
    let creator = SkillCreator::new(dir.path().to_path_buf(), config);
    let calls = vec![
        ToolCallRecord {
            name: "shell".into(),
            args: serde_json::json!({"command": "cargo build"}),
        },
        ToolCallRecord {
            name: "shell".into(),
            args: serde_json::json!({"command": "cargo test"}),
        },
    ];
    let result = creator
        .create_from_execution("Build and test", &calls, Some(&provider))
        .await
        .unwrap();
    assert!(result.is_none());
}

// ── Tool call extraction from history ────────────────────────

#[test]
fn extract_from_empty_history() {
    let history = vec![];
    let records = extract_tool_calls_from_history(&history);
    assert!(records.is_empty());
}

#[test]
fn extract_from_user_messages_only() {
    use crate::providers::ChatMessage;
    let history = vec![ChatMessage::user("hello"), ChatMessage::user("world")];
    let records = extract_tool_calls_from_history(&history);
    assert!(records.is_empty());
}

// ── Fuzz-like tests for slug ─────────────────────────────────

#[test]
fn slug_fuzz_various_inputs() {
    let inputs = [
        "",
        " ",
        "---",
        "a",
        "hello world!",
        "UPPER CASE",
        "with-hyphens-already",
        "with__underscores",
        "123 numbers 456",
        "emoji: cafe",
        &"x".repeat(200),
        "a-b-c-d-e-f-g-h-i-j-k-l-m-n-o-p-q-r-s-t-u-v-w-x-y-z-0-1-2-3-4-5",
    ];

    for input in &inputs {
        let slug = SkillCreator::generate_slug(input);
        // Slug should always pass validation (or be empty for degenerate input).
        if !slug.is_empty() {
            assert!(
                SkillCreator::validate_slug(&slug),
                "Generated slug '{slug}' from '{input}' failed validation"
            );
        }
    }
}

// ── Fuzz-like tests for TOML generation ──────────────────────

#[test]
fn toml_fuzz_various_inputs() {
    let descriptions = [
        "simple task",
        "task with \"quotes\" and \\ backslashes",
        "task with\nnewlines\r\nand tabs\there",
        "",
        &"long ".repeat(100),
    ];

    let args_variants = [
        serde_json::json!({}),
        serde_json::json!({"command": "echo hello"}),
        serde_json::json!({"command": "echo \"hello world\"", "extra": 42}),
    ];

    for desc in &descriptions {
        for args in &args_variants {
            let calls = vec![
                ToolCallRecord {
                    name: "tool1".into(),
                    args: args.clone(),
                },
                ToolCallRecord {
                    name: "tool2".into(),
                    args: args.clone(),
                },
            ];
            let toml_str = SkillCreator::generate_skill_toml("test-slug", desc, &calls);
            // Must always produce valid TOML.
            let _parsed: toml::Value = toml::from_str(&toml_str)
                .unwrap_or_else(|e| panic!("Invalid TOML for desc '{desc}': {e}\n{toml_str}"));
        }
    }
}

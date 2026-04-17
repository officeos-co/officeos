//! Personality template integration tests. See API.md §5 + §17 tests 4–5.

use std::fs;
use tempfile::TempDir;

/// Empty tmpdir, call `seed`, assert 3 .md files exist with correct content.
/// BOOTSTRAP.md should have the prompt substituted (no `{{prompt}}` literal).
#[tokio::test]
async fn test_seed_writes_missing_files() {
    let tmp = TempDir::new().unwrap();
    let prompt = "You are a helpful test agent.";

    zeroclaw_agent::personality::seed(tmp.path(), prompt)
        .await
        .expect("seed should succeed");

    // All three files must exist.
    let soul = tmp.path().join("SOUL.md");
    let identity = tmp.path().join("IDENTITY.md");
    let bootstrap = tmp.path().join("BOOTSTRAP.md");

    assert!(soul.exists(), "SOUL.md must exist");
    assert!(identity.exists(), "IDENTITY.md must exist");
    assert!(bootstrap.exists(), "BOOTSTRAP.md must exist");

    // Files must be non-empty.
    let soul_content = fs::read_to_string(&soul).unwrap();
    let identity_content = fs::read_to_string(&identity).unwrap();
    let bootstrap_content = fs::read_to_string(&bootstrap).unwrap();

    assert!(!soul_content.is_empty(), "SOUL.md must not be empty");
    assert!(!identity_content.is_empty(), "IDENTITY.md must not be empty");
    assert!(!bootstrap_content.is_empty(), "BOOTSTRAP.md must not be empty");

    // BOOTSTRAP.md must contain the substituted prompt, not the token.
    assert!(
        bootstrap_content.contains(prompt),
        "BOOTSTRAP.md should contain the substituted prompt"
    );
    assert!(
        !bootstrap_content.contains("{{prompt}}"),
        "BOOTSTRAP.md must not contain the literal {{prompt}} token"
    );
}

/// Pre-write SOUL.md with custom content, call `seed` with a different prompt.
/// Assert SOUL.md is unchanged; BOOTSTRAP.md is written fresh.
#[tokio::test]
async fn test_seed_idempotent_skip_existing() {
    let tmp = TempDir::new().unwrap();

    let custom_soul = "My custom SOUL content that should not be overwritten.";
    fs::write(tmp.path().join("SOUL.md"), custom_soul).unwrap();

    let prompt = "A different prompt.";
    zeroclaw_agent::personality::seed(tmp.path(), prompt)
        .await
        .expect("seed should succeed");

    // SOUL.md should be unchanged.
    let soul_content = fs::read_to_string(tmp.path().join("SOUL.md")).unwrap();
    assert_eq!(soul_content, custom_soul, "SOUL.md must not be overwritten");

    // BOOTSTRAP.md should have been written with the prompt.
    let bootstrap_content = fs::read_to_string(tmp.path().join("BOOTSTRAP.md")).unwrap();
    assert!(
        bootstrap_content.contains(prompt),
        "BOOTSTRAP.md should contain the new prompt"
    );
}

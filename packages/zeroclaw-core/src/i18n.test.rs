use super::*;
use std::fs;

/// Helper: create a temp dir with a `tool_descriptions/<locale>.toml` file.
fn write_locale_file(dir: &Path, locale: &str, content: &str) {
    let td = dir.join("tool_descriptions");
    fs::create_dir_all(&td).unwrap();
    fs::write(td.join(format!("{locale}.toml")), content).unwrap();
}

#[test]
fn load_english_descriptions() {
    let tmp = tempfile::tempdir().unwrap();
    write_locale_file(
        tmp.path(),
        "en",
        r#"[tools]
shell = "Execute a shell command"
file_read = "Read file contents"
"#,
    );
    let descs = ToolDescriptions::load("en", &[tmp.path().to_path_buf()]);
    assert_eq!(descs.get("shell"), Some("Execute a shell command"));
    assert_eq!(descs.get("file_read"), Some("Read file contents"));
    assert_eq!(descs.get("nonexistent"), None);
    assert_eq!(descs.locale(), "en");
}

#[test]
fn fallback_to_english_when_locale_key_missing() {
    let tmp = tempfile::tempdir().unwrap();
    write_locale_file(
        tmp.path(),
        "en",
        r#"[tools]
shell = "Execute a shell command"
file_read = "Read file contents"
"#,
    );
    write_locale_file(
        tmp.path(),
        "zh-CN",
        r#"[tools]
shell = "在工作区目录中执行 shell 命令"
"#,
    );
    let descs = ToolDescriptions::load("zh-CN", &[tmp.path().to_path_buf()]);
    // Translated key returns Chinese.
    assert_eq!(descs.get("shell"), Some("在工作区目录中执行 shell 命令"));
    // Missing key falls back to English.
    assert_eq!(descs.get("file_read"), Some("Read file contents"));
    assert_eq!(descs.locale(), "zh-CN");
}

#[test]
fn fallback_when_locale_file_missing() {
    let tmp = tempfile::tempdir().unwrap();
    write_locale_file(
        tmp.path(),
        "en",
        r#"[tools]
shell = "Execute a shell command"
"#,
    );
    // Request a locale that has no file.
    let descs = ToolDescriptions::load("fr", &[tmp.path().to_path_buf()]);
    // Falls back to English.
    assert_eq!(descs.get("shell"), Some("Execute a shell command"));
    assert_eq!(descs.locale(), "fr");
}

#[test]
fn fallback_when_no_files_exist() {
    let tmp = tempfile::tempdir().unwrap();
    let descs = ToolDescriptions::load("en", &[tmp.path().to_path_buf()]);
    assert_eq!(descs.get("shell"), None);
}

#[test]
fn empty_always_returns_none() {
    let descs = ToolDescriptions::empty();
    assert_eq!(descs.get("shell"), None);
    assert_eq!(descs.locale(), "en");
}

#[test]
fn detect_locale_from_env() {
    // Save and restore env.
    let saved = std::env::var("ZEROCLAW_LOCALE").ok();
    let saved_lang = std::env::var("LANG").ok();

    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::set_var("ZEROCLAW_LOCALE", "ja-JP") };
    assert_eq!(detect_locale(), "ja-JP");

    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("ZEROCLAW_LOCALE") };
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::set_var("LANG", "zh_CN.UTF-8") };
    assert_eq!(detect_locale(), "zh-CN");

    // Restore.
    match saved {
        // SAFETY: test-only, single-threaded test runner.
        Some(v) => unsafe { std::env::set_var("ZEROCLAW_LOCALE", v) },
        // SAFETY: test-only, single-threaded test runner.
        None => unsafe { std::env::remove_var("ZEROCLAW_LOCALE") },
    }
    match saved_lang {
        // SAFETY: test-only, single-threaded test runner.
        Some(v) => unsafe { std::env::set_var("LANG", v) },
        // SAFETY: test-only, single-threaded test runner.
        None => unsafe { std::env::remove_var("LANG") },
    }
}

#[test]
fn normalize_locale_strips_encoding() {
    assert_eq!(normalize_locale("en_US.UTF-8"), "en-US");
    assert_eq!(normalize_locale("zh_CN.utf8"), "zh-CN");
    assert_eq!(normalize_locale("fr"), "fr");
    assert_eq!(normalize_locale("pt_BR"), "pt-BR");
}

#[test]
fn config_locale_overrides_env() {
    // This tests the precedence logic: if config provides a locale,
    // it should be used instead of detect_locale().
    // The actual override happens at the call site in prompt.rs / loop_.rs,
    // so here we just verify ToolDescriptions works with an explicit locale.
    let tmp = tempfile::tempdir().unwrap();
    write_locale_file(
        tmp.path(),
        "de",
        r#"[tools]
shell = "Einen Shell-Befehl im Arbeitsverzeichnis ausführen"
"#,
    );
    let descs = ToolDescriptions::load("de", &[tmp.path().to_path_buf()]);
    assert_eq!(
        descs.get("shell"),
        Some("Einen Shell-Befehl im Arbeitsverzeichnis ausführen")
    );
}

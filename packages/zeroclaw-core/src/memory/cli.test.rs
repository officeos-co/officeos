use super::*;

#[test]
fn parse_category_known_variants() {
    assert_eq!(parse_category("core"), MemoryCategory::Core);
    assert_eq!(parse_category("daily"), MemoryCategory::Daily);
    assert_eq!(parse_category("conversation"), MemoryCategory::Conversation);
    assert_eq!(parse_category("CORE"), MemoryCategory::Core);
    assert_eq!(parse_category("  Daily  "), MemoryCategory::Daily);
}

#[test]
fn parse_category_custom_fallback() {
    assert_eq!(
        parse_category("project_notes"),
        MemoryCategory::Custom("project_notes".into())
    );
}

#[test]
fn truncate_content_short_text_unchanged() {
    assert_eq!(truncate_content("hello", 10), "hello");
}

#[test]
fn truncate_content_long_text_truncated() {
    let result = truncate_content("this is a very long string", 10);
    assert!(result.ends_with("..."));
    assert!(result.chars().count() <= 10);
}

#[test]
fn truncate_content_multiline_uses_first_line() {
    assert_eq!(truncate_content("first\nsecond", 20), "first");
}

#[test]
fn truncate_content_empty_string() {
    assert_eq!(truncate_content("", 10), "");
}

use super::*;

#[test]
fn empty_text() {
    assert!(chunk_markdown("", 512).is_empty());
    assert!(chunk_markdown("   ", 512).is_empty());
}

#[test]
fn single_short_paragraph() {
    let chunks = chunk_markdown("Hello world", 512);
    assert_eq!(chunks.len(), 1);
    assert_eq!(chunks[0].content, "Hello world");
    assert!(chunks[0].heading.is_none());
}

#[test]
fn heading_sections() {
    let text = "# Title\nSome intro.\n\n## Section A\nContent A.\n\n## Section B\nContent B.";
    let chunks = chunk_markdown(text, 512);
    assert!(chunks.len() >= 3);
    assert!(chunks[0].heading.is_none() || chunks[0].heading.as_deref() == Some("# Title"));
}

#[test]
fn respects_max_tokens() {
    // Build multi-line text (one sentence per line) to exercise line-level splitting
    let long_text: String = (0..200).fold(String::new(), |mut s, i| {
        use std::fmt::Write;
        let _ = writeln!(
            s,
            "This is sentence number {i} with some extra words to fill it up."
        );
        s
    });
    let chunks = chunk_markdown(&long_text, 50); // 50 tokens ≈ 200 chars
    assert!(
        chunks.len() > 1,
        "Expected multiple chunks, got {}",
        chunks.len()
    );
    for chunk in &chunks {
        // Allow some slack (heading re-insertion etc.)
        assert!(
            chunk.content.len() <= 300,
            "Chunk too long: {} chars",
            chunk.content.len()
        );
    }
}

#[test]
fn preserves_heading_in_split_sections() {
    let mut text = String::from("## Big Section\n");
    for i in 0..100 {
        use std::fmt::Write;
        let _ = write!(text, "Line {i} with some content here.\n\n");
    }
    let chunks = chunk_markdown(&text, 50);
    assert!(chunks.len() > 1);
    // All chunks from this section should reference the heading
    for chunk in &chunks {
        if chunk.heading.is_some() {
            assert_eq!(chunk.heading.as_deref(), Some("## Big Section"));
        }
    }
}

#[test]
fn indexes_are_sequential() {
    let text = "# A\nContent A\n\n# B\nContent B\n\n# C\nContent C";
    let chunks = chunk_markdown(text, 512);
    for (i, chunk) in chunks.iter().enumerate() {
        assert_eq!(chunk.index, i);
    }
}

#[test]
fn chunk_count_reasonable() {
    let text = "Hello world. This is a test document.";
    let chunks = chunk_markdown(text, 512);
    assert_eq!(chunks.len(), 1);
}

// ── Edge cases ───────────────────────────────────────────────

#[test]
fn headings_only_no_body() {
    let text = "# Title\n## Section A\n## Section B\n### Subsection";
    let chunks = chunk_markdown(text, 512);
    // Should produce chunks for each heading (even with empty bodies)
    assert!(!chunks.is_empty());
}

#[test]
fn deeply_nested_headings_ignored() {
    // #### and deeper are NOT treated as heading splits
    let text = "# Top\nIntro\n#### Deep heading\nDeep content";
    let chunks = chunk_markdown(text, 512);
    // "#### Deep heading" should stay with its parent section
    assert!(!chunks.is_empty());
    let all_content: String = chunks.iter().map(|c| c.content.clone()).collect();
    assert!(all_content.contains("Deep heading"));
    assert!(all_content.contains("Deep content"));
}

#[test]
fn very_long_single_line_no_newlines() {
    // One giant line with no newlines — can't split on lines effectively
    let text = "word ".repeat(5000);
    let chunks = chunk_markdown(&text, 50);
    // Should produce at least 1 chunk without panicking
    assert!(!chunks.is_empty());
}

#[test]
fn only_newlines_and_whitespace() {
    assert!(chunk_markdown("\n\n\n   \n\n", 512).is_empty());
}

#[test]
fn max_tokens_zero() {
    // max_tokens=0 → max_chars=0, should not panic or infinite loop
    let chunks = chunk_markdown("Hello world", 0);
    // Every chunk will exceed 0 chars, so it splits maximally
    assert!(!chunks.is_empty());
}

#[test]
fn max_tokens_one() {
    // max_tokens=1 → max_chars=4, very aggressive splitting
    let text = "Line one\nLine two\nLine three";
    let chunks = chunk_markdown(text, 1);
    assert!(!chunks.is_empty());
}

#[test]
fn unicode_content() {
    let text = "# 日本語\nこんにちは世界\n\n## Émojis\n🦀 Rust is great 🚀";
    let chunks = chunk_markdown(text, 512);
    assert!(!chunks.is_empty());
    let all: String = chunks.iter().map(|c| c.content.clone()).collect();
    assert!(all.contains("こんにちは"));
    assert!(all.contains("🦀"));
}

#[test]
fn fts5_special_chars_in_content() {
    let text = "Content with \"quotes\" and (parentheses) and * asterisks *";
    let chunks = chunk_markdown(text, 512);
    assert_eq!(chunks.len(), 1);
    assert!(chunks[0].content.contains("\"quotes\""));
}

#[test]
fn multiple_blank_lines_between_paragraphs() {
    let text = "Paragraph one.\n\n\n\n\nParagraph two.\n\n\n\nParagraph three.";
    let chunks = chunk_markdown(text, 512);
    assert_eq!(chunks.len(), 1); // All fits in one chunk
    assert!(chunks[0].content.contains("Paragraph one"));
    assert!(chunks[0].content.contains("Paragraph three"));
}

#[test]
fn heading_at_end_of_text() {
    let text = "Some content\n# Trailing Heading";
    let chunks = chunk_markdown(text, 512);
    assert!(!chunks.is_empty());
}

#[test]
fn single_heading_no_content() {
    let text = "# Just a heading";
    let chunks = chunk_markdown(text, 512);
    assert_eq!(chunks.len(), 1);
    assert_eq!(chunks[0].heading.as_deref(), Some("# Just a heading"));
}

#[test]
fn no_content_loss() {
    let text = "# A\nContent A line 1\nContent A line 2\n\n## B\nContent B\n\n## C\nContent C";
    let chunks = chunk_markdown(text, 512);
    let reassembled: String = chunks.iter().fold(String::new(), |mut s, c| {
        use std::fmt::Write;
        let _ = writeln!(s, "{}", c.content);
        s
    });
    // All original content words should appear
    for word in ["Content", "line", "1", "2"] {
        assert!(
            reassembled.contains(word),
            "Missing word '{word}' in reassembled chunks"
        );
    }
}

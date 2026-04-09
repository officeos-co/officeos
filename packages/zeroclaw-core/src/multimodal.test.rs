use super::*;

#[test]
fn parse_image_markers_extracts_multiple_markers() {
    let input = "Check this [IMAGE:/tmp/a.png] and this [IMAGE:https://example.com/b.jpg]";
    let (cleaned, refs) = parse_image_markers(input);

    assert_eq!(cleaned, "Check this  and this");
    assert_eq!(refs.len(), 2);
    assert_eq!(refs[0], "/tmp/a.png");
    assert_eq!(refs[1], "https://example.com/b.jpg");
}

#[test]
fn parse_image_markers_keeps_invalid_empty_marker() {
    let input = "hello [IMAGE:] world";
    let (cleaned, refs) = parse_image_markers(input);

    assert_eq!(cleaned, "hello [IMAGE:] world");
    assert!(refs.is_empty());
}

#[tokio::test]
async fn prepare_messages_normalizes_local_image_to_data_uri() {
    let temp = tempfile::tempdir().unwrap();
    let image_path = temp.path().join("sample.png");

    // Minimal PNG signature bytes are enough for MIME detection.
    std::fs::write(
        &image_path,
        [0x89, b'P', b'N', b'G', b'\r', b'\n', 0x1a, b'\n'],
    )
    .unwrap();

    let messages = vec![ChatMessage::user(format!(
        "Please inspect this screenshot [IMAGE:{}]",
        image_path.display()
    ))];

    let prepared = prepare_messages_for_provider(&messages, &MultimodalConfig::default())
        .await
        .unwrap();

    assert!(prepared.contains_images);
    assert_eq!(prepared.messages.len(), 1);

    let (cleaned, refs) = parse_image_markers(&prepared.messages[0].content);
    assert_eq!(cleaned, "Please inspect this screenshot");
    assert_eq!(refs.len(), 1);
    assert!(refs[0].starts_with("data:image/png;base64,"));
}

#[tokio::test]
async fn prepare_messages_trims_excess_images_from_older_messages() {
    // 3 messages, each with 1 image — max is 2.
    // The oldest message's image should be stripped.
    let messages = vec![
        ChatMessage::user("[IMAGE:/tmp/old.png]\nOld caption".to_string()),
        ChatMessage::user("[IMAGE:/tmp/mid.png]\nMid caption".to_string()),
        ChatMessage::user("[IMAGE:/tmp/new.png]\nNew caption".to_string()),
    ];

    // Should not error — instead trims oldest.
    // (Will error on normalize_image_reference for the surviving images
    //  since /tmp/mid.png and /tmp/new.png don't exist, but the trimming
    //  itself should succeed.)
    let trimmed = trim_old_images(&messages, 2);
    assert_eq!(trimmed.len(), 3);

    // Oldest message should have image stripped
    let (_, refs0) = parse_image_markers(&trimmed[0].content);
    assert!(refs0.is_empty(), "oldest image should be stripped");
    assert!(trimmed[0].content.contains("Old caption"));

    // Newer messages keep their images
    let (_, refs1) = parse_image_markers(&trimmed[1].content);
    assert_eq!(refs1.len(), 1);
    let (_, refs2) = parse_image_markers(&trimmed[2].content);
    assert_eq!(refs2.len(), 1);
}

#[test]
fn trim_old_images_replaces_image_only_message() {
    // A message with only an image and no text should get a placeholder.
    let messages = vec![
        ChatMessage::user("[IMAGE:/tmp/old.png]".to_string()),
        ChatMessage::user("[IMAGE:/tmp/new.png]\nKeep this".to_string()),
    ];

    let trimmed = trim_old_images(&messages, 1);
    assert_eq!(trimmed[0].content, "[image removed from history]");
    assert!(trimmed[1].content.contains("[IMAGE:/tmp/new.png]"));
}

#[test]
fn trim_old_images_multi_image_message_stripped_as_unit() {
    // A single message has 3 images. We need to drop 2 to reach max=1.
    // But trimming works at message granularity — the entire message gets
    // stripped (all 3 images removed), which over-trims to 0. The newest
    // message (text-only) is untouched.
    let messages = vec![
        ChatMessage::user(
            "[IMAGE:/tmp/a.png]\n[IMAGE:/tmp/b.png]\n[IMAGE:/tmp/c.png]\nThree pics".to_string(),
        ),
        ChatMessage::user("Just text, no images".to_string()),
    ];

    let trimmed = trim_old_images(&messages, 1);
    assert_eq!(trimmed.len(), 2);
    // All images in the first message are gone, but text remains
    let (_, refs0) = parse_image_markers(&trimmed[0].content);
    assert!(refs0.is_empty());
    assert!(trimmed[0].content.contains("Three pics"));
    // Second message unchanged
    assert_eq!(trimmed[1].content, "Just text, no images");
}

#[test]
fn trim_old_images_skips_assistant_messages() {
    // Assistant messages with image markers should not be counted or stripped.
    let messages = vec![
        ChatMessage {
            role: "assistant".to_string(),
            content: "[IMAGE:/tmp/assistant.png]\nAssistant generated".to_string(),
        },
        ChatMessage::user("[IMAGE:/tmp/user1.png]\nFirst".to_string()),
        ChatMessage::user("[IMAGE:/tmp/user2.png]\nSecond".to_string()),
    ];

    let trimmed = trim_old_images(&messages, 1);
    // Assistant message untouched (not counted toward limit)
    assert!(trimmed[0].content.contains("[IMAGE:/tmp/assistant.png]"));
    // Oldest user image stripped
    let (_, refs1) = parse_image_markers(&trimmed[1].content);
    assert!(refs1.is_empty());
    assert!(trimmed[1].content.contains("First"));
    // Newest user image kept
    let (_, refs2) = parse_image_markers(&trimmed[2].content);
    assert_eq!(refs2.len(), 1);
}

#[test]
fn trim_old_images_no_trimming_when_under_limit() {
    let messages = vec![
        ChatMessage::user("[IMAGE:/tmp/a.png]\nCaption A".to_string()),
        ChatMessage::user("[IMAGE:/tmp/b.png]\nCaption B".to_string()),
    ];

    let trimmed = trim_old_images(&messages, 5);
    // Nothing should change — both images are under the limit
    assert_eq!(trimmed[0].content, messages[0].content);
    assert_eq!(trimmed[1].content, messages[1].content);
}

#[test]
fn trim_old_images_no_trimming_when_exactly_at_limit() {
    let messages = vec![
        ChatMessage::user("[IMAGE:/tmp/a.png]\nA".to_string()),
        ChatMessage::user("[IMAGE:/tmp/b.png]\nB".to_string()),
    ];

    let trimmed = trim_old_images(&messages, 2);
    assert_eq!(trimmed[0].content, messages[0].content);
    assert_eq!(trimmed[1].content, messages[1].content);
}

#[test]
fn trim_old_images_empty_messages() {
    let trimmed = trim_old_images(&[], 4);
    assert!(trimmed.is_empty());
}

#[test]
fn trim_old_images_interleaved_roles() {
    // Realistic conversation: user sends image, assistant replies, user sends
    // another image, etc. Only user messages should be candidates for trimming.
    let messages = vec![
        ChatMessage::user("[IMAGE:/tmp/1.png]\nLook at this".to_string()),
        ChatMessage {
            role: "assistant".to_string(),
            content: "I see a photo.".to_string(),
        },
        ChatMessage::user("[IMAGE:/tmp/2.png]\nWhat about this?".to_string()),
        ChatMessage {
            role: "assistant".to_string(),
            content: "That's a chart.".to_string(),
        },
        ChatMessage::user("[IMAGE:/tmp/3.png]\nAnd this one".to_string()),
    ];

    let trimmed = trim_old_images(&messages, 2);
    assert_eq!(trimmed.len(), 5);
    // Oldest user image stripped
    let (_, refs0) = parse_image_markers(&trimmed[0].content);
    assert!(refs0.is_empty());
    assert!(trimmed[0].content.contains("Look at this"));
    // Assistant messages untouched
    assert_eq!(trimmed[1].content, "I see a photo.");
    assert_eq!(trimmed[3].content, "That's a chart.");
    // Two newest user images kept
    let (_, refs2) = parse_image_markers(&trimmed[2].content);
    assert_eq!(refs2.len(), 1);
    let (_, refs4) = parse_image_markers(&trimmed[4].content);
    assert_eq!(refs4.len(), 1);
}

#[test]
fn trim_old_images_strips_multiple_oldest_messages() {
    // 5 user images, max 1 — should strip the first 4 messages' images.
    let messages: Vec<ChatMessage> = (1..=5)
        .map(|i| ChatMessage::user(format!("[IMAGE:/tmp/{i}.png]\nCaption {i}")))
        .collect();

    let trimmed = trim_old_images(&messages, 1);
    assert_eq!(trimmed.len(), 5);
    for (i, msg) in trimmed.iter().enumerate().take(4) {
        let (_, refs) = parse_image_markers(&msg.content);
        assert!(refs.is_empty(), "message {i} should have images stripped");
        assert!(msg.content.contains(&format!("Caption {}", i + 1)));
    }
    // Only the last message keeps its image
    let (_, refs_last) = parse_image_markers(&trimmed[4].content);
    assert_eq!(refs_last.len(), 1);
}

#[tokio::test]
async fn prepare_messages_trims_then_normalizes_surviving_images() {
    // End-to-end: 3 images, max 2. After trimming the oldest, the two
    // surviving images should be normalized (base64-encoded) successfully.
    let temp = tempfile::tempdir().unwrap();
    let mut paths = Vec::new();
    for name in ["old.png", "mid.png", "new.png"] {
        let p = temp.path().join(name);
        // Minimal valid PNG (1x1 white pixel)
        let png_data = [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x08, 0x02, 0x00, 0x00, 0x00, 0x90,
            0x77, 0x53, 0xDE, // 1x1 RGB
            0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, 0x54, // IDAT chunk
            0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00, 0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21,
            0xBC, 0x33, // IDAT data + CRC
            0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, // IEND chunk
            0xAE, 0x42, 0x60, 0x82,
        ];
        std::fs::write(&p, png_data).unwrap();
        paths.push(p);
    }

    let messages = vec![
        ChatMessage::user(format!("[IMAGE:{}]\nOld", paths[0].display())),
        ChatMessage::user(format!("[IMAGE:{}]\nMid", paths[1].display())),
        ChatMessage::user(format!("[IMAGE:{}]\nNew", paths[2].display())),
    ];

    let config = MultimodalConfig {
        max_images: 2,
        max_image_size_mb: 5,
        allow_remote_fetch: false,
        ..Default::default()
    };

    let result = prepare_messages_for_provider(&messages, &config)
        .await
        .expect("should succeed after trimming");

    assert!(result.contains_images);
    assert_eq!(result.messages.len(), 3);
    // First message should have image stripped, text preserved
    assert!(!result.messages[0].content.contains("data:image"));
    assert!(result.messages[0].content.contains("Old"));
    // Second and third should have base64-encoded images
    assert!(result.messages[1].content.contains("data:image"));
    assert!(result.messages[2].content.contains("data:image"));
}

#[tokio::test]
async fn prepare_messages_rejects_remote_url_when_disabled() {
    let messages = vec![ChatMessage::user(
        "Look [IMAGE:https://example.com/img.png]".to_string(),
    )];

    let error = prepare_messages_for_provider(&messages, &MultimodalConfig::default())
        .await
        .expect_err("should reject remote image URL when fetch is disabled");

    assert!(
        error
            .to_string()
            .contains("multimodal remote image fetch is disabled")
    );
}

#[tokio::test]
async fn prepare_messages_rejects_oversized_local_image() {
    let temp = tempfile::tempdir().unwrap();
    let image_path = temp.path().join("big.png");

    let bytes = vec![0u8; 1024 * 1024 + 1];
    std::fs::write(&image_path, bytes).unwrap();

    let messages = vec![ChatMessage::user(format!(
        "[IMAGE:{}]",
        image_path.display()
    ))];
    let config = MultimodalConfig {
        max_images: 4,
        max_image_size_mb: 1,
        allow_remote_fetch: false,
        ..Default::default()
    };

    let error = prepare_messages_for_provider(&messages, &config)
        .await
        .expect_err("should reject oversized local image");

    assert!(
        error
            .to_string()
            .contains("multimodal image size limit exceeded")
    );
}

#[test]
fn extract_ollama_image_payload_supports_data_uris() {
    let payload = extract_ollama_image_payload("data:image/png;base64,abcd==")
        .expect("payload should be extracted");
    assert_eq!(payload, "abcd==");
}

/// Stripping `[IMAGE:]` markers from history messages leaves only the text
/// portion, which is the behaviour needed for non-vision providers (#3674).
#[test]
fn parse_image_markers_strips_markers_leaving_caption() {
    let input = "[IMAGE:/tmp/photo.jpg]\n\nDescribe this screenshot";
    let (cleaned, refs) = parse_image_markers(input);
    assert_eq!(cleaned, "Describe this screenshot");
    assert_eq!(refs.len(), 1);
    assert_eq!(refs[0], "/tmp/photo.jpg");
}

/// An image-only message (no caption) should produce an empty string after
/// marker stripping, so callers can drop it from history.
#[test]
fn parse_image_markers_image_only_message_becomes_empty() {
    let input = "[IMAGE:/tmp/photo.jpg]";
    let (cleaned, refs) = parse_image_markers(input);
    assert!(
        cleaned.is_empty(),
        "expected empty string, got: {cleaned:?}"
    );
    assert_eq!(refs.len(), 1);
}

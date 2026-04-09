use super::*;

#[test]
fn clean_content_passes() {
    let detector = LeakDetector::new();
    let result = detector.scan("This is just some normal text");
    assert!(matches!(result, LeakResult::Clean));
}

#[test]
fn detects_stripe_keys() {
    let detector = LeakDetector::new();
    let content = "My Stripe key is sk_test_1234567890abcdefghijklmnop";
    let result = detector.scan(content);
    match result {
        LeakResult::Detected { patterns, redacted } => {
            assert!(patterns.iter().any(|p| p.contains("Stripe")));
            assert!(redacted.contains("[REDACTED"));
        }
        LeakResult::Clean => panic!("Should detect Stripe key"),
    }
}

#[test]
fn detects_aws_credentials() {
    let detector = LeakDetector::new();
    let content = "AWS key: AKIAIOSFODNN7EXAMPLE";
    let result = detector.scan(content);
    match result {
        LeakResult::Detected { patterns, .. } => {
            assert!(patterns.iter().any(|p| p.contains("AWS")));
        }
        LeakResult::Clean => panic!("Should detect AWS key"),
    }
}

#[test]
fn detects_private_keys() {
    let detector = LeakDetector::new();
    let content = r#"
-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEA0ZPr5JeyVDonXsKhfq...
-----END RSA PRIVATE KEY-----
"#;
    let result = detector.scan(content);
    match result {
        LeakResult::Detected { patterns, redacted } => {
            assert!(patterns.iter().any(|p| p.contains("private key")));
            assert!(redacted.contains("[REDACTED_PRIVATE_KEY]"));
        }
        LeakResult::Clean => panic!("Should detect private key"),
    }
}

#[test]
fn detects_jwt_tokens() {
    let detector = LeakDetector::new();
    let content = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
    let result = detector.scan(content);
    match result {
        LeakResult::Detected { patterns, redacted } => {
            assert!(patterns.iter().any(|p| p.contains("JWT")));
            assert!(redacted.contains("[REDACTED_JWT]"));
        }
        LeakResult::Clean => panic!("Should detect JWT"),
    }
}

#[test]
fn detects_database_urls() {
    let detector = LeakDetector::new();
    let content = "DATABASE_URL=postgres://user:secretpassword@localhost:5432/mydb";
    let result = detector.scan(content);
    match result {
        LeakResult::Detected { patterns, .. } => {
            assert!(patterns.iter().any(|p| p.contains("PostgreSQL")));
        }
        LeakResult::Clean => panic!("Should detect database URL"),
    }
}

#[test]
fn low_sensitivity_skips_generic() {
    let detector = LeakDetector::with_sensitivity(0.3);
    let content = "secret=mygenericvalue123456";
    let result = detector.scan(content);
    // Low sensitivity should not flag generic secrets
    assert!(matches!(result, LeakResult::Clean));
}

#[test]
fn url_path_segments_not_flagged() {
    let detector = LeakDetector::new();
    // URL with a long mixed-alphanumeric path segment that would previously
    // false-positive as a high-entropy token.
    let content =
        "See https://example.org/documents/2024-report-a1b2c3d4e5f6g7h8i9j0.pdf for details";
    let result = detector.scan(content);
    assert!(
        matches!(result, LeakResult::Clean),
        "URL path segments should not trigger high-entropy detection"
    );
}

#[test]
fn url_with_long_path_not_redacted() {
    let detector = LeakDetector::new();
    let content = "Reference: https://gov.example.com/publications/research/2024-annual-fiscal-policy-review-9a8b7c6d5e4f3g2h1i0j.html";
    let result = detector.scan(content);
    assert!(
        matches!(result, LeakResult::Clean),
        "Long URL paths should not be redacted"
    );
}

#[test]
fn media_markers_not_redacted_as_high_entropy() {
    let detector = LeakDetector::new();
    let content = "Here is the image: [IMAGE:/Users/matt/.zeroclaw/workspace/skills/image-gen/images/20260324_135911.png]";
    let result = detector.scan(content);
    assert!(
        matches!(result, LeakResult::Clean),
        "Local media markers should not be redacted"
    );
}

#[test]
fn detects_high_entropy_token_outside_url() {
    let detector = LeakDetector::new();
    // A standalone high-entropy token (not in a URL) should still be detected.
    let content = "Found credential: aB3xK9mW2pQ7vL4nR8sT1yU6hD0jF5cG";
    let result = detector.scan(content);
    match result {
        LeakResult::Detected { patterns, redacted } => {
            assert!(patterns.iter().any(|p| p.contains("High-entropy")));
            assert!(redacted.contains("[REDACTED_HIGH_ENTROPY_TOKEN]"));
        }
        LeakResult::Clean => panic!("Should detect high-entropy token"),
    }
}

#[test]
fn low_sensitivity_raises_entropy_threshold() {
    let detector = LeakDetector::with_sensitivity(0.3);
    // At low sensitivity the entropy threshold is higher (3.5 + 0.3*1.25 = 3.875).
    // A repetitive mixed token has low entropy and should not be flagged.
    let content = "token found: ab12ab12ab12ab12ab12ab12ab12ab12";
    let result = detector.scan(content);
    assert!(
        matches!(result, LeakResult::Clean),
        "Low-entropy repetitive tokens should not be flagged"
    );
}

#[test]
fn extract_candidate_tokens_splits_correctly() {
    let tokens = extract_candidate_tokens("foo.bar:baz qux-quux key=val");
    assert!(tokens.contains(&"foo"));
    assert!(tokens.contains(&"bar"));
    assert!(tokens.contains(&"baz"));
    assert!(tokens.contains(&"qux-quux"));
    // '=' is a delimiter, not part of tokens
    assert!(tokens.contains(&"key"));
    assert!(tokens.contains(&"val"));
}

#[test]
fn media_marker_image_path_not_redacted() {
    let detector = LeakDetector::new();
    let content = "Here is your image: [IMAGE:/Users/matt/.zeroclaw/workspace/skills/image-gen/images/20260324_135911.png]";
    let result = detector.scan(content);
    assert!(
        matches!(result, LeakResult::Clean),
        "Media marker image paths should not trigger high-entropy detection"
    );
}

#[test]
fn media_marker_video_not_redacted() {
    let detector = LeakDetector::new();
    let content = "Attached: [VIDEO:/path/to/long/video/file/name123456.mp4]";
    let result = detector.scan(content);
    assert!(
        matches!(result, LeakResult::Clean),
        "Media marker video paths should not trigger high-entropy detection"
    );
}

#[test]
fn actual_high_entropy_still_detected() {
    let detector = LeakDetector::new();
    let content = "Leaked credential: aB3xK9mW2pQ7vL4nR8sT1yU6hD0jF5cG";
    let result = detector.scan(content);
    match result {
        LeakResult::Detected { patterns, redacted } => {
            assert!(patterns.iter().any(|p| p.contains("High-entropy")));
            assert!(redacted.contains("[REDACTED_HIGH_ENTROPY_TOKEN]"));
        }
        LeakResult::Clean => {
            panic!("Should still detect high-entropy tokens outside media markers")
        }
    }
}

#[test]
fn shannon_entropy_empty_string() {
    assert_eq!(shannon_entropy(""), 0.0);
}

#[test]
fn shannon_entropy_single_char() {
    // All same characters: entropy = 0
    assert_eq!(shannon_entropy("aaaa"), 0.0);
}

#[test]
fn shannon_entropy_two_equal_chars() {
    // "ab" repeated: entropy = 1.0 bit
    let e = shannon_entropy("abab");
    assert!((e - 1.0).abs() < 0.001);
}

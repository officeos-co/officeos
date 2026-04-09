use super::*;

// ── URL extraction ──────────────────────────────────────────────

#[test]
fn extract_urls_finds_http_and_https() {
    let text = "Check https://example.com and http://test.org/page for info";
    let urls = extract_urls(text, 10);
    assert_eq!(urls, vec!["https://example.com", "http://test.org/page",]);
}

#[test]
fn extract_urls_respects_max() {
    let text = "https://a.com https://b.com https://c.com https://d.com";
    let urls = extract_urls(text, 2);
    assert_eq!(urls.len(), 2);
    assert_eq!(urls[0], "https://a.com");
    assert_eq!(urls[1], "https://b.com");
}

#[test]
fn extract_urls_deduplicates() {
    let text = "Visit https://example.com and https://example.com again";
    let urls = extract_urls(text, 10);
    assert_eq!(urls.len(), 1);
}

#[test]
fn extract_urls_handles_no_urls() {
    let text = "Just a normal message without links";
    let urls = extract_urls(text, 10);
    assert!(urls.is_empty());
}

#[test]
fn extract_urls_stops_at_angle_brackets() {
    let text = "Link: <https://example.com/path> done";
    let urls = extract_urls(text, 10);
    assert_eq!(urls, vec!["https://example.com/path"]);
}

#[test]
fn extract_urls_stops_at_quotes() {
    let text = r#"href="https://example.com/page" end"#;
    let urls = extract_urls(text, 10);
    assert_eq!(urls, vec!["https://example.com/page"]);
}

// ── SSRF protection ─────────────────────────────────────────────

#[test]
fn ssrf_blocks_localhost() {
    assert!(is_ssrf_target("http://localhost/admin"));
    assert!(is_ssrf_target("https://localhost:8080/api"));
}

#[test]
fn ssrf_blocks_loopback_ip() {
    assert!(is_ssrf_target("http://127.0.0.1/secret"));
    assert!(is_ssrf_target("http://127.0.0.2:9090"));
}

#[test]
fn ssrf_blocks_private_10_network() {
    assert!(is_ssrf_target("http://10.0.0.1/internal"));
    assert!(is_ssrf_target("http://10.255.255.255"));
}

#[test]
fn ssrf_blocks_private_172_network() {
    assert!(is_ssrf_target("http://172.16.0.1/admin"));
    assert!(is_ssrf_target("http://172.31.255.255"));
}

#[test]
fn ssrf_blocks_private_192_168_network() {
    assert!(is_ssrf_target("http://192.168.1.1/router"));
    assert!(is_ssrf_target("http://192.168.0.100:3000"));
}

#[test]
fn ssrf_blocks_link_local() {
    assert!(is_ssrf_target("http://169.254.0.1/metadata"));
    assert!(is_ssrf_target("http://169.254.169.254/latest"));
}

#[test]
fn ssrf_blocks_ipv6_loopback() {
    // IPv6 in brackets is rejected by extract_host
    assert!(is_ssrf_target("http://[::1]/admin"));
}

#[test]
fn ssrf_blocks_dot_local() {
    assert!(is_ssrf_target("http://myhost.local/api"));
}

#[test]
fn ssrf_allows_public_urls() {
    assert!(!is_ssrf_target("https://example.com/page"));
    assert!(!is_ssrf_target("https://www.google.com"));
    assert!(!is_ssrf_target("http://93.184.216.34/resource"));
}

// ── Title extraction ────────────────────────────────────────────

#[test]
fn extract_title_basic() {
    let html = "<html><head><title>My Page Title</title></head><body>Hello</body></html>";
    assert_eq!(extract_title(html), Some("my page title".to_string()));
}

#[test]
fn extract_title_with_entities() {
    let html = "<title>Tom &amp; Jerry&#39;s Page</title>";
    assert_eq!(extract_title(html), Some("tom & jerry's page".to_string()));
}

#[test]
fn extract_title_case_insensitive() {
    let html = "<HTML><HEAD><TITLE>Upper Case</TITLE></HEAD></HTML>";
    assert_eq!(extract_title(html), Some("upper case".to_string()));
}

#[test]
fn extract_title_multibyte_chars_no_panic() {
    // İ (U+0130) lowercases to 2 chars, changing byte length.
    // This must not panic or produce wrong offsets.
    let html = "<title>İstanbul Guide</title>";
    let result = extract_title(html);
    assert!(result.is_some());
    let title = result.unwrap();
    assert!(title.contains("stanbul"));
}

#[test]
fn extract_title_missing() {
    let html = "<html><body>No title here</body></html>";
    assert_eq!(extract_title(html), None);
}

#[test]
fn extract_title_empty() {
    let html = "<title>   </title>";
    assert_eq!(extract_title(html), None);
}

// ── Body text extraction ────────────────────────────────────────

#[test]
fn extract_body_text_strips_html() {
    let html = "<html><body><h1>Header</h1><p>Some content here</p></body></html>";
    let text = extract_body_text(html, 200);
    assert!(text.contains("Header"));
    assert!(text.contains("Some content"));
    assert!(!text.contains("<h1>"));
}

#[test]
fn extract_body_text_truncates() {
    let html = "<p>A very long paragraph that should be truncated to fit within the limit.</p>";
    let text = extract_body_text(html, 20);
    assert!(text.len() <= 25); // 20 chars + "..."
    assert!(text.ends_with("..."));
}

// ── Config toggle ───────────────────────────────────────────────

#[tokio::test]
async fn enrich_message_disabled_returns_original() {
    let config = LinkEnricherConfig {
        enabled: false,
        max_links: 3,
        timeout_secs: 10,
    };
    let msg = "Check https://example.com for details";
    let result = enrich_message(msg, &config).await;
    assert_eq!(result, msg);
}

#[tokio::test]
async fn enrich_message_no_urls_returns_original() {
    let config = LinkEnricherConfig {
        enabled: true,
        max_links: 3,
        timeout_secs: 10,
    };
    let msg = "No links in this message";
    let result = enrich_message(msg, &config).await;
    assert_eq!(result, msg);
}

#[tokio::test]
async fn enrich_message_ssrf_urls_returns_original() {
    let config = LinkEnricherConfig {
        enabled: true,
        max_links: 3,
        timeout_secs: 10,
    };
    let msg = "Try http://127.0.0.1/admin and http://192.168.1.1/router";
    let result = enrich_message(msg, &config).await;
    assert_eq!(result, msg);
}

#[test]
fn default_config_is_disabled() {
    let config = LinkEnricherConfig::default();
    assert!(!config.enabled);
    assert_eq!(config.max_links, 3);
    assert_eq!(config.timeout_secs, 10);
}

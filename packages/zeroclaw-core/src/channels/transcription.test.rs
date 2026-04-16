use super::*;

#[tokio::test]
async fn rejects_oversized_audio() {
    let big = vec![0u8; MAX_AUDIO_BYTES + 1];
    let config = TranscriptionConfig::default();

    let err = transcribe_audio(big, "test.ogg", &config)
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("too large"),
        "expected size error, got: {err}"
    );
}

#[tokio::test]
async fn rejects_missing_api_key() {
    // Ensure all candidate keys are absent for this test.
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("GROQ_API_KEY") };
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("OPENAI_API_KEY") };
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("TRANSCRIPTION_API_KEY") };

    let data = vec![0u8; 100];
    let config = TranscriptionConfig::default();

    let err = transcribe_audio(data, "test.ogg", &config)
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("transcription API key"),
        "expected missing-key error, got: {err}"
    );
}

#[tokio::test]
async fn uses_config_api_key_without_groq_env() {
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("GROQ_API_KEY") };

    let data = vec![0u8; 100];
    let mut config = TranscriptionConfig::default();
    config.api_key = Some("transcription-key".to_string());

    // Keep invalid extension so we fail before network, but after key resolution.
    let err = transcribe_audio(data, "recording.aac", &config)
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("Unsupported audio format"),
        "expected unsupported-format error, got: {err}"
    );
}

#[tokio::test]
async fn openai_default_provider_uses_openai_config() {
    let data = vec![0u8; 100];
    let mut config = TranscriptionConfig::default();
    config.default_provider = "openai".to_string();
    config.openai = Some(crate::config::OpenAiSttConfig {
        api_key: None,
        model: "gpt-4o-mini-transcribe".to_string(),
    });

    let err = transcribe_audio(data, "test.ogg", &config)
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("[transcription.openai].api_key"),
        "expected openai-specific missing-key error, got: {err}"
    );
}

#[test]
fn mime_for_audio_maps_accepted_formats() {
    let cases = [
        ("flac", "audio/flac"),
        ("mp3", "audio/mpeg"),
        ("mpeg", "audio/mpeg"),
        ("mpga", "audio/mpeg"),
        ("mp4", "audio/mp4"),
        ("m4a", "audio/mp4"),
        ("ogg", "audio/ogg"),
        ("oga", "audio/ogg"),
        ("opus", "audio/opus"),
        ("wav", "audio/wav"),
        ("webm", "audio/webm"),
    ];
    for (ext, expected) in cases {
        assert_eq!(
            mime_for_audio(ext),
            Some(expected),
            "failed for extension: {ext}"
        );
    }
}

#[test]
fn mime_for_audio_case_insensitive() {
    assert_eq!(mime_for_audio("OGG"), Some("audio/ogg"));
    assert_eq!(mime_for_audio("MP3"), Some("audio/mpeg"));
    assert_eq!(mime_for_audio("Opus"), Some("audio/opus"));
}

#[test]
fn mime_for_audio_rejects_unknown() {
    assert_eq!(mime_for_audio("txt"), None);
    assert_eq!(mime_for_audio("pdf"), None);
    assert_eq!(mime_for_audio("aac"), None);
    assert_eq!(mime_for_audio(""), None);
}

#[test]
fn normalize_audio_filename_rewrites_oga() {
    assert_eq!(normalize_audio_filename("voice.oga"), "voice.ogg");
    assert_eq!(normalize_audio_filename("file.OGA"), "file.ogg");
}

#[test]
fn normalize_audio_filename_preserves_accepted() {
    assert_eq!(normalize_audio_filename("voice.ogg"), "voice.ogg");
    assert_eq!(normalize_audio_filename("track.mp3"), "track.mp3");
    assert_eq!(normalize_audio_filename("clip.opus"), "clip.opus");
}

#[test]
fn normalize_audio_filename_no_extension() {
    assert_eq!(normalize_audio_filename("voice"), "voice");
}

#[tokio::test]
async fn rejects_unsupported_audio_format() {
    let data = vec![0u8; 100];
    let config = TranscriptionConfig::default();

    let err = transcribe_audio(data, "recording.aac", &config)
        .await
        .unwrap_err();
    let msg = err.to_string();
    assert!(
        msg.contains("Unsupported audio format"),
        "expected unsupported-format error, got: {msg}"
    );
    assert!(
        msg.contains(".aac"),
        "error should mention the rejected extension, got: {msg}"
    );
}

// ── TranscriptionManager tests ──────────────────────────────

#[test]
fn manager_creation_with_default_config() {
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("GROQ_API_KEY") };

    let config = TranscriptionConfig::default();
    let manager = TranscriptionManager::new(&config).unwrap();
    assert_eq!(manager.default_provider, "groq");
    // Groq won't be registered without a key.
    assert!(manager.providers.is_empty());
}

#[test]
fn manager_registers_groq_with_key() {
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("GROQ_API_KEY") };

    let mut config = TranscriptionConfig::default();
    config.api_key = Some("test-groq-key".to_string());

    let manager = TranscriptionManager::new(&config).unwrap();
    assert!(manager.providers.contains_key("groq"));
    assert_eq!(manager.providers["groq"].name(), "groq");
}

#[test]
fn manager_registers_multiple_providers() {
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("GROQ_API_KEY") };

    let mut config = TranscriptionConfig::default();
    config.api_key = Some("test-groq-key".to_string());
    config.openai = Some(crate::config::OpenAiSttConfig {
        api_key: Some("test-openai-key".to_string()),
        model: "whisper-1".to_string(),
    });
    config.deepgram = Some(crate::config::DeepgramSttConfig {
        api_key: Some("test-deepgram-key".to_string()),
        model: "nova-2".to_string(),
    });

    let manager = TranscriptionManager::new(&config).unwrap();
    assert!(manager.providers.contains_key("groq"));
    assert!(manager.providers.contains_key("openai"));
    assert!(manager.providers.contains_key("deepgram"));
    assert_eq!(manager.available_providers().len(), 3);
}

#[tokio::test]
async fn manager_rejects_unconfigured_provider() {
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("GROQ_API_KEY") };

    let mut config = TranscriptionConfig::default();
    config.api_key = Some("test-groq-key".to_string());

    let manager = TranscriptionManager::new(&config).unwrap();
    let err = manager
        .transcribe_with_provider(&[0u8; 100], "test.ogg", "nonexistent")
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("not configured"),
        "expected not-configured error, got: {err}"
    );
}

#[test]
fn manager_default_provider_from_config() {
    // SAFETY: test-only, single-threaded test runner.
    unsafe { std::env::remove_var("GROQ_API_KEY") };

    let mut config = TranscriptionConfig::default();
    config.default_provider = "openai".to_string();
    config.openai = Some(crate::config::OpenAiSttConfig {
        api_key: Some("test-openai-key".to_string()),
        model: "whisper-1".to_string(),
    });

    let manager = TranscriptionManager::new(&config).unwrap();
    assert_eq!(manager.default_provider, "openai");
}

#[test]
fn validate_audio_rejects_oversized() {
    let big = vec![0u8; MAX_AUDIO_BYTES + 1];
    let err = validate_audio(&big, "test.ogg").unwrap_err();
    assert!(err.to_string().contains("too large"));
}

#[test]
fn validate_audio_rejects_unsupported_format() {
    let data = vec![0u8; 100];
    let err = validate_audio(&data, "test.aac").unwrap_err();
    assert!(err.to_string().contains("Unsupported audio format"));
}

#[test]
fn validate_audio_accepts_supported_format() {
    let data = vec![0u8; 100];
    let (name, mime) = validate_audio(&data, "test.ogg").unwrap();
    assert_eq!(name, "test.ogg");
    assert_eq!(mime, "audio/ogg");
}

#[test]
fn validate_audio_normalizes_oga() {
    let data = vec![0u8; 100];
    let (name, mime) = validate_audio(&data, "voice.oga").unwrap();
    assert_eq!(name, "voice.ogg");
    assert_eq!(mime, "audio/ogg");
}

#[test]
fn backward_compat_config_defaults_unchanged() {
    let config = TranscriptionConfig::default();
    assert!(!config.enabled);
    assert!(config.api_key.is_none());
    assert!(config.api_url.contains("groq.com"));
    assert_eq!(config.model, "whisper-large-v3-turbo");
    assert_eq!(config.default_provider, "groq");
    assert!(config.openai.is_none());
    assert!(config.deepgram.is_none());
    assert!(config.assemblyai.is_none());
    assert!(config.google.is_none());
    assert!(config.local_whisper.is_none());
    assert!(!config.transcribe_non_ptt_audio);
}

// ── LocalWhisperProvider tests (TDD — added below as red/green cycles) ──

fn local_whisper_config(url: &str) -> crate::config::LocalWhisperConfig {
    crate::config::LocalWhisperConfig {
        url: url.to_string(),
        bearer_token: Some("test-token".to_string()),
        max_audio_bytes: 10 * 1024 * 1024,
        timeout_secs: 30,
    }
}

#[test]
fn local_whisper_rejects_empty_url() {
    let mut cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    cfg.url = String::new();
    let err = LocalWhisperProvider::from_config(&cfg).err().unwrap();
    assert!(
        err.to_string().contains("`url` must not be empty"),
        "got: {err}"
    );
}

#[test]
fn local_whisper_rejects_invalid_url() {
    let mut cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    cfg.url = "not-a-url".to_string();
    let err = LocalWhisperProvider::from_config(&cfg).err().unwrap();
    assert!(err.to_string().contains("invalid `url`"), "got: {err}");
}

#[test]
fn local_whisper_rejects_non_http_url() {
    let mut cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    cfg.url = "ftp://10.10.0.1:8001/v1/transcribe".to_string();
    let err = LocalWhisperProvider::from_config(&cfg).err().unwrap();
    assert!(err.to_string().contains("http or https"), "got: {err}");
}

#[test]
fn local_whisper_rejects_empty_bearer_token() {
    let mut cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    cfg.bearer_token = Some(String::new());
    let err = LocalWhisperProvider::from_config(&cfg).err().unwrap();
    assert!(
        err.to_string().contains("`bearer_token` must not be empty"),
        "got: {err}"
    );
}

#[test]
fn local_whisper_rejects_missing_bearer_token() {
    let mut cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    cfg.bearer_token = None;
    let err = LocalWhisperProvider::from_config(&cfg).err().unwrap();
    assert!(
        err.to_string().contains("`bearer_token` must be set"),
        "got: {err}"
    );
}

#[test]
fn local_whisper_rejects_zero_max_audio_bytes() {
    let mut cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    cfg.max_audio_bytes = 0;
    let err = LocalWhisperProvider::from_config(&cfg).err().unwrap();
    assert!(
        err.to_string()
            .contains("`max_audio_bytes` must be greater than zero"),
        "got: {err}"
    );
}

#[test]
fn local_whisper_rejects_zero_timeout() {
    let mut cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    cfg.timeout_secs = 0;
    let err = LocalWhisperProvider::from_config(&cfg).err().unwrap();
    assert!(
        err.to_string()
            .contains("`timeout_secs` must be greater than zero"),
        "got: {err}"
    );
}

#[test]
fn local_whisper_registered_when_config_present() {
    let mut config = TranscriptionConfig::default();
    config.local_whisper = Some(local_whisper_config("http://127.0.0.1:9999/v1/transcribe"));
    config.default_provider = "local_whisper".to_string();

    let manager = TranscriptionManager::new(&config).unwrap();
    assert!(
        manager.available_providers().contains(&"local_whisper"),
        "expected local_whisper in {:?}",
        manager.available_providers()
    );
}

#[test]
fn local_whisper_misconfigured_section_fails_manager_construction() {
    // A misconfigured local_whisper section logs a warning and skips
    // registration. When local_whisper is also the default_provider and
    // transcription is enabled, the safety net in TranscriptionManager
    // surfaces the error: "not configured".
    let mut config = TranscriptionConfig::default();
    let mut bad_cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    bad_cfg.bearer_token = Some(String::new());
    config.local_whisper = Some(bad_cfg);
    config.enabled = true;
    config.default_provider = "local_whisper".to_string();

    let err = TranscriptionManager::new(&config).err().unwrap();
    assert!(
        err.to_string().contains("not configured"),
        "expected 'not configured' from manager safety net, got: {err}"
    );
}

#[test]
fn validate_audio_still_enforces_25mb_cap() {
    // Regression: extracting resolve_audio_format() must not weaken validate_audio().
    let at_limit = vec![0u8; MAX_AUDIO_BYTES];
    assert!(validate_audio(&at_limit, "test.ogg").is_ok());
    let over_limit = vec![0u8; MAX_AUDIO_BYTES + 1];
    let err = validate_audio(&over_limit, "test.ogg").unwrap_err();
    assert!(err.to_string().contains("too large"));
}

#[tokio::test]
async fn local_whisper_rejects_oversized_audio() {
    let cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    let provider = LocalWhisperProvider::from_config(&cfg).unwrap();
    let big = vec![0u8; cfg.max_audio_bytes + 1];
    let err = provider.transcribe(&big, "voice.ogg").await.unwrap_err();
    assert!(err.to_string().contains("too large"), "got: {err}");
}

#[tokio::test]
async fn local_whisper_rejects_unsupported_format() {
    let cfg = local_whisper_config("http://127.0.0.1:9999/v1/transcribe");
    let provider = LocalWhisperProvider::from_config(&cfg).unwrap();
    let data = vec![0u8; 100];
    let err = provider.transcribe(&data, "voice.aiff").await.unwrap_err();
    assert!(
        err.to_string().contains("Unsupported audio format"),
        "got: {err}"
    );
}

// ── LocalWhisperProvider HTTP mock tests ────────────────────

#[tokio::test]
async fn local_whisper_returns_text_from_response() {
    use wiremock::matchers::{header_exists, method, path};
    use wiremock::{Mock, MockServer, ResponseTemplate};

    let server = MockServer::start().await;

    Mock::given(method("POST"))
        .and(path("/v1/transcribe"))
        .and(header_exists("authorization"))
        .respond_with(
            ResponseTemplate::new(200).set_body_json(serde_json::json!({"text": "hello world"})),
        )
        .mount(&server)
        .await;

    let cfg = local_whisper_config(&format!("{}/v1/transcribe", server.uri()));
    let provider = LocalWhisperProvider::from_config(&cfg).unwrap();

    let result = provider
        .transcribe(b"fake-audio", "voice.ogg")
        .await
        .unwrap();
    assert_eq!(result, "hello world");
}

#[tokio::test]
async fn local_whisper_sends_bearer_auth_header() {
    use wiremock::matchers::{header, method, path};
    use wiremock::{Mock, MockServer, ResponseTemplate};

    let server = MockServer::start().await;

    Mock::given(method("POST"))
        .and(path("/v1/transcribe"))
        .and(header("authorization", "Bearer test-token"))
        .respond_with(
            ResponseTemplate::new(200).set_body_json(serde_json::json!({"text": "auth ok"})),
        )
        .mount(&server)
        .await;

    let cfg = local_whisper_config(&format!("{}/v1/transcribe", server.uri()));
    let provider = LocalWhisperProvider::from_config(&cfg).unwrap();

    let result = provider
        .transcribe(b"fake-audio", "voice.ogg")
        .await
        .unwrap();
    assert_eq!(result, "auth ok");
}

#[tokio::test]
async fn local_whisper_propagates_http_error() {
    use wiremock::matchers::{method, path};
    use wiremock::{Mock, MockServer, ResponseTemplate};

    let server = MockServer::start().await;

    Mock::given(method("POST"))
        .and(path("/v1/transcribe"))
        .respond_with(
            ResponseTemplate::new(503)
                .set_body_json(serde_json::json!({"error": {"message": "service unavailable"}})),
        )
        .mount(&server)
        .await;

    let cfg = local_whisper_config(&format!("{}/v1/transcribe", server.uri()));
    let provider = LocalWhisperProvider::from_config(&cfg).unwrap();

    let err = provider
        .transcribe(b"fake-audio", "voice.ogg")
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("503") || err.to_string().contains("service unavailable"),
        "expected HTTP error, got: {err}"
    );
}

#[tokio::test]
async fn local_whisper_propagates_non_json_http_error() {
    use wiremock::matchers::{method, path};
    use wiremock::{Mock, MockServer, ResponseTemplate};

    let server = MockServer::start().await;

    Mock::given(method("POST"))
        .and(path("/v1/transcribe"))
        .respond_with(
            ResponseTemplate::new(502)
                .set_body_string("Bad Gateway")
                .insert_header("content-type", "text/plain"),
        )
        .mount(&server)
        .await;

    let cfg = local_whisper_config(&format!("{}/v1/transcribe", server.uri()));
    let provider = LocalWhisperProvider::from_config(&cfg).unwrap();

    let err = provider
        .transcribe(b"fake-audio", "voice.ogg")
        .await
        .unwrap_err();
    assert!(err.to_string().contains("502"), "got: {err}");
    assert!(
        err.to_string().contains("Bad Gateway"),
        "expected plain-text body in error, got: {err}"
    );
}

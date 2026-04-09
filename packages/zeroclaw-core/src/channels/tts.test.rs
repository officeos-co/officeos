use super::*;

fn default_tts_config() -> TtsConfig {
    TtsConfig::default()
}

#[test]
fn tts_manager_creation_with_defaults() {
    let config = default_tts_config();
    let manager = TtsManager::new(&config).unwrap();
    // No providers configured by default, so list is empty.
    assert!(manager.available_providers().is_empty());
}

#[test]
fn tts_manager_with_edge_provider() {
    let mut config = default_tts_config();
    config.default_provider = "edge".to_string();
    config.edge = Some(crate::config::EdgeTtsConfig {
        binary_path: "edge-tts".into(),
    });

    let manager = TtsManager::new(&config).unwrap();
    assert_eq!(manager.available_providers(), vec!["edge"]);
}

#[tokio::test]
async fn tts_rejects_empty_text() {
    let mut config = default_tts_config();
    config.default_provider = "edge".to_string();
    config.edge = Some(crate::config::EdgeTtsConfig {
        binary_path: "edge-tts".into(),
    });

    let manager = TtsManager::new(&config).unwrap();
    let err = manager
        .synthesize_with_provider("", "edge", "en-US-AriaNeural")
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("must not be empty"),
        "expected empty-text error, got: {err}"
    );
}

#[tokio::test]
async fn tts_rejects_text_exceeding_max_length() {
    let mut config = default_tts_config();
    config.default_provider = "edge".to_string();
    config.max_text_length = 10;
    config.edge = Some(crate::config::EdgeTtsConfig {
        binary_path: "edge-tts".into(),
    });

    let manager = TtsManager::new(&config).unwrap();
    let long_text = "a".repeat(11);
    let err = manager
        .synthesize_with_provider(&long_text, "edge", "en-US-AriaNeural")
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("too long"),
        "expected too-long error, got: {err}"
    );
}

#[tokio::test]
async fn tts_rejects_unknown_provider() {
    let config = default_tts_config();
    let manager = TtsManager::new(&config).unwrap();
    let err = manager
        .synthesize_with_provider("hello", "nonexistent", "voice")
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("not configured"),
        "expected not-configured error, got: {err}"
    );
}

#[test]
fn piper_provider_creation() {
    let provider = PiperTtsProvider::new("http://127.0.0.1:5000/v1/audio/speech");
    assert_eq!(provider.name(), "piper");
    assert_eq!(provider.api_url, "http://127.0.0.1:5000/v1/audio/speech");
    assert_eq!(provider.supported_formats(), vec!["mp3", "wav", "opus"]);
    // Piper voices depend on installed models; list is empty.
    assert!(provider.supported_voices().is_empty());
}

#[test]
fn tts_manager_with_piper_provider() {
    let mut config = default_tts_config();
    config.default_provider = "piper".to_string();
    config.piper = Some(crate::config::PiperTtsConfig {
        api_url: "http://127.0.0.1:5000/v1/audio/speech".into(),
    });

    let manager = TtsManager::new(&config).unwrap();
    assert_eq!(manager.available_providers(), vec!["piper"]);
}

#[tokio::test]
async fn tts_rejects_empty_text_for_piper() {
    let mut config = default_tts_config();
    config.default_provider = "piper".to_string();
    config.piper = Some(crate::config::PiperTtsConfig {
        api_url: "http://127.0.0.1:5000/v1/audio/speech".into(),
    });

    let manager = TtsManager::new(&config).unwrap();
    let err = manager
        .synthesize_with_provider("", "piper", "default")
        .await
        .unwrap_err();
    assert!(
        err.to_string().contains("must not be empty"),
        "expected empty-text error, got: {err}"
    );
}

#[test]
fn piper_not_registered_when_config_is_none() {
    let config = default_tts_config();
    let manager = TtsManager::new(&config).unwrap();
    assert!(!manager.available_providers().contains(&"piper".to_string()));
}

#[test]
fn tts_config_defaults() {
    let config = TtsConfig::default();
    assert!(!config.enabled);
    assert_eq!(config.default_provider, "openai");
    assert_eq!(config.default_voice, "alloy");
    assert_eq!(config.default_format, "mp3");
    assert_eq!(config.max_text_length, DEFAULT_MAX_TEXT_LENGTH);
    assert!(config.openai.is_none());
    assert!(config.elevenlabs.is_none());
    assert!(config.google.is_none());
    assert!(config.edge.is_none());
    assert!(config.piper.is_none());
}

#[test]
fn tts_manager_max_text_length_zero_uses_default() {
    let mut config = default_tts_config();
    config.max_text_length = 0;
    let manager = TtsManager::new(&config).unwrap();
    assert_eq!(manager.max_text_length, DEFAULT_MAX_TEXT_LENGTH);
}

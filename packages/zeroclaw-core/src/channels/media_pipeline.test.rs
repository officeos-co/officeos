use super::*;

fn default_pipeline_config(enabled: bool) -> MediaPipelineConfig {
    MediaPipelineConfig {
        enabled,
        transcribe_audio: true,
        describe_images: true,
        summarize_video: true,
    }
}

fn sample_audio() -> MediaAttachment {
    MediaAttachment {
        file_name: "voice.ogg".to_string(),
        data: vec![0u8; 100],
        mime_type: Some("audio/ogg".to_string()),
    }
}

fn sample_image() -> MediaAttachment {
    MediaAttachment {
        file_name: "photo.jpg".to_string(),
        data: vec![0u8; 50],
        mime_type: Some("image/jpeg".to_string()),
    }
}

fn sample_video() -> MediaAttachment {
    MediaAttachment {
        file_name: "clip.mp4".to_string(),
        data: vec![0u8; 200],
        mime_type: Some("video/mp4".to_string()),
    }
}

#[test]
fn media_kind_from_mime() {
    let audio = MediaAttachment {
        file_name: "file".to_string(),
        data: vec![],
        mime_type: Some("audio/ogg".to_string()),
    };
    assert_eq!(audio.kind(), MediaKind::Audio);

    let image = MediaAttachment {
        file_name: "file".to_string(),
        data: vec![],
        mime_type: Some("image/png".to_string()),
    };
    assert_eq!(image.kind(), MediaKind::Image);

    let video = MediaAttachment {
        file_name: "file".to_string(),
        data: vec![],
        mime_type: Some("video/mp4".to_string()),
    };
    assert_eq!(video.kind(), MediaKind::Video);
}

#[test]
fn media_kind_from_extension() {
    let audio = MediaAttachment {
        file_name: "voice.ogg".to_string(),
        data: vec![],
        mime_type: None,
    };
    assert_eq!(audio.kind(), MediaKind::Audio);

    let image = MediaAttachment {
        file_name: "photo.png".to_string(),
        data: vec![],
        mime_type: None,
    };
    assert_eq!(image.kind(), MediaKind::Image);

    let video = MediaAttachment {
        file_name: "clip.mp4".to_string(),
        data: vec![],
        mime_type: None,
    };
    assert_eq!(video.kind(), MediaKind::Video);

    let unknown = MediaAttachment {
        file_name: "data.bin".to_string(),
        data: vec![],
        mime_type: None,
    };
    assert_eq!(unknown.kind(), MediaKind::Unknown);
}

#[tokio::test]
async fn disabled_pipeline_returns_original_text() {
    let config = default_pipeline_config(false);
    let tc = TranscriptionConfig::default();
    let pipeline = MediaPipeline::new(&config, &tc, false);

    let result = pipeline.process("hello", &[sample_audio()]).await;
    assert_eq!(result, "hello");
}

#[tokio::test]
async fn empty_attachments_returns_original_text() {
    let config = default_pipeline_config(true);
    let tc = TranscriptionConfig::default();
    let pipeline = MediaPipeline::new(&config, &tc, false);

    let result = pipeline.process("hello", &[]).await;
    assert_eq!(result, "hello");
}

#[tokio::test]
async fn image_annotation_with_vision() {
    let config = default_pipeline_config(true);
    let tc = TranscriptionConfig::default();
    let pipeline = MediaPipeline::new(&config, &tc, true);

    let result = pipeline.process("check this", &[sample_image()]).await;
    assert!(
        result.contains("[Image: photo.jpg attached, will be processed by vision model]"),
        "expected vision annotation, got: {result}"
    );
    assert!(result.contains("check this"));
}

#[tokio::test]
async fn image_annotation_without_vision() {
    let config = default_pipeline_config(true);
    let tc = TranscriptionConfig::default();
    let pipeline = MediaPipeline::new(&config, &tc, false);

    let result = pipeline.process("check this", &[sample_image()]).await;
    assert!(
        result.contains("[Image: photo.jpg attached]"),
        "expected basic image annotation, got: {result}"
    );
}

#[tokio::test]
async fn video_annotation() {
    let config = default_pipeline_config(true);
    let tc = TranscriptionConfig::default();
    let pipeline = MediaPipeline::new(&config, &tc, false);

    let result = pipeline.process("watch", &[sample_video()]).await;
    assert!(
        result.contains("[Video: clip.mp4 attached]"),
        "expected video annotation, got: {result}"
    );
}

#[tokio::test]
async fn audio_without_transcription_enabled() {
    let config = default_pipeline_config(true);
    let mut tc = TranscriptionConfig::default();
    tc.enabled = false;
    let pipeline = MediaPipeline::new(&config, &tc, false);

    let result = pipeline.process("", &[sample_audio()]).await;
    assert_eq!(result, "[Audio: attached]");
}

#[tokio::test]
async fn multiple_attachments_produce_multiple_annotations() {
    let config = default_pipeline_config(true);
    let mut tc = TranscriptionConfig::default();
    tc.enabled = false;
    let pipeline = MediaPipeline::new(&config, &tc, false);

    let attachments = vec![sample_audio(), sample_image(), sample_video()];
    let result = pipeline.process("context", &attachments).await;

    assert!(
        result.contains("[Audio: attached]"),
        "missing audio annotation"
    );
    assert!(
        result.contains("[Image: photo.jpg attached]"),
        "missing image annotation"
    );
    assert!(
        result.contains("[Video: clip.mp4 attached]"),
        "missing video annotation"
    );
    assert!(result.contains("context"), "missing original text");
}

#[tokio::test]
async fn disabled_sub_features_skip_processing() {
    let config = MediaPipelineConfig {
        enabled: true,
        transcribe_audio: false,
        describe_images: false,
        summarize_video: false,
    };
    let tc = TranscriptionConfig::default();
    let pipeline = MediaPipeline::new(&config, &tc, false);

    let attachments = vec![sample_audio(), sample_image(), sample_video()];
    let result = pipeline.process("hello", &attachments).await;
    assert_eq!(result, "hello");
}

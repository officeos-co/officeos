import { describe, it } from "bun:test";

describe("video-transcript skill", () => {
  describe("get_info", () => {
    it.todo("should POST to /info with url");
    it.todo("should return title, duration, uploader from proxy response");
    it.todo("should count formats array length");
    it.todo("should return null for missing optional fields");
    it.todo("should throw on proxy non-ok response");
  });

  describe("list_subtitles", () => {
    it.todo("should POST to /subtitles with url");
    it.todo("should return subtitles and automatic_captions records");
    it.todo("should return empty records when no subtitles available");
  });

  describe("get_subtitles", () => {
    it.todo("should POST to /get-subtitles with url, lang, format, auto_generated");
    it.todo("should default lang to en");
    it.todo("should default format to vtt");
    it.todo("should return content string");
  });

  describe("get_transcript", () => {
    it.todo("should POST to /transcript with all params");
    it.todo("should return transcript text and source field");
    it.todo("should count words correctly");
    it.todo("should indicate source as subtitles or whisper");
    it.todo("should pass whisper_model to proxy");
  });

  describe("download_audio", () => {
    it.todo("should POST to /download-audio with url, format, quality");
    it.todo("should default format to mp3");
    it.todo("should default quality to 5");
    it.todo("should return download_url, filename, duration_seconds");
    it.todo("should handle file_size_bytes from both filesize and file_size_bytes keys");
  });

  describe("download_video", () => {
    it.todo("should POST to /download-video with url, quality, format");
    it.todo("should default quality to 720");
    it.todo("should default format to mp4");
    it.todo("should pass include_subtitles flag");
    it.todo("should return resolution field");
  });

  describe("transcribe", () => {
    it.todo("should POST to /transcribe with url and model");
    it.todo("should pass language when provided");
    it.todo("should pass translate_to_english flag");
    it.todo("should pass word_timestamps flag");
    it.todo("should return text, language, duration_seconds, and segments");
    it.todo("should map segment id, start, end, text from response");
    it.todo("should include words array in segments when word_timestamps is true");
  });

  describe("proxy error handling", () => {
    it.todo("should include endpoint path in error message");
    it.todo("should include HTTP status code in error message");
    it.todo("should truncate long error bodies to 300 chars");
  });
});

import { defineSkill, z } from "@harro/skill-sdk";

import manifest from "./skill.json" with { type: "json" };
async function proxyPost(
  ctx: { fetch: typeof globalThis.fetch; credentials: Record<string, string> },
  endpoint: string,
  body: unknown,
): Promise<any> {
  const base = ctx.credentials.proxy_url.replace(/\/$/, "");
  const res = await ctx.fetch(`${base}${endpoint}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(`Video-transcript proxy ${res.status} at ${endpoint}: ${text.slice(0, 300)}`);
  }
  return res.json();
}

import doc from "./SKILL.md";

export default defineSkill({
  ...manifest,
  doc,

  actions: {
    // ── Metadata ───────────────────────────────────────────────────────

    get_info: {
      description:
        "Get video metadata (title, duration, description, channel, available formats) without downloading.",
      params: z.object({
        url: z.string().url().describe("Video URL — YouTube, Vimeo, Twitter/X, TikTok, etc."),
      }),
      returns: z.object({
        id: z.string().nullable(),
        title: z.string(),
        description: z.string().nullable(),
        uploader: z.string().nullable(),
        channel_url: z.string().nullable(),
        duration: z.number().nullable(),
        view_count: z.number().nullable(),
        upload_date: z.string().nullable(),
        thumbnail: z.string().nullable(),
        webpage_url: z.string(),
        formats_count: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyPost(ctx, "/info", { url: params.url });
        return {
          id: data.id ?? null,
          title: data.title ?? "",
          description: data.description ?? null,
          uploader: data.uploader ?? null,
          channel_url: data.channel_url ?? null,
          duration: data.duration ?? null,
          view_count: data.view_count ?? null,
          upload_date: data.upload_date ?? null,
          thumbnail: data.thumbnail ?? null,
          webpage_url: data.webpage_url ?? params.url,
          formats_count: Array.isArray(data.formats) ? data.formats.length : 0,
        };
      },
    },

    // ── Subtitles ──────────────────────────────────────────────────────

    list_subtitles: {
      description: "List all available subtitle and caption tracks for a video.",
      params: z.object({
        url: z.string().url().describe("Video URL"),
      }),
      returns: z.object({
        subtitles: z.record(
          z.array(z.object({ ext: z.string(), url: z.string().optional() })),
        ),
        automatic_captions: z.record(
          z.array(z.object({ ext: z.string(), url: z.string().optional() })),
        ),
      }),
      execute: async (params, ctx) => {
        const data = await proxyPost(ctx, "/subtitles", { url: params.url });
        return {
          subtitles: data.subtitles ?? {},
          automatic_captions: data.automatic_captions ?? {},
        };
      },
    },

    get_subtitles: {
      description: "Download a specific subtitle track and return its content as text.",
      params: z.object({
        url: z.string().url().describe("Video URL"),
        lang: z.string().default("en").describe("Language code e.g. en, de, fr, es, ja"),
        format: z
          .enum(["vtt", "srt", "json3"])
          .default("vtt")
          .describe("Subtitle format: vtt (WebVTT), srt (SubRip), json3"),
        auto_generated: z
          .boolean()
          .default(false)
          .describe("Accept auto-generated captions if manual not available"),
      }),
      returns: z.object({
        lang: z.string(),
        format: z.string(),
        content: z.string(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyPost(ctx, "/get-subtitles", {
          url: params.url,
          lang: params.lang,
          format: params.format,
          auto_generated: params.auto_generated,
        });
        return {
          lang: data.lang ?? params.lang,
          format: data.format ?? params.format,
          content: data.content ?? "",
        };
      },
    },

    get_transcript: {
      description:
        "Get the full plain-text transcript for a video. Uses native subtitles first, falls back to Whisper.",
      params: z.object({
        url: z.string().url().describe("Video URL"),
        lang: z.string().default("en").describe("Preferred language for subtitles"),
        whisper_fallback: z
          .boolean()
          .default(true)
          .describe("Transcribe with Whisper if no native subtitles available"),
        whisper_model: z
          .enum(["tiny", "base", "small", "medium", "large"])
          .default("base")
          .describe("Whisper model to use if transcribing"),
      }),
      returns: z.object({
        transcript: z.string(),
        source: z.enum(["subtitles", "whisper"]),
        lang: z.string(),
        word_count: z.number(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyPost(ctx, "/transcript", {
          url: params.url,
          lang: params.lang,
          whisper_fallback: params.whisper_fallback,
          whisper_model: params.whisper_model,
        });
        const transcript: string = data.transcript ?? "";
        return {
          transcript,
          source: data.source ?? "subtitles",
          lang: data.lang ?? params.lang,
          word_count: transcript.split(/\s+/).filter(Boolean).length,
        };
      },
    },

    // ── Downloads ──────────────────────────────────────────────────────

    download_audio: {
      description: "Extract audio from a video and return a download URL.",
      params: z.object({
        url: z.string().url().describe("Video URL"),
        format: z
          .enum(["mp3", "m4a", "wav", "opus"])
          .default("mp3")
          .describe("Audio output format"),
        quality: z
          .union([z.literal(0), z.literal(5), z.literal(9)])
          .default(5)
          .describe("Audio quality: 0=best, 5=medium, 9=worst (VBR scale)"),
      }),
      returns: z.object({
        download_url: z.string(),
        filename: z.string(),
        duration_seconds: z.number().nullable(),
        file_size_bytes: z.number().nullable(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyPost(ctx, "/download-audio", {
          url: params.url,
          format: params.format,
          quality: params.quality,
        });
        return {
          download_url: data.download_url ?? data.url ?? "",
          filename: data.filename ?? "",
          duration_seconds: data.duration_seconds ?? null,
          file_size_bytes: data.file_size_bytes ?? data.filesize ?? null,
        };
      },
    },

    download_video: {
      description: "Download a video in a specified quality and format.",
      params: z.object({
        url: z.string().url().describe("Video URL"),
        quality: z
          .enum(["best", "1080", "720", "480", "360"])
          .default("720")
          .describe("Video quality: best, 1080p, 720p, 480p, or 360p"),
        format: z
          .enum(["mp4", "webm", "mkv"])
          .default("mp4")
          .describe("Video container format"),
        include_subtitles: z
          .boolean()
          .default(false)
          .describe("Embed available subtitles into the video"),
      }),
      returns: z.object({
        download_url: z.string(),
        filename: z.string(),
        duration_seconds: z.number().nullable(),
        file_size_bytes: z.number().nullable(),
        resolution: z.string().nullable(),
      }),
      execute: async (params, ctx) => {
        const data = await proxyPost(ctx, "/download-video", {
          url: params.url,
          quality: params.quality,
          format: params.format,
          include_subtitles: params.include_subtitles,
        });
        return {
          download_url: data.download_url ?? data.url ?? "",
          filename: data.filename ?? "",
          duration_seconds: data.duration_seconds ?? null,
          file_size_bytes: data.file_size_bytes ?? data.filesize ?? null,
          resolution: data.resolution ?? null,
        };
      },
    },

    // ── Transcription ──────────────────────────────────────────────────

    transcribe: {
      description:
        "Transcribe a video or audio URL using Whisper. Best for content without native subtitles.",
      params: z.object({
        url: z.string().url().describe("Video or audio URL to transcribe"),
        model: z
          .enum(["tiny", "base", "small", "medium", "large"])
          .default("base")
          .describe("Whisper model: tiny (fastest) → large (most accurate)"),
        language: z
          .string()
          .optional()
          .describe("Force language code e.g. en, de, fr (auto-detects if omitted)"),
        translate_to_english: z
          .boolean()
          .default(false)
          .describe("Translate non-English audio to English"),
        word_timestamps: z
          .boolean()
          .default(false)
          .describe("Include per-word start/end timestamps in segments"),
      }),
      returns: z.object({
        text: z.string(),
        language: z.string(),
        duration_seconds: z.number().nullable(),
        segments: z.array(
          z.object({
            id: z.number(),
            start: z.number(),
            end: z.number(),
            text: z.string(),
            words: z
              .array(z.object({ word: z.string(), start: z.number(), end: z.number() }))
              .optional(),
          }),
        ),
      }),
      execute: async (params, ctx) => {
        const data = await proxyPost(ctx, "/transcribe", {
          url: params.url,
          model: params.model,
          language: params.language,
          translate_to_english: params.translate_to_english,
          word_timestamps: params.word_timestamps,
        });
        return {
          text: data.text ?? "",
          language: data.language ?? "unknown",
          duration_seconds: data.duration ?? null,
          segments: (data.segments ?? []).map((s: any, i: number) => ({
            id: s.id ?? i,
            start: s.start,
            end: s.end,
            text: s.text ?? "",
            words: s.words,
          })),
        };
      },
    },
  },
});

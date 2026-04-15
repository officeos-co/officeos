# Video Transcript Skill

Extract metadata, subtitles, and transcripts from videos on YouTube and 1000+ other sites. Downloads audio/video and transcribes speech via Whisper. Requires a self-hosted proxy service wrapping yt-dlp and Whisper.

## Credentials

| Key | Description |
|---|---|
| `proxy_url` | URL of your self-hosted yt-dlp/Whisper proxy service e.g. `http://localhost:8765` |

## Actions

### Metadata

#### `get_info`
Get video metadata without downloading: title, duration, description, channel, formats.

**Params**
| Name | Type | Description |
|---|---|---|
| `url` | `string` | Video URL (YouTube, Vimeo, Twitter/X, TikTok, etc.) |

**Returns** Object with id, title, description, uploader, duration, view_count, upload_date, formats summary.

---

### Subtitles & Transcripts

#### `list_subtitles`
List all available subtitle/caption tracks for a video.

**Params**
| Name | Type | Description |
|---|---|---|
| `url` | `string` | Video URL |

**Returns** Object with `subtitles` (manual) and `automatic_captions` records keyed by language code, each with available formats.

---

#### `get_subtitles`
Download a specific subtitle track and return its content.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `url` | `string` | — | Video URL |
| `lang` | `string` | `en` | Language code e.g. `en`, `de`, `fr` |
| `format` | `vtt \| srt \| json3` | `vtt` | Subtitle format |
| `auto_generated` | `boolean` | `false` | Use auto-generated captions if manual unavailable |

**Returns** Object with `lang`, `format`, `content` (raw subtitle text).

---

#### `get_transcript`
Get the full transcript text for a video. Uses native subtitles if available, falls back to Whisper transcription.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `url` | `string` | — | Video URL |
| `lang` | `string` | `en` | Preferred language for subtitles |
| `whisper_fallback` | `boolean` | `true` | Transcribe with Whisper if no subtitles found |
| `whisper_model` | `tiny \| base \| small \| medium \| large` | `base` | Whisper model to use for fallback |

**Returns** Object with `transcript` (plain text), `source` (`subtitles` or `whisper`), `lang`.

---

### Downloads

#### `download_audio`
Extract audio from a video and return a download URL or local path.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `url` | `string` | — | Video URL |
| `format` | `mp3 \| m4a \| wav \| opus` | `mp3` | Audio format |
| `quality` | `0 \| 5 \| 9` | `5` | Audio quality: 0=best, 9=worst (VBR scale) |

**Returns** Object with `download_url`, `filename`, `duration_seconds`, `file_size_bytes`.

---

#### `download_video`
Download a video in a specified quality/format.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `url` | `string` | — | Video URL |
| `quality` | `best \| 1080 \| 720 \| 480 \| 360` | `720` | Video quality |
| `format` | `mp4 \| webm \| mkv` | `mp4` | Video container format |
| `include_subtitles` | `boolean` | `false` | Embed available subtitles |

**Returns** Object with `download_url`, `filename`, `duration_seconds`, `file_size_bytes`, `resolution`.

---

### Transcription

#### `transcribe`
Transcribe an audio or video URL using Whisper. Best for content without native subtitles.

**Params**
| Name | Type | Default | Description |
|---|---|---|---|
| `url` | `string` | — | Video or audio URL |
| `model` | `tiny \| base \| small \| medium \| large` | `base` | Whisper model size |
| `language` | `string` | — | Force language code e.g. `en`, `de` (auto-detect if omitted) |
| `translate_to_english` | `boolean` | `false` | Translate non-English audio to English |
| `word_timestamps` | `boolean` | `false` | Include per-word timestamps |

**Returns** Object with `text` (full transcript), `language` (detected/forced), `segments` array with start/end times.

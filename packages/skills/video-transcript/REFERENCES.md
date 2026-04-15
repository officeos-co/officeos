# Video Transcript Skill — References

## Backend tools
This skill uses a **proxy pattern**: it delegates all heavy operations to a self-hosted service that wraps:

### yt-dlp
- **Repo**: https://github.com/yt-dlp/yt-dlp
- **License**: Unlicense (public domain)
- **Supported sites**: 1000+ (YouTube, Vimeo, Twitter/X, TikTok, etc.)
- **Key capabilities**: video info, subtitle extraction, audio/video download, format selection

### Whisper (OpenAI)
- **Repo**: https://github.com/openai/whisper
- **License**: MIT
- **Capability**: Speech-to-text transcription of audio files
- **Models**: tiny, base, small, medium, large

## Proxy service contract
The skill expects a `proxy_url` pointing to a service that exposes:

| Path | Method | Description |
|---|---|---|
| `/info` | POST | Get video metadata (yt-dlp --dump-json) |
| `/transcript` | POST | Fetch native subtitles/transcript |
| `/subtitles` | POST | List available subtitle tracks |
| `/get-subtitles` | POST | Download specific subtitle track |
| `/download-audio` | POST | Extract audio and return URL/path |
| `/download-video` | POST | Download video in specified format |
| `/transcribe` | POST | Transcribe audio via Whisper |

## Reference proxy implementation
A minimal proxy can be built with:
```python
# fastapi + yt-dlp + whisper
import yt_dlp, whisper
from fastapi import FastAPI
```

See `apps/backend/docs/video-transcript-proxy.md` for full proxy spec.

## Supported video sources (via yt-dlp)
YouTube, Vimeo, Twitter/X, TikTok, Instagram, Dailymotion, Twitch VODs, and 1000+ others.

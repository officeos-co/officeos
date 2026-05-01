"""
social.youtube — YouTube Data API v3 client.

Handles: video upload, Shorts repurposing, thumbnail setting,
         search, channel stats. OAuth2 token refresh built in.
"""
from __future__ import annotations

import json
import logging
import mimetypes
import os
import time
from pathlib import Path
from typing import Any

import httpx

logger = logging.getLogger(__name__)

_API_BASE = "https://www.googleapis.com/youtube/v3"
_UPLOAD_BASE = "https://www.googleapis.com/upload/youtube/v3"
_OAUTH_TOKEN_URL = "https://oauth2.googleapis.com/token"
_CHUNK_SIZE = 5 * 1024 * 1024  # 5 MB resumable upload chunks


class YouTubeClient:
    """YouTube Data API v3 client with OAuth2 token refresh."""

    def __init__(
        self,
        client_id: str,
        client_secret: str,
        refresh_token: str,
        access_token: str = "",
        token_expiry: float = 0.0,
    ) -> None:
        self._client_id = client_id
        self._client_secret = client_secret
        self._refresh_token = refresh_token
        self._access_token = access_token
        self._token_expiry = token_expiry

    @classmethod
    def from_env(cls) -> "YouTubeClient":
        return cls(
            client_id=os.environ["YOUTUBE_CLIENT_ID"],
            client_secret=os.environ["YOUTUBE_CLIENT_SECRET"],
            refresh_token=os.environ["YOUTUBE_REFRESH_TOKEN"],
        )

    # ------------------------------------------------------------------
    # Token management
    # ------------------------------------------------------------------

    async def _ensure_token(self) -> str:
        if self._access_token and time.time() < self._token_expiry - 60:
            return self._access_token
        async with httpx.AsyncClient() as client:
            resp = await client.post(_OAUTH_TOKEN_URL, data={
                "client_id": self._client_id,
                "client_secret": self._client_secret,
                "refresh_token": self._refresh_token,
                "grant_type": "refresh_token",
            })
            resp.raise_for_status()
            data = resp.json()
            self._access_token = data["access_token"]
            self._token_expiry = time.time() + data.get("expires_in", 3600)
        return self._access_token

    async def _headers(self) -> dict[str, str]:
        token = await self._ensure_token()
        return {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}

    # ------------------------------------------------------------------
    # Upload
    # ------------------------------------------------------------------

    async def upload_video(
        self,
        file_path: str,
        title: str,
        description: str = "",
        tags: list[str] = None,
        category_id: str = "22",  # People & Blogs
        privacy: str = "public",
        is_short: bool = False,
    ) -> dict[str, Any]:
        """
        Upload a video using the resumable upload protocol.
        Returns the YouTube video resource (includes videoId).
        """
        path = Path(file_path)
        if not path.exists():
            raise FileNotFoundError(f"Video file not found: {file_path}")

        file_size = path.stat().st_size
        mime_type = mimetypes.guess_type(str(path))[0] or "video/mp4"

        body = {
            "snippet": {
                "title": title[:100],
                "description": description[:5000],
                "tags": (tags or [])[:500],
                "categoryId": category_id,
            },
            "status": {"privacyStatus": privacy},
        }

        headers = await self._headers()
        headers["X-Upload-Content-Type"] = mime_type
        headers["X-Upload-Content-Length"] = str(file_size)
        headers["Content-Type"] = "application/json; charset=UTF-8"

        # Step 1: initiate resumable upload
        async with httpx.AsyncClient(timeout=30) as client:
            init_resp = await client.post(
                f"{_UPLOAD_BASE}/videos?uploadType=resumable&part=snippet,status",
                headers=headers,
                content=json.dumps(body).encode(),
            )
            init_resp.raise_for_status()
            upload_url = init_resp.headers["Location"]

        # Step 2: upload in chunks
        video_resource = await self._upload_chunks(upload_url, path, file_size, mime_type)
        logger.info("youtube.upload: videoId=%s title=%r", video_resource.get("id"), title)
        return video_resource

    async def _upload_chunks(
        self, upload_url: str, path: Path, file_size: int, mime_type: str
    ) -> dict[str, Any]:
        offset = 0
        async with httpx.AsyncClient(timeout=120) as client:
            with path.open("rb") as f:
                while offset < file_size:
                    chunk = f.read(_CHUNK_SIZE)
                    end = offset + len(chunk) - 1
                    headers = {
                        "Content-Range": f"bytes {offset}-{end}/{file_size}",
                        "Content-Type": mime_type,
                    }
                    resp = await client.put(upload_url, headers=headers, content=chunk)
                    if resp.status_code in (200, 201):
                        return resp.json()
                    if resp.status_code == 308:  # Resume Incomplete
                        offset = int(resp.headers.get("Range", f"bytes=0-{end}").split("-")[1]) + 1
                    else:
                        resp.raise_for_status()
        raise RuntimeError("Upload ended without receiving final response")

    # ------------------------------------------------------------------
    # Shorts repurposing
    # ------------------------------------------------------------------

    async def create_short(
        self,
        file_path: str,
        title: str,
        description: str = "",
        tags: list[str] = None,
    ) -> dict[str, Any]:
        """Upload a video as a YouTube Short (≤60s vertical video)."""
        tags = list(tags or [])
        if "#Shorts" not in tags:
            tags.insert(0, "#Shorts")
        short_description = f"#Shorts\n\n{description}"
        return await self.upload_video(
            file_path=file_path,
            title=title,
            description=short_description,
            tags=tags,
            privacy="public",
            is_short=True,
        )

    # ------------------------------------------------------------------
    # Thumbnail
    # ------------------------------------------------------------------

    async def set_thumbnail(self, video_id: str, image_path: str) -> dict[str, Any]:
        path = Path(image_path)
        mime_type = mimetypes.guess_type(str(path))[0] or "image/jpeg"
        token = await self._ensure_token()
        async with httpx.AsyncClient(timeout=60) as client:
            resp = await client.post(
                f"{_UPLOAD_BASE}/thumbnails/set?videoId={video_id}&uploadType=media",
                headers={"Authorization": f"Bearer {token}", "Content-Type": mime_type},
                content=path.read_bytes(),
            )
            resp.raise_for_status()
            return resp.json()

    # ------------------------------------------------------------------
    # Search
    # ------------------------------------------------------------------

    async def search_videos(
        self, query: str, max_results: int = 10, order: str = "viewCount"
    ) -> list[dict[str, Any]]:
        headers = await self._headers()
        async with httpx.AsyncClient(timeout=20) as client:
            resp = await client.get(
                f"{_API_BASE}/search",
                headers=headers,
                params={
                    "part": "snippet",
                    "q": query,
                    "type": "video",
                    "maxResults": max_results,
                    "order": order,
                },
            )
            resp.raise_for_status()
            items = resp.json().get("items", [])
            return [
                {
                    "video_id": item["id"]["videoId"],
                    "title": item["snippet"]["title"],
                    "channel": item["snippet"]["channelTitle"],
                    "published_at": item["snippet"]["publishedAt"],
                    "description": item["snippet"]["description"][:200],
                }
                for item in items
            ]

    # ------------------------------------------------------------------
    # Stats
    # ------------------------------------------------------------------

    async def get_video_stats(self, video_id: str) -> dict[str, Any]:
        headers = await self._headers()
        async with httpx.AsyncClient(timeout=20) as client:
            resp = await client.get(
                f"{_API_BASE}/videos",
                headers=headers,
                params={"part": "statistics,snippet", "id": video_id},
            )
            resp.raise_for_status()
            items = resp.json().get("items", [])
            if not items:
                return {}
            item = items[0]
            return {
                "video_id": video_id,
                "title": item["snippet"]["title"],
                "view_count": int(item["statistics"].get("viewCount", 0)),
                "like_count": int(item["statistics"].get("likeCount", 0)),
                "comment_count": int(item["statistics"].get("commentCount", 0)),
            }

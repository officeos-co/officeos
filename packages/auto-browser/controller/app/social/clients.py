"""
social.instagram — Instagram Graph API client.
social.reddit — Reddit API client (OAuth2 password flow).
social.twitter — X API v2 client (chunked media upload).
"""
from __future__ import annotations

import asyncio
import base64
import logging
import os
import time
from pathlib import Path
from typing import Any

import httpx

logger = logging.getLogger(__name__)


# ===========================================================================
# Instagram Graph API
# ===========================================================================

_IG_API = "https://graph.instagram.com/v19.0"
_FB_API = "https://graph.facebook.com/v19.0"


class InstagramClient:
    """Instagram Graph API — image posts, Reels, carousels."""

    def __init__(self, access_token: str, ig_user_id: str) -> None:
        self._token = access_token
        self._user_id = ig_user_id

    @classmethod
    def from_env(cls) -> "InstagramClient":
        return cls(
            access_token=os.environ["INSTAGRAM_ACCESS_TOKEN"],
            ig_user_id=os.environ["INSTAGRAM_USER_ID"],
        )

    async def _post(self, url: str, params: dict) -> dict[str, Any]:
        params["access_token"] = self._token
        async with httpx.AsyncClient(timeout=60) as client:
            resp = await client.post(url, params=params)
            resp.raise_for_status()
            return resp.json()

    async def _get(self, url: str, params: dict = None) -> dict[str, Any]:
        p = dict(params or {})
        p["access_token"] = self._token
        async with httpx.AsyncClient(timeout=30) as client:
            resp = await client.get(url, params=p)
            resp.raise_for_status()
            return resp.json()

    async def post_image(self, image_url: str, caption: str = "") -> dict[str, Any]:
        """Post a single image. image_url must be publicly accessible."""
        container = await self._post(
            f"{_FB_API}/{self._user_id}/media",
            {"image_url": image_url, "caption": caption[:2200]},
        )
        container_id = container["id"]
        result = await self._post(
            f"{_FB_API}/{self._user_id}/media_publish",
            {"creation_id": container_id},
        )
        logger.info("instagram.post_image: media_id=%s", result.get("id"))
        return result

    async def post_reel(
        self, video_url: str, caption: str = "", cover_url: str = ""
    ) -> dict[str, Any]:
        """Upload a Reel. Polls for video processing completion before publishing."""
        params = {
            "media_type": "REELS",
            "video_url": video_url,
            "caption": caption[:2200],
        }
        if cover_url:
            params["cover_url"] = cover_url

        container = await self._post(f"{_FB_API}/{self._user_id}/media", params)
        container_id = container["id"]

        # Poll for processing
        await self._wait_for_media_ready(container_id)

        result = await self._post(
            f"{_FB_API}/{self._user_id}/media_publish",
            {"creation_id": container_id},
        )
        logger.info("instagram.post_reel: media_id=%s", result.get("id"))
        return result

    async def _wait_for_media_ready(
        self, container_id: str, max_wait: int = 180, interval: int = 5
    ) -> None:
        elapsed = 0
        while elapsed < max_wait:
            data = await self._get(
                f"{_FB_API}/{container_id}",
                {"fields": "status_code,status"},
            )
            status = data.get("status_code", "")
            if status == "FINISHED":
                return
            if status == "ERROR":
                raise RuntimeError(f"Instagram media processing failed: {data.get('status')}")
            await asyncio.sleep(interval)
            elapsed += interval
        raise TimeoutError(f"Instagram media container {container_id} not ready after {max_wait}s")

    async def post_carousel(
        self, image_urls: list[str], caption: str = ""
    ) -> dict[str, Any]:
        """Post a carousel of up to 10 images."""
        if len(image_urls) > 10:
            image_urls = image_urls[:10]
        children = []
        for url in image_urls:
            c = await self._post(
                f"{_FB_API}/{self._user_id}/media",
                {"image_url": url, "is_carousel_item": "true"},
            )
            children.append(c["id"])

        container = await self._post(
            f"{_FB_API}/{self._user_id}/media",
            {
                "media_type": "CAROUSEL",
                "children": ",".join(children),
                "caption": caption[:2200],
            },
        )
        result = await self._post(
            f"{_FB_API}/{self._user_id}/media_publish",
            {"creation_id": container["id"]},
        )
        logger.info("instagram.post_carousel: media_id=%s", result.get("id"))
        return result


# ===========================================================================
# Reddit API (OAuth2 password flow)
# ===========================================================================

_REDDIT_API = "https://oauth.reddit.com"
_REDDIT_AUTH = "https://www.reddit.com/api/v1/access_token"


class RedditClient:
    """Reddit API — text, link, and video submissions."""

    def __init__(
        self,
        client_id: str,
        client_secret: str,
        username: str,
        password: str,
        user_agent: str = "auto-browser/1.0",
    ) -> None:
        self._client_id = client_id
        self._client_secret = client_secret
        self._username = username
        self._password = password
        self._user_agent = user_agent
        self._access_token = ""
        self._token_expiry = 0.0

    @classmethod
    def from_env(cls) -> "RedditClient":
        return cls(
            client_id=os.environ["REDDIT_CLIENT_ID"],
            client_secret=os.environ["REDDIT_CLIENT_SECRET"],
            username=os.environ["REDDIT_USERNAME"],
            password=os.environ["REDDIT_PASSWORD"],
        )

    async def _ensure_token(self) -> str:
        if self._access_token and time.time() < self._token_expiry - 30:
            return self._access_token
        creds = base64.b64encode(f"{self._client_id}:{self._client_secret}".encode()).decode()
        async with httpx.AsyncClient(timeout=20) as client:
            resp = await client.post(
                _REDDIT_AUTH,
                headers={
                    "Authorization": f"Basic {creds}",
                    "User-Agent": self._user_agent,
                },
                data={
                    "grant_type": "password",
                    "username": self._username,
                    "password": self._password,
                },
            )
            resp.raise_for_status()
            data = resp.json()
            self._access_token = data["access_token"]
            self._token_expiry = time.time() + data.get("expires_in", 3600)
        return self._access_token

    async def _headers(self) -> dict[str, str]:
        token = await self._ensure_token()
        return {"Authorization": f"bearer {token}", "User-Agent": self._user_agent}

    async def submit_text(
        self, subreddit: str, title: str, text: str
    ) -> dict[str, Any]:
        headers = await self._headers()
        async with httpx.AsyncClient(timeout=30) as client:
            resp = await client.post(
                f"{_REDDIT_API}/api/submit",
                headers=headers,
                data={
                    "sr": subreddit,
                    "kind": "self",
                    "title": title[:300],
                    "text": text,
                    "resubmit": "true",
                },
            )
            resp.raise_for_status()
            data = resp.json()
            url = data.get("json", {}).get("data", {}).get("url", "")
            logger.info("reddit.submit_text: subreddit=%s url=%s", subreddit, url)
            return {"url": url, "raw": data}

    async def submit_link(
        self, subreddit: str, title: str, url: str
    ) -> dict[str, Any]:
        headers = await self._headers()
        async with httpx.AsyncClient(timeout=30) as client:
            resp = await client.post(
                f"{_REDDIT_API}/api/submit",
                headers=headers,
                data={
                    "sr": subreddit,
                    "kind": "link",
                    "title": title[:300],
                    "url": url,
                    "resubmit": "true",
                },
            )
            resp.raise_for_status()
            data = resp.json()
            post_url = data.get("json", {}).get("data", {}).get("url", "")
            return {"url": post_url, "raw": data}

    async def submit_video(
        self, subreddit: str, title: str, video_url: str, thumbnail_url: str = ""
    ) -> dict[str, Any]:
        headers = await self._headers()
        payload: dict[str, Any] = {
            "sr": subreddit,
            "kind": "video",
            "title": title[:300],
            "url": video_url,
            "resubmit": "true",
        }
        if thumbnail_url:
            payload["video_poster_url"] = thumbnail_url
        async with httpx.AsyncClient(timeout=30) as client:
            resp = await client.post(
                f"{_REDDIT_API}/api/submit",
                headers=headers,
                data=payload,
            )
            resp.raise_for_status()
            data = resp.json()
            post_url = data.get("json", {}).get("data", {}).get("url", "")
            logger.info("reddit.submit_video: subreddit=%s url=%s", subreddit, post_url)
            return {"url": post_url, "raw": data}


# ===========================================================================
# X (Twitter) API v2
# ===========================================================================

_X_API = "https://api.twitter.com/2"
_X_MEDIA_UPLOAD = "https://upload.twitter.com/1.1/media/upload.json"
_X_CHUNK_SIZE = 4 * 1024 * 1024  # 4 MB


class XClient:
    """X API v2 — tweets, threads, chunked media upload."""

    def __init__(
        self,
        api_key: str,
        api_secret: str,
        access_token: str,
        access_secret: str,
    ) -> None:
        self._api_key = api_key
        self._api_secret = api_secret
        self._access_token = access_token
        self._access_secret = access_secret

    @classmethod
    def from_env(cls) -> "XClient":
        return cls(
            api_key=os.environ["X_API_KEY"],
            api_secret=os.environ["X_API_SECRET"],
            access_token=os.environ["X_ACCESS_TOKEN"],
            access_secret=os.environ["X_ACCESS_SECRET"],
        )

    def _oauth1_headers(self, method: str, url: str, params: dict = None) -> dict[str, str]:
        """Generate OAuth 1.0a Authorization header."""
        import hashlib
        import hmac
        import urllib.parse
        import uuid
        params = params or {}
        nonce = uuid.uuid4().hex
        ts = str(int(time.time()))
        oauth_params = {
            "oauth_consumer_key": self._api_key,
            "oauth_nonce": nonce,
            "oauth_signature_method": "HMAC-SHA1",
            "oauth_timestamp": ts,
            "oauth_token": self._access_token,
            "oauth_version": "1.0",
        }
        all_params = {**params, **oauth_params}
        base_str = "&".join([
            method.upper(),
            urllib.parse.quote(url, safe=""),
            urllib.parse.quote("&".join(
                f"{urllib.parse.quote(k, safe='')}={urllib.parse.quote(str(v), safe='')}"
                for k, v in sorted(all_params.items())
            ), safe=""),
        ])
        signing_key = f"{urllib.parse.quote(self._api_secret, safe='')}&{urllib.parse.quote(self._access_secret, safe='')}"
        sig = base64.b64encode(
            hmac.new(signing_key.encode(), base_str.encode(), hashlib.sha1).digest()
        ).decode()
        oauth_params["oauth_signature"] = sig
        header = "OAuth " + ", ".join(
            f'{urllib.parse.quote(k, safe="")}="{urllib.parse.quote(str(v), safe="")}"'
            for k, v in sorted(oauth_params.items())
        )
        return {"Authorization": header}

    async def post_tweet(
        self, text: str, media_ids: list[str] = None, reply_to_id: str = ""
    ) -> dict[str, Any]:
        body: dict[str, Any] = {"text": text[:280]}
        if media_ids:
            body["media"] = {"media_ids": media_ids}
        if reply_to_id:
            body["reply"] = {"in_reply_to_tweet_id": reply_to_id}

        headers = self._oauth1_headers("POST", f"{_X_API}/tweets")
        headers["Content-Type"] = "application/json"
        async with httpx.AsyncClient(timeout=30) as client:
            resp = await client.post(f"{_X_API}/tweets", headers=headers, json=body)
            resp.raise_for_status()
            data = resp.json()
            tweet_id = data.get("data", {}).get("id", "")
            logger.info("x.post_tweet: tweet_id=%s", tweet_id)
            return data

    async def post_thread(self, texts: list[str], media_ids: list[str] = None) -> list[dict[str, Any]]:
        """Post a thread. First tweet may include media."""
        results = []
        reply_to = ""
        for i, text in enumerate(texts):
            mids = media_ids if i == 0 else None
            result = await self.post_tweet(text, media_ids=mids, reply_to_id=reply_to)
            tweet_id = result.get("data", {}).get("id", "")
            reply_to = tweet_id
            results.append(result)
            if i < len(texts) - 1:
                await asyncio.sleep(1.0)  # brief pause between thread tweets
        return results

    async def upload_media(self, file_path: str) -> str:
        """Chunked media upload (INIT/APPEND/FINALIZE). Returns media_id_string."""
        path = Path(file_path)
        file_size = path.stat().st_size
        media_type = "video/mp4" if path.suffix.lower() in (".mp4", ".mov") else "image/jpeg"
        media_category = "tweet_video" if "video" in media_type else "tweet_image"

        auth = self._oauth1_headers("POST", _X_MEDIA_UPLOAD)

        async with httpx.AsyncClient(timeout=60) as client:
            # INIT
            resp = await client.post(
                _X_MEDIA_UPLOAD,
                headers=auth,
                data={
                    "command": "INIT",
                    "total_bytes": file_size,
                    "media_type": media_type,
                    "media_category": media_category,
                },
            )
            resp.raise_for_status()
            media_id = resp.json()["media_id_string"]

            # APPEND
            with path.open("rb") as f:
                segment = 0
                while chunk := f.read(_X_CHUNK_SIZE):
                    append_auth = self._oauth1_headers("POST", _X_MEDIA_UPLOAD)
                    r = await client.post(
                        _X_MEDIA_UPLOAD,
                        headers=append_auth,
                        data={"command": "APPEND", "media_id": media_id, "segment_index": segment},
                        files={"media": chunk},
                    )
                    r.raise_for_status()
                    segment += 1

            # FINALIZE
            fin_auth = self._oauth1_headers("POST", _X_MEDIA_UPLOAD)
            resp = await client.post(
                _X_MEDIA_UPLOAD,
                headers=fin_auth,
                data={"command": "FINALIZE", "media_id": media_id},
            )
            resp.raise_for_status()
            fin_data = resp.json()

        # Poll for video processing
        if fin_data.get("processing_info"):
            await self._wait_media_processing(media_id)

        logger.info("x.upload_media: media_id=%s path=%s", media_id, file_path)
        return media_id

    async def _wait_media_processing(
        self, media_id: str, max_wait: int = 120, interval: int = 5
    ) -> None:
        elapsed = 0
        async with httpx.AsyncClient(timeout=20) as client:
            while elapsed < max_wait:
                auth = self._oauth1_headers("GET", _X_MEDIA_UPLOAD)
                resp = await client.get(
                    _X_MEDIA_UPLOAD,
                    headers=auth,
                    params={"command": "STATUS", "media_id": media_id},
                )
                resp.raise_for_status()
                info = resp.json().get("processing_info", {})
                state = info.get("state", "")
                if state == "succeeded":
                    return
                if state == "failed":
                    raise RuntimeError(f"X media processing failed: {info}")
                await asyncio.sleep(interval)
                elapsed += interval
        raise TimeoutError(f"X media {media_id} not processed after {max_wait}s")

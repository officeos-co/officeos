from __future__ import annotations

import tempfile
import unittest
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import AsyncMock, patch

from app.social.clients import InstagramClient, RedditClient, XClient
from app.social.youtube import YouTubeClient


class _FakeResponse:
    def __init__(self, payload: dict | None = None, *, status_code: int = 200, headers: dict | None = None) -> None:
        self._payload = payload or {}
        self.status_code = status_code
        self.headers = headers or {}

    def json(self) -> dict:
        return self._payload

    def raise_for_status(self) -> None:
        if self.status_code >= 400:
            raise RuntimeError(f"http {self.status_code}")


class _FakeAsyncClient:
    def __init__(
        self,
        *,
        posts: list[object] | None = None,
        gets: list[object] | None = None,
        puts: list[object] | None = None,
    ) -> None:
        self._posts = list(posts or [])
        self._gets = list(gets or [])
        self._puts = list(puts or [])
        self.post_calls: list[tuple[tuple, dict]] = []
        self.get_calls: list[tuple[tuple, dict]] = []
        self.put_calls: list[tuple[tuple, dict]] = []

    async def __aenter__(self) -> "_FakeAsyncClient":
        return self

    async def __aexit__(self, exc_type, exc, tb) -> bool:
        return False

    def _next(self, queue: list[object], args: tuple, kwargs: dict) -> object:
        item = queue.pop(0)
        if callable(item):
            return item(*args, **kwargs)
        return item

    async def post(self, *args, **kwargs) -> _FakeResponse:
        self.post_calls.append((args, kwargs))
        return self._next(self._posts, args, kwargs)

    async def get(self, *args, **kwargs) -> _FakeResponse:
        self.get_calls.append((args, kwargs))
        return self._next(self._gets, args, kwargs)

    async def put(self, *args, **kwargs) -> _FakeResponse:
        self.put_calls.append((args, kwargs))
        return self._next(self._puts, args, kwargs)


class InstagramClientTests(unittest.IsolatedAsyncioTestCase):
    async def test_instagram_post_image_and_reel_publish_media(self) -> None:
        client = InstagramClient("token", "ig-user")

        with patch.object(
            client,
            "_post",
            AsyncMock(side_effect=[{"id": "container-1"}, {"id": "media-1"}]),
        ) as post_mock:
            result = await client.post_image("https://cdn.example.com/pic.jpg", "caption")

        self.assertEqual(result["id"], "media-1")
        self.assertEqual(post_mock.await_count, 2)

        with (
            patch.object(
                client,
                "_post",
                AsyncMock(side_effect=[{"id": "reel-container"}, {"id": "reel-media"}]),
            ) as reel_post,
            patch.object(client, "_wait_for_media_ready", AsyncMock()) as wait_mock,
        ):
            reel_result = await client.post_reel("https://cdn.example.com/reel.mp4", "caption", "cover.png")

        self.assertEqual(reel_result["id"], "reel-media")
        wait_mock.assert_awaited_once_with("reel-container")
        self.assertEqual(reel_post.await_count, 2)

    async def test_instagram_wait_for_media_ready_and_carousel(self) -> None:
        client = InstagramClient("token", "ig-user")

        with patch.object(client, "_get", AsyncMock(return_value={"status_code": "FINISHED"})):
            await client._wait_for_media_ready("container-1", max_wait=5, interval=1)

        with patch.object(client, "_get", AsyncMock(return_value={"status_code": "ERROR", "status": "bad"})):
            with self.assertRaisesRegex(RuntimeError, "processing failed"):
                await client._wait_for_media_ready("container-2", max_wait=5, interval=1)

        with (
            patch.object(client, "_get", AsyncMock(return_value={"status_code": "IN_PROGRESS"})),
            patch("app.social.clients.asyncio.sleep", new=AsyncMock()),
        ):
            with self.assertRaisesRegex(TimeoutError, "not ready"):
                await client._wait_for_media_ready("container-3", max_wait=1, interval=1)

        with patch.object(
            client,
            "_post",
            AsyncMock(
                side_effect=[
                    {"id": "child-1"},
                    {"id": "child-2"},
                    {"id": "carousel-container"},
                    {"id": "carousel-media"},
                ]
            ),
        ) as post_mock:
            result = await client.post_carousel(
                ["https://cdn.example.com/a.jpg", "https://cdn.example.com/b.jpg"],
                "caption",
            )

        self.assertEqual(result["id"], "carousel-media")
        self.assertEqual(post_mock.await_count, 4)


class RedditClientTests(unittest.IsolatedAsyncioTestCase):
    async def test_reddit_token_fetch_caches_and_submit_methods_return_urls(self) -> None:
        token_client = _FakeAsyncClient(posts=[_FakeResponse({"access_token": "reddit-token", "expires_in": 3600})])
        client = RedditClient("cid", "secret", "user", "pass")

        with (
            patch("app.social.clients.httpx.AsyncClient", return_value=token_client),
            patch("app.social.clients.time.time", side_effect=[1000.0, 1000.0, 1001.0]),
        ):
            first = await client._ensure_token()
            second = await client._ensure_token()

        self.assertEqual(first, "reddit-token")
        self.assertEqual(second, "reddit-token")
        self.assertEqual(len(token_client.post_calls), 1)

        submit_text_client = _FakeAsyncClient(
            posts=[_FakeResponse({"json": {"data": {"url": "https://reddit.example/text"}}})]
        )
        submit_link_client = _FakeAsyncClient(
            posts=[_FakeResponse({"json": {"data": {"url": "https://reddit.example/link"}}})]
        )
        submit_video_client = _FakeAsyncClient(
            posts=[_FakeResponse({"json": {"data": {"url": "https://reddit.example/video"}}})]
        )

        with (
            patch.object(client, "_headers", AsyncMock(return_value={"Authorization": "bearer reddit-token"})),
            patch(
                "app.social.clients.httpx.AsyncClient",
                side_effect=[submit_text_client, submit_link_client, submit_video_client],
            ),
        ):
            text_result = await client.submit_text("python", "Hello", "Body")
            link_result = await client.submit_link("python", "Hello", "https://example.com")
            video_result = await client.submit_video("python", "Hello", "https://example.com/video.mp4")

        self.assertEqual(text_result["url"], "https://reddit.example/text")
        self.assertEqual(link_result["url"], "https://reddit.example/link")
        self.assertEqual(video_result["url"], "https://reddit.example/video")


class XClientTests(unittest.IsolatedAsyncioTestCase):
    async def test_x_oauth_headers_and_thread_posting(self) -> None:
        client = XClient("api-key", "api-secret", "access-token", "access-secret")

        with (
            patch("app.social.clients.time.time", return_value=1710000000),
            patch("uuid.uuid4", return_value=SimpleNamespace(hex="fixednonce")),
        ):
            headers = client._oauth1_headers("POST", "https://api.twitter.com/2/tweets", {"status": "ok"})

        self.assertIn("OAuth ", headers["Authorization"])
        self.assertIn("oauth_consumer_key=\"api-key\"", headers["Authorization"])
        self.assertIn("oauth_nonce=\"fixednonce\"", headers["Authorization"])

        with (
            patch.object(
                client,
                "post_tweet",
                AsyncMock(side_effect=[{"data": {"id": "1"}}, {"data": {"id": "2"}}]),
            ) as post_mock,
            patch("app.social.clients.asyncio.sleep", new=AsyncMock()) as sleep_mock,
        ):
            results = await client.post_thread(["one", "two"], media_ids=["media-1"])

        self.assertEqual([item["data"]["id"] for item in results], ["1", "2"])
        self.assertEqual(post_mock.await_args_list[1].kwargs["reply_to_id"], "1")
        sleep_mock.assert_awaited_once()

    async def test_x_upload_media_and_wait_for_processing(self) -> None:
        client = XClient("api-key", "api-secret", "access-token", "access-secret")

        with tempfile.TemporaryDirectory() as tempdir:
            media_path = Path(tempdir) / "video.mp4"
            media_path.write_bytes(b"x" * 10)

            upload_client = _FakeAsyncClient(
                posts=[
                    _FakeResponse({"media_id_string": "media-123"}),
                    _FakeResponse({}),
                    _FakeResponse({"processing_info": {"state": "pending"}}),
                ]
            )
            with (
                patch.object(client, "_oauth1_headers", return_value={"Authorization": "OAuth test"}),
                patch.object(client, "_wait_media_processing", AsyncMock()) as wait_mock,
                patch("app.social.clients.httpx.AsyncClient", return_value=upload_client),
            ):
                media_id = await client.upload_media(str(media_path))

            self.assertEqual(media_id, "media-123")
            wait_mock.assert_awaited_once_with("media-123")
            self.assertEqual(len(upload_client.post_calls), 3)

        success_client = _FakeAsyncClient(
            gets=[_FakeResponse({"processing_info": {"state": "succeeded"}})]
        )
        with (
            patch.object(client, "_oauth1_headers", return_value={"Authorization": "OAuth test"}),
            patch("app.social.clients.httpx.AsyncClient", return_value=success_client),
        ):
            await client._wait_media_processing("media-123", max_wait=5, interval=1)

        failure_client = _FakeAsyncClient(
            gets=[_FakeResponse({"processing_info": {"state": "failed"}})]
        )
        with (
            patch.object(client, "_oauth1_headers", return_value={"Authorization": "OAuth test"}),
            patch("app.social.clients.httpx.AsyncClient", return_value=failure_client),
        ):
            with self.assertRaisesRegex(RuntimeError, "processing failed"):
                await client._wait_media_processing("media-123", max_wait=5, interval=1)


class YouTubeClientTests(unittest.IsolatedAsyncioTestCase):
    async def test_youtube_token_management_and_upload_helpers(self) -> None:
        client = YouTubeClient("client-id", "client-secret", "refresh-token")
        token_client = _FakeAsyncClient(posts=[_FakeResponse({"access_token": "yt-token", "expires_in": 3600})])

        with (
            patch("app.social.youtube.httpx.AsyncClient", return_value=token_client),
            patch("app.social.youtube.time.time", side_effect=[1000.0, 1000.0, 1001.0]),
        ):
            token = await client._ensure_token()
            cached = await client._ensure_token()

        self.assertEqual(token, "yt-token")
        self.assertEqual(cached, "yt-token")
        self.assertEqual(len(token_client.post_calls), 1)

        with tempfile.TemporaryDirectory() as tempdir:
            video_path = Path(tempdir) / "clip.mp4"
            video_path.write_bytes(b"video-bytes")

            init_client = _FakeAsyncClient(
                posts=[_FakeResponse({}, headers={"Location": "https://upload.example/session"})]
            )
            with (
                patch.object(client, "_headers", AsyncMock(return_value={"Authorization": "Bearer yt-token"})),
                patch.object(client, "_upload_chunks", AsyncMock(return_value={"id": "video-1"})) as upload_mock,
                patch("app.social.youtube.httpx.AsyncClient", return_value=init_client),
            ):
                result = await client.upload_video(str(video_path), "A title", "desc", tags=["tag1"])

            self.assertEqual(result["id"], "video-1")
            upload_mock.assert_awaited_once()

            with patch.object(client, "upload_video", AsyncMock(return_value={"id": "short-1"})) as short_mock:
                short = await client.create_short(str(video_path), "Short title", "desc", tags=["demo"])

            self.assertEqual(short["id"], "short-1")
            self.assertEqual(short_mock.await_args.kwargs["tags"][0], "#Shorts")
            self.assertTrue(short_mock.await_args.kwargs["description"].startswith("#Shorts"))

            thumb_path = Path(tempdir) / "thumb.jpg"
            thumb_path.write_bytes(b"thumb")
            thumb_client = _FakeAsyncClient(posts=[_FakeResponse({"items": [{"default": {"url": "thumb"}}]})])
            with (
                patch.object(client, "_ensure_token", AsyncMock(return_value="yt-token")),
                patch("app.social.youtube.httpx.AsyncClient", return_value=thumb_client),
            ):
                thumb_result = await client.set_thumbnail("video-1", str(thumb_path))

            self.assertIn("items", thumb_result)

        search_client = _FakeAsyncClient(
            gets=[
                _FakeResponse(
                    {
                        "items": [
                            {
                                "id": {"videoId": "vid-1"},
                                "snippet": {
                                    "title": "A viral clip",
                                    "channelTitle": "Channel",
                                    "publishedAt": "2026-01-01T00:00:00Z",
                                    "description": "x" * 300,
                                },
                            }
                        ]
                    }
                ),
                _FakeResponse(
                    {
                        "items": [
                            {
                                "snippet": {"title": "A viral clip"},
                                "statistics": {"viewCount": "12", "likeCount": "3", "commentCount": "1"},
                            }
                        ]
                    }
                ),
            ]
        )
        with (
            patch.object(client, "_headers", AsyncMock(return_value={"Authorization": "Bearer yt-token"})),
            patch("app.social.youtube.httpx.AsyncClient", return_value=search_client),
        ):
            results = await client.search_videos("viral clips", max_results=1)
            stats = await client.get_video_stats("vid-1")

        self.assertEqual(results[0]["video_id"], "vid-1")
        self.assertEqual(stats["view_count"], 12)

    async def test_youtube_upload_chunks_handles_resume_incomplete(self) -> None:
        with tempfile.TemporaryDirectory() as tempdir:
            path = Path(tempdir) / "clip.mp4"
            path.write_bytes(b"abcdefghij")
            upload_client = _FakeAsyncClient(
                puts=[
                    _FakeResponse({}, status_code=308, headers={"Range": "bytes=0-4"}),
                    _FakeResponse({"id": "video-2"}, status_code=200),
                ]
            )

            client = YouTubeClient("client-id", "client-secret", "refresh-token")
            with patch("app.social.youtube.httpx.AsyncClient", return_value=upload_client):
                result = await client._upload_chunks(
                    "https://upload.example/session",
                    path,
                    path.stat().st_size,
                    "video/mp4",
                )

        self.assertEqual(result["id"], "video-2")
        self.assertEqual(len(upload_client.put_calls), 2)

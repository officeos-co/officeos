from __future__ import annotations

import base64
import tempfile
import unittest
from pathlib import Path
from unittest.mock import AsyncMock, patch

from app.integrations.veo3_and_research import Veo3Client, ViralResearchEngine


class _FakeResponse:
    def __init__(self, payload: dict | None = None, *, status_code: int = 200) -> None:
        self._payload = payload or {}
        self.status_code = status_code

    def json(self) -> dict:
        return self._payload

    def raise_for_status(self) -> None:
        if self.status_code >= 400:
            raise RuntimeError(f"http {self.status_code}")


class _FakeAsyncClient:
    def __init__(self, *, posts: list[object] | None = None, gets: list[object] | None = None) -> None:
        self._posts = list(posts or [])
        self._gets = list(gets or [])
        self.post_calls: list[tuple[tuple, dict]] = []
        self.get_calls: list[tuple[tuple, dict]] = []

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


class _FakeProcess:
    def __init__(self, *, stdout: bytes = b"", stderr: bytes = b"", returncode: int = 0) -> None:
        self._stdout = stdout
        self._stderr = stderr
        self.returncode = returncode

    async def communicate(self) -> tuple[bytes, bytes]:
        return self._stdout, self._stderr


class Veo3ClientTests(unittest.IsolatedAsyncioTestCase):
    async def test_submit_generation_returns_operation_or_sync_payload(self) -> None:
        client = Veo3Client("project-1")
        async_headers = AsyncMock(return_value={"Authorization": "Bearer token"})

        op_http = _FakeAsyncClient(posts=[_FakeResponse({"name": "operations/123"})])
        with (
            patch.object(client, "_headers", async_headers),
            patch("app.integrations.veo3_and_research.httpx.AsyncClient", return_value=op_http),
        ):
            op_name = await client.submit_generation("make a trailer")

        self.assertEqual(op_name, "operations/123")

        sync_http = _FakeAsyncClient(
            posts=[_FakeResponse({"predictions": [{"videoBytesBase64Encoded": "Zm9v"}]})]
        )
        with (
            patch.object(client, "_headers", async_headers),
            patch("app.integrations.veo3_and_research.httpx.AsyncClient", return_value=sync_http),
        ):
            sync_result = await client.submit_generation("make a trailer")

        self.assertTrue(sync_result.startswith("sync:"))
        self.assertIn("videoBytesBase64Encoded", sync_result)

    async def test_save_sync_prediction_requires_predictions(self) -> None:
        client = Veo3Client("project-1")

        with self.assertRaisesRegex(RuntimeError, "no predictions"):
            client._save_sync_prediction({})

    async def test_poll_operation_download_and_generate_cover_result_shapes(self) -> None:
        client = Veo3Client("project-1")

        self.assertEqual(await client.poll_operation('sync:{"ok":true}'), {"ok": True})

        done_http = _FakeAsyncClient(gets=[_FakeResponse({"done": True, "response": {"predictions": [{"id": "p1"}]}})])
        with (
            patch.object(client, "_headers", AsyncMock(return_value={"Authorization": "Bearer token"})),
            patch("app.integrations.veo3_and_research.httpx.AsyncClient", return_value=done_http),
        ):
            done = await client.poll_operation("operations/123", max_wait=5, interval=1)
        self.assertEqual(done["predictions"][0]["id"], "p1")

        error_http = _FakeAsyncClient(gets=[_FakeResponse({"done": True, "error": {"message": "bad"}})])
        with (
            patch.object(client, "_headers", AsyncMock(return_value={"Authorization": "Bearer token"})),
            patch("app.integrations.veo3_and_research.httpx.AsyncClient", return_value=error_http),
        ):
            with self.assertRaisesRegex(RuntimeError, "operation failed"):
                await client.poll_operation("operations/123", max_wait=5, interval=1)

        with tempfile.TemporaryDirectory() as tempdir:
            output_path = Path(tempdir) / "out.mp4"
            inline = await client.download_video(
                {"predictions": [{"videoBytesBase64Encoded": base64.b64encode(b"video").decode()}]},
                str(output_path),
            )
            self.assertEqual(Path(inline).read_bytes(), b"video")

            with patch.object(client, "_download_from_gcs", AsyncMock(return_value=str(output_path))) as gcs_mock:
                gcs_result = await client.download_video({"gcsUri": "gs://bucket/file.mp4"}, str(output_path))
            self.assertEqual(gcs_result, str(output_path))
            gcs_mock.assert_awaited_once()

            with self.assertRaisesRegex(RuntimeError, "contains no video data"):
                await client.download_video({}, str(output_path))

        with (
            patch.object(client, "submit_generation", AsyncMock(return_value="operations/abc")) as submit_mock,
            patch.object(client, "poll_operation", AsyncMock(return_value={"predictions": []})) as poll_mock,
            patch.object(client, "download_video", AsyncMock(return_value="/tmp/out.mp4")) as download_mock,
        ):
            final_path = await client.generate("prompt", "/tmp/out.mp4")

        self.assertEqual(final_path, "/tmp/out.mp4")
        submit_mock.assert_awaited_once()
        poll_mock.assert_awaited_once_with("operations/abc")
        download_mock.assert_awaited_once_with({"predictions": []}, "/tmp/out.mp4")

    async def test_access_token_and_gcs_download_use_cli_helpers(self) -> None:
        client = Veo3Client("project-1")
        token_proc = _FakeProcess(stdout=b"access-token\n")
        with patch(
            "app.integrations.veo3_and_research.asyncio.create_subprocess_exec",
            new=AsyncMock(return_value=token_proc),
        ):
            token = await client._access_token()

        self.assertEqual(token, "access-token")

        with tempfile.TemporaryDirectory() as tempdir:
            output_path = Path(tempdir) / "download.mp4"
            ok_proc = _FakeProcess(returncode=0)
            with patch(
                "app.integrations.veo3_and_research.asyncio.create_subprocess_exec",
                new=AsyncMock(return_value=ok_proc),
            ):
                downloaded = await client._download_from_gcs("gs://bucket/video.mp4", str(output_path))

            self.assertEqual(downloaded, str(output_path))

            fail_proc = _FakeProcess(stderr=b"bad", returncode=1)
            with patch(
                "app.integrations.veo3_and_research.asyncio.create_subprocess_exec",
                new=AsyncMock(return_value=fail_proc),
            ):
                with self.assertRaisesRegex(RuntimeError, "gsutil cp failed"):
                    await client._download_from_gcs("gs://bucket/video.mp4", str(output_path))


class ViralResearchEngineTests(unittest.IsolatedAsyncioTestCase):
    async def test_research_pipeline_scores_topics_and_uses_template_fallback(self) -> None:
        youtube_client = type(
            "YT",
            (),
            {
                "search_videos": AsyncMock(
                    return_value=[
                        {"title": "Amazing Makers Build Robots", "view_count": 1000, "like_count": 150},
                        {"title": "Robots Solve Daily Tasks", "view_count": 500, "like_count": 40},
                    ]
                )
            },
        )()
        reddit_client = type("Reddit", (), {"_headers": AsyncMock(return_value={"Authorization": "bearer tok"})})()
        reddit_http = _FakeAsyncClient(
            gets=[
                _FakeResponse(
                    {
                        "data": {
                            "children": [
                                {"data": {"title": "Robots Are Everywhere", "score": 300, "url": "https://reddit/a"}}
                            ]
                        }
                    }
                )
            ]
        )
        engine = ViralResearchEngine(youtube_client, reddit_client)

        with patch("app.integrations.veo3_and_research.httpx.AsyncClient", return_value=reddit_http):
            result = await engine.research("home robotics", subreddits=["robotics"], yt_results=2, reddit_results=6)

        self.assertEqual(result["niche"], "home robotics")
        self.assertTrue(result["trending_topics"])
        self.assertIn("home robotics", result["veo3_prompt"])
        self.assertGreaterEqual(result["top_videos"][0]["virality_score"], result["top_videos"][1]["virality_score"])

    async def test_research_helpers_handle_failures_and_llm_fallback(self) -> None:
        youtube_client = type("YT", (), {"search_videos": AsyncMock(side_effect=RuntimeError("nope"))})()
        reddit_client = type("Reddit", (), {"_headers": AsyncMock(side_effect=RuntimeError("bad auth"))})()
        engine = ViralResearchEngine(youtube_client, reddit_client, llm_fn=AsyncMock(side_effect=RuntimeError("llm down")))

        youtube_results = await engine._fetch_youtube("gaming", 5)
        reddit_results = await engine._fetch_reddit("gaming", [], 6)
        prompt = await engine._synthesize_prompt(
            "gaming",
            ["clips", "wins"],
            [{"title": "Clutch Wins", "virality_score": 0.9}],
            [{"title": "Amazing Finish", "score": 100}],
        )

        self.assertEqual(youtube_results, [])
        self.assertEqual(reddit_results, [])
        self.assertIn("gaming", prompt)
        self.assertIn("clips", prompt)

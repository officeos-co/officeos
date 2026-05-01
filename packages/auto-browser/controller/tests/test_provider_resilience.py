from __future__ import annotations

import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

import httpx

from app.config import Settings
from app.providers.base import BaseProviderAdapter, ProviderAPIError


class FakeAsyncClient:
    def __init__(self, responses):
        self.responses = list(responses)

    async def __aenter__(self):
        return self

    async def __aexit__(self, exc_type, exc, tb):
        return None

    async def post(self, url, headers=None, json=None):
        response = self.responses.pop(0)
        if isinstance(response, Exception):
            raise response
        return response


class DummyAdapter(BaseProviderAdapter):
    provider = "openai"

    @property
    def default_model(self) -> str:
        return "dummy"

    @property
    def configured(self) -> bool:
        return True

    @property
    def missing_detail(self) -> str:
        return ""

    async def _decide(self, **kwargs):  # pragma: no cover - unused here
        raise NotImplementedError


class ProviderResilienceTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self) -> None:
        self.tempdir = tempfile.TemporaryDirectory()
        root = Path(self.tempdir.name)
        settings = Settings(_env_file=None)
        settings.artifact_root = str(root / "artifacts")
        settings.upload_root = str(root / "uploads")
        settings.auth_root = str(root / "auth")
        settings.approval_root = str(root / "approvals")
        settings.session_store_root = str(root / "sessions")
        settings.model_max_retries = 1
        settings.model_retry_backoff_seconds = 0
        self.adapter = DummyAdapter(settings)

    async def asyncTearDown(self) -> None:
        self.tempdir.cleanup()

    async def test_post_json_retries_retryable_status_codes(self) -> None:
        request = httpx.Request("POST", "https://example.com")
        responses = [
            httpx.Response(429, request=request, json={"error": {"message": "rate limited"}}),
            httpx.Response(200, request=request, json={"ok": True}),
        ]

        with patch("app.providers.base.httpx.AsyncClient", return_value=FakeAsyncClient(responses)):
            payload = await self.adapter._post_json(
                url="https://example.com",
                headers={},
                payload={"demo": True},
            )

        self.assertEqual(payload, {"ok": True})

    async def test_post_json_normalizes_final_provider_error(self) -> None:
        request = httpx.Request("POST", "https://example.com")
        responses = [
            httpx.Response(500, request=request, json={"error": {"message": "upstream exploded"}}),
            httpx.Response(500, request=request, json={"error": {"message": "still broken"}}),
        ]

        with patch("app.providers.base.httpx.AsyncClient", return_value=FakeAsyncClient(responses)):
            with self.assertRaises(ProviderAPIError) as ctx:
                await self.adapter._post_json(
                    url="https://example.com",
                    headers={},
                    payload={"demo": True},
                )

        self.assertEqual(ctx.exception.status_code, 500)
        self.assertIn("still broken", str(ctx.exception))

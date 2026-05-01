from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from app.browser_manager import BrowserManager
from app.config import Settings


class BrowserConnectTests(unittest.IsolatedAsyncioTestCase):
    async def test_ws_endpoint_prefers_shared_file(self) -> None:
        with tempfile.TemporaryDirectory() as tempdir:
            root = Path(tempdir)
            endpoint_file = root / "browser-ws-endpoint.txt"
            endpoint_file.write_text("ws://browser-node:9223/tenant", encoding="utf-8")
            settings = Settings(
                ARTIFACT_ROOT=str(root / "artifacts"),
                UPLOAD_ROOT=str(root / "uploads"),
                AUTH_ROOT=str(root / "auth"),
                APPROVAL_ROOT=str(root / "approvals"),
                AUDIT_ROOT=str(root / "audit"),
                SESSION_STORE_ROOT=str(root / "sessions"),
                REMOTE_ACCESS_INFO_PATH=str(root / "tunnels/reverse-ssh.json"),
                BROWSER_WS_ENDPOINT_FILE=str(endpoint_file),
                BROWSER_WS_ENDPOINT="ws://fallback:9223/ignored",
            )

            manager = BrowserManager(settings)
            endpoint = await manager._resolve_browser_ws_endpoint()

            self.assertEqual(endpoint, "ws://browser-node:9223/tenant")

    async def test_ws_endpoint_falls_back_to_direct_setting(self) -> None:
        with tempfile.TemporaryDirectory() as tempdir:
            root = Path(tempdir)
            settings = Settings(
                ARTIFACT_ROOT=str(root / "artifacts"),
                UPLOAD_ROOT=str(root / "uploads"),
                AUTH_ROOT=str(root / "auth"),
                APPROVAL_ROOT=str(root / "approvals"),
                AUDIT_ROOT=str(root / "audit"),
                SESSION_STORE_ROOT=str(root / "sessions"),
                REMOTE_ACCESS_INFO_PATH=str(root / "tunnels/reverse-ssh.json"),
                BROWSER_WS_ENDPOINT_FILE=str(root / "missing.txt"),
                BROWSER_WS_ENDPOINT="ws://browser-node:9223/direct",
            )

            manager = BrowserManager(settings)
            endpoint = await manager._resolve_browser_ws_endpoint()

            self.assertEqual(endpoint, "ws://browser-node:9223/direct")

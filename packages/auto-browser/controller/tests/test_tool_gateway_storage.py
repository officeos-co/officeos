from __future__ import annotations

import unittest
from types import SimpleNamespace
from unittest.mock import AsyncMock

from pydantic import ValidationError

from app.tool_gateway import McpToolGateway
from app.tool_inputs import GetStorageInput, SetStorageInput


def _build_gateway() -> tuple[McpToolGateway, SimpleNamespace, SimpleNamespace]:
    page = SimpleNamespace(evaluate=AsyncMock(return_value=None))
    session = SimpleNamespace(page=page)
    manager = SimpleNamespace(get_session=AsyncMock(return_value=session))
    gateway = McpToolGateway(
        manager=manager,
        orchestrator=SimpleNamespace(list_providers=lambda: []),
        job_queue=SimpleNamespace(),
        tool_profile="full",
    )
    return gateway, manager, page


class ToolGatewayStorageInputTests(unittest.TestCase):
    def test_get_storage_local(self) -> None:
        payload = GetStorageInput(session_id="s1", storage_type="local")
        self.assertEqual(payload.storage_type, "local")

    def test_get_storage_session(self) -> None:
        payload = GetStorageInput(session_id="s1", storage_type="session")
        self.assertEqual(payload.storage_type, "session")

    def test_get_storage_invalid(self) -> None:
        with self.assertRaises(ValidationError):
            GetStorageInput(session_id="s1", storage_type="constructor")

    def test_set_storage_invalid(self) -> None:
        with self.assertRaises(ValidationError):
            SetStorageInput(session_id="s1", key="k", value="v", storage_type="__proto__")

    def test_set_storage_valid(self) -> None:
        payload = SetStorageInput(session_id="s1", key="k", value="v", storage_type="session")
        self.assertEqual(payload.storage_type, "session")


class ToolGatewayStorageRuntimeGuardTests(unittest.IsolatedAsyncioTestCase):
    async def test_get_storage_runtime_guard_rejects_invalid_storage_type(self) -> None:
        gateway, manager, _ = _build_gateway()
        payload = GetStorageInput.model_construct(session_id="s1", key="k", storage_type="constructor")

        with self.assertRaisesRegex(ValueError, "Invalid storage_type"):
            await gateway._get_local_storage(payload)

        manager.get_session.assert_not_awaited()

    async def test_set_storage_runtime_guard_rejects_invalid_storage_type(self) -> None:
        gateway, manager, _ = _build_gateway()
        payload = SetStorageInput.model_construct(session_id="s1", key="k", value="v", storage_type="__proto__")

        with self.assertRaisesRegex(ValueError, "Invalid storage_type"):
            await gateway._set_local_storage(payload)

        manager.get_session.assert_not_awaited()

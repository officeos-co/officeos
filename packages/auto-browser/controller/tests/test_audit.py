from __future__ import annotations

import tempfile
import unittest
from pathlib import Path

from app.audit import AuditStore, reset_current_operator, set_current_operator


class AuditStoreTests(unittest.IsolatedAsyncioTestCase):
    async def test_append_and_filter_events(self) -> None:
        with tempfile.TemporaryDirectory() as tempdir:
            store = AuditStore(Path(tempdir))
            await store.startup()

            token = set_current_operator("operator-1", name="Alice")
            try:
                await store.append(
                    event_type="session_created",
                    status="ok",
                    action="create_session",
                    session_id="session-1",
                    details={"start_url": "https://example.com"},
                )
            finally:
                reset_current_operator(token)

            token = set_current_operator("operator-2", name="Bob")
            try:
                await store.append(
                    event_type="browser_action",
                    status="ok",
                    action="click",
                    session_id="session-2",
                )
            finally:
                reset_current_operator(token)

            all_events = await store.list(limit=10)
            self.assertEqual(len(all_events), 2)
            self.assertEqual(all_events[0].operator.id, "operator-2")

            session_events = await store.list(limit=10, session_id="session-1")
            self.assertEqual(len(session_events), 1)
            self.assertEqual(session_events[0].details["start_url"], "https://example.com")

            operator_events = await store.list(limit=10, operator_id="operator-1")
            self.assertEqual(len(operator_events), 1)
            self.assertEqual(operator_events[0].operator.name, "Alice")

    async def test_sqlite_store_enforces_retention(self) -> None:
        with tempfile.TemporaryDirectory() as tempdir:
            root = Path(tempdir) / "audit"
            db_path = Path(tempdir) / "db" / "operator.db"
            store = AuditStore(root, db_path=str(db_path), max_events=2)
            await store.startup()

            for index in range(3):
                token = set_current_operator(f"operator-{index}")
                try:
                    await store.append(
                        event_type="browser_action",
                        status="ok",
                        action="click",
                        session_id=f"session-{index}",
                    )
                finally:
                    reset_current_operator(token)

            events = await store.list(limit=10)
            self.assertEqual(len(events), 2)
            self.assertEqual(events[0].session_id, "session-2")
            self.assertEqual(events[-1].session_id, "session-1")
            self.assertTrue(db_path.exists())

    async def test_file_store_trims_on_interval_not_every_append(self) -> None:
        with tempfile.TemporaryDirectory() as tempdir:
            root = Path(tempdir) / "audit"
            store = AuditStore(root, max_events=2, file_trim_interval=2)
            await store.startup()

            for index in range(3):
                token = set_current_operator(f"operator-{index}")
                try:
                    await store.append(
                        event_type="browser_action",
                        status="ok",
                        action="click",
                        session_id=f"session-{index}",
                    )
                finally:
                    reset_current_operator(token)

            raw_lines = (root / "events.jsonl").read_text(encoding="utf-8").splitlines()
            self.assertEqual(len([line for line in raw_lines if line.strip()]), 3)

            token = set_current_operator("operator-3")
            try:
                await store.append(
                    event_type="browser_action",
                    status="ok",
                    action="click",
                    session_id="session-3",
                )
            finally:
                reset_current_operator(token)

            events = await store.list(limit=10)
            self.assertEqual(len(events), 2)
            self.assertEqual(events[0].session_id, "session-3")
            self.assertEqual(events[-1].session_id, "session-2")

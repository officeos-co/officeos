from __future__ import annotations

import asyncio
import tempfile
import unittest
from pathlib import Path

from app.agent_jobs import AgentJobQueue
from app.models import AgentRunRequest, AgentRunResult, AgentStepRequest, AgentStepResult


class FakeOrchestrator:
    async def step(self, **kwargs):
        await asyncio.sleep(0.01)
        return AgentStepResult(
            provider=kwargs["provider_name"],
            model="test-model",
            goal=kwargs["goal"],
            status="done",
            observation={"url": "https://example.com"},
            decision={"action": "done", "reason": "done"},
            execution=None,
            usage=None,
            raw_text=None,
            error=None,
            error_code=None,
        )

    async def run(self, **kwargs):
        await asyncio.sleep(0.01)
        return AgentRunResult(
            provider=kwargs["provider_name"],
            model="test-model",
            goal=kwargs["goal"],
            status="done",
            steps=[],
            final_session={"id": kwargs["session_id"], "status": "active"},
        )


class AgentJobQueueTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self) -> None:
        self.tempdir = tempfile.TemporaryDirectory()
        self.queue = AgentJobQueue(
            orchestrator=FakeOrchestrator(),
            store_root=Path(self.tempdir.name),
            worker_count=1,
        )
        await self.queue.startup()

    async def asyncTearDown(self) -> None:
        await self.queue.shutdown()
        self.tempdir.cleanup()

    async def test_enqueued_step_job_runs_to_completion(self) -> None:
        job = await self.queue.enqueue_step(
            "session-1",
            AgentStepRequest(provider="openai", goal="do one thing"),
        )

        for _ in range(50):
            stored = await self.queue.get_job(job["id"])
            if stored["status"] == "completed":
                break
            await asyncio.sleep(0.02)
        else:
            self.fail("step job did not complete")

        self.assertEqual(stored["result"]["status"], "done")
        self.assertEqual(stored["kind"], "agent_step")

    async def test_running_jobs_become_interrupted_on_restart(self) -> None:
        await self.queue.store.create(
            session_id="session-2",
            kind="agent_run",
            request=AgentRunRequest(provider="openai", goal="run it").model_dump(),
        )
        records = await self.queue.store.list()
        record = records[0]
        record.status = "running"
        await self.queue.store.update(record)

        await self.queue.shutdown()
        self.queue = AgentJobQueue(
            orchestrator=FakeOrchestrator(),
            store_root=Path(self.tempdir.name),
            worker_count=1,
        )
        await self.queue.startup()

        updated = await self.queue.get_job(record.id)
        self.assertEqual(updated["status"], "interrupted")

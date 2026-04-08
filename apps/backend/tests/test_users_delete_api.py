# ruff: noqa: S101, INP001
"""Tests for user self-delete API behavior."""

from __future__ import annotations

from dataclasses import dataclass
from typing import Any
from uuid import uuid4

import pytest

from app.api import users
from app.core.auth import AuthContext
from app.models.users import User


@dataclass
class _FakeSession:
    committed: int = 0

    async def commit(self) -> None:
        self.committed += 1


class _EmptyMembershipQuery:
    async def all(self, _session: Any) -> list[Any]:
        return []


class _FakeOrganizationMemberModel:
    class objects:
        @staticmethod
        def filter_by(**_kwargs: Any) -> _EmptyMembershipQuery:
            return _EmptyMembershipQuery()


@pytest.mark.asyncio
async def test_delete_me_removes_user(monkeypatch: pytest.MonkeyPatch) -> None:
    """User delete should remove local account and commit."""
    session = _FakeSession()
    user = User(id=uuid4(), external_id="google-user-456")
    auth = AuthContext(actor_type="user", user=user)
    calls: dict[str, int] = {"update": 0, "delete": 0}

    async def _update_where(*_args: Any, **_kwargs: Any) -> int:
        calls["update"] += 1
        return 0

    async def _delete_where(*_args: Any, **_kwargs: Any) -> int:
        calls["delete"] += 1
        return 1

    monkeypatch.setattr(users, "OrganizationMember", _FakeOrganizationMemberModel)
    monkeypatch.setattr(users.crud, "update_where", _update_where)
    monkeypatch.setattr(users.crud, "delete_where", _delete_where)

    response = await users.delete_me(session=session, auth=auth)

    assert response.ok is True
    assert calls["update"] == 3  # invites (2) + tasks (1)
    assert calls["delete"] == 1  # user row
    assert session.committed == 1

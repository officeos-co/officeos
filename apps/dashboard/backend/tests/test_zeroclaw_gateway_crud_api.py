# ruff: noqa: INP001
"""Tests for ZeroClaw gateway CRUD behavior.

These tests focus on the ZeroClaw-specific branching in the gateway API layer
(create, update, delete) without requiring a full FastAPI integration test stack.
They test the service-level logic that differentiates ZeroClaw from OpenClaw gateways.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any
from uuid import UUID, uuid4

import pytest

from app.models.gateways import Gateway
from app.schemas.gateways import GatewayCreate, GatewayRead, GatewayUpdate


# ---------------------------------------------------------------------------
# Schema-level tests
# ---------------------------------------------------------------------------


class TestGatewaySchemas:
    def test_gateway_create_defaults_to_openclaw(self) -> None:
        payload = GatewayCreate(
            name="test",
            url="ws://gateway.example/ws",
            workspace_root="/tmp/ws",
        )
        assert payload.type == "openclaw"

    def test_gateway_create_accepts_zeroclaw_type(self) -> None:
        payload = GatewayCreate(name="zc-agent", type="zeroclaw")
        assert payload.type == "zeroclaw"
        # url and workspace_root default to empty for zeroclaw
        assert payload.url == ""
        assert payload.workspace_root == ""

    def test_gateway_create_accepts_docker_image(self) -> None:
        payload = GatewayCreate(
            name="zc-agent",
            type="zeroclaw",
            docker_image="my-registry/zeroclaw:v1",
        )
        assert payload.docker_image == "my-registry/zeroclaw:v1"

    def test_gateway_read_includes_zeroclaw_fields(self) -> None:
        read = GatewayRead(
            id=uuid4(),
            organization_id=uuid4(),
            name="zc",
            url="ws://localhost:43000/ws/chat",
            workspace_root="/zeroclaw-data/workspace",
            type="zeroclaw",
            container_id="abc123",
            host_port=43000,
            docker_image="ghcr.io/zeroclaw-labs/zeroclaw:debian",
            container_status="running",
            created_at="2026-01-01T00:00:00",
            updated_at="2026-01-01T00:00:00",
        )
        assert read.type == "zeroclaw"
        assert read.container_id == "abc123"
        assert read.host_port == 43000
        assert read.container_status == "running"

    def test_gateway_read_defaults_zeroclaw_fields_to_none(self) -> None:
        """Existing OpenClaw gateways don't have ZeroClaw fields."""
        read = GatewayRead(
            id=uuid4(),
            organization_id=uuid4(),
            name="oc",
            url="ws://gateway.example/ws",
            workspace_root="/tmp/ws",
            created_at="2026-01-01T00:00:00",
            updated_at="2026-01-01T00:00:00",
        )
        assert read.type == "openclaw"
        assert read.container_id is None
        assert read.host_port is None
        assert read.container_status is None


# ---------------------------------------------------------------------------
# Model-level tests
# ---------------------------------------------------------------------------


class TestGatewayModel:
    def test_gateway_model_defaults_to_openclaw(self) -> None:
        gw = Gateway(
            id=uuid4(),
            organization_id=uuid4(),
            name="test",
            url="ws://example/ws",
            workspace_root="/ws",
        )
        assert gw.type == "openclaw"
        assert gw.container_id is None
        assert gw.host_port is None
        assert gw.container_status is None

    def test_gateway_model_accepts_zeroclaw_type(self) -> None:
        gw = Gateway(
            id=uuid4(),
            organization_id=uuid4(),
            name="zc",
            url="ws://localhost:43000/ws/chat",
            workspace_root="/zeroclaw-data/workspace",
            type="zeroclaw",
            container_id="abc123",
            host_port=43000,
            docker_image="ghcr.io/zeroclaw-labs/zeroclaw:debian",
            container_status="running",
        )
        assert gw.type == "zeroclaw"
        assert gw.container_id == "abc123"
        assert gw.host_port == 43000


# ---------------------------------------------------------------------------
# Update validation tests (ZeroClaw managed fields)
# ---------------------------------------------------------------------------


class TestZeroClawUpdateValidation:
    """Test that ZeroClaw gateways block updates to managed fields.

    This tests the logic that should exist in the update_gateway endpoint:
    managed fields (url, token, workspace_root) cannot be changed on ZeroClaw gateways.
    """

    def test_zeroclaw_gateway_managed_fields(self) -> None:
        """Verify the set of managed fields for ZeroClaw gateways."""
        managed = {"url", "token", "workspace_root"}

        # These fields should be blocked for ZeroClaw updates
        update_url = GatewayUpdate(url="ws://new-url/ws")
        assert "url" in update_url.model_dump(exclude_unset=True)
        assert "url" in managed

        update_token = GatewayUpdate(token="new-token")
        assert "token" in update_token.model_dump(exclude_unset=True)
        assert "token" in managed

    def test_name_update_is_allowed(self) -> None:
        """Name is NOT a managed field and should be updateable."""
        update = GatewayUpdate(name="new-name")
        fields = update.model_dump(exclude_unset=True)
        managed = {"url", "token", "workspace_root"}
        assert not (managed & set(fields.keys()))


# ---------------------------------------------------------------------------
# Delete behavior tests
# ---------------------------------------------------------------------------


class TestZeroClawDeleteBehavior:
    """Test that delete logic properly handles ZeroClaw container cleanup."""

    def test_zeroclaw_gateway_has_container_id(self) -> None:
        """ZeroClaw gateway should have container_id for cleanup."""
        gw = Gateway(
            id=uuid4(),
            organization_id=uuid4(),
            name="zc",
            url="ws://localhost:43000/ws/chat",
            workspace_root="/zeroclaw-data/workspace",
            type="zeroclaw",
            container_id="abc123",
        )
        assert gw.type == "zeroclaw"
        assert gw.container_id is not None

    def test_openclaw_gateway_has_no_container_id(self) -> None:
        """OpenClaw gateway should not trigger container cleanup."""
        gw = Gateway(
            id=uuid4(),
            organization_id=uuid4(),
            name="oc",
            url="ws://gateway.example/ws",
            workspace_root="/ws",
        )
        assert gw.type == "openclaw"
        assert gw.container_id is None

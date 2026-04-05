"""Docker container lifecycle manager for ZeroClaw agent instances.

Manages the creation, start, stop, restart, removal, and status inspection
of ZeroClaw containers via the Docker SDK.
"""

from __future__ import annotations

import os
import secrets
from dataclasses import dataclass
from typing import Any
from uuid import UUID

DEFAULT_IMAGE = "ghcr.io/zeroclaw-labs/zeroclaw:debian"
DEFAULT_PORT_RANGE_START = 43000
DEFAULT_PORT_RANGE_END = 44000
ZEROCLAW_INTERNAL_PORT = 42617
MANAGED_BY_LABEL = "openclaw-mission-control"
DEFAULT_PROVIDER = "openrouter"
DEFAULT_MEMORY_BACKEND = "sqlite"


class DockerManagerError(Exception):
    """Raised for any Docker-manager-level failure."""


@dataclass
class ContainerResult:
    container_id: str
    host_port: int
    token: str


@dataclass
class ContainerStatus:
    status: str  # "running" | "stopped" | "not_found" | "error"
    health: str | None = None
    uptime: str | None = None


class DockerManager:
    """Manages ZeroClaw Docker containers."""

    def __init__(self, client: Any | None = None) -> None:
        self._client = client

    @property
    def client(self) -> Any:
        if self._client is None:
            raise DockerManagerError(
                "Docker daemon is not available. "
                "Ensure Docker is running and the socket is accessible."
            )
        return self._client

    def create_container(
        self,
        *,
        gateway_id: UUID,
        name: str,
        org_id: UUID,
        image: str | None = None,
        provider: str | None = None,
        model: str | None = None,
        memory: str | None = None,
    ) -> ContainerResult:
        api_key = os.environ.get("ZEROCLAW_LLM_API_KEY")
        if not api_key:
            raise DockerManagerError(
                "ZEROCLAW_LLM_API_KEY environment variable is required."
            )

        docker_image = image or DEFAULT_IMAGE
        token = secrets.token_urlsafe(32)
        host_port = self._allocate_port()

        resolved_provider = provider or DEFAULT_PROVIDER
        resolved_memory = memory or DEFAULT_MEMORY_BACKEND

        environment = {
            "API_KEY": api_key,
            "PROVIDER": resolved_provider,
            "ZEROCLAW_GATEWAY_PORT": str(ZEROCLAW_INTERNAL_PORT),
            "ZEROCLAW_ALLOW_PUBLIC_BIND": "true",
        }

        # Build the entrypoint command: onboard --quick first, then daemon
        onboard_cmd = (
            f"zeroclaw onboard --quick"
            f" --api-key $API_KEY"
            f" --provider {resolved_provider}"
            f" --memory {resolved_memory}"
        )
        if model:
            onboard_cmd += f" --model {model}"

        boot_command = f"{onboard_cmd} && zeroclaw daemon"

        labels = {
            "managed-by": MANAGED_BY_LABEL,
            "gateway-id": str(gateway_id),
            "org-id": str(org_id),
        }

        volume_name = f"zeroclaw-{gateway_id}"

        container = self.client.containers.run(
            image=docker_image,
            command=["sh", "-c", boot_command],
            detach=True,
            name=f"zeroclaw-{str(gateway_id)[:8]}",
            environment=environment,
            ports={f"{ZEROCLAW_INTERNAL_PORT}/tcp": host_port},
            volumes={volume_name: {"bind": "/zeroclaw-data", "mode": "rw"}},
            labels=labels,
            restart_policy={"Name": "unless-stopped"},
            mem_limit="512m",
            cpu_period=100000,
            cpu_quota=200000,
        )

        return ContainerResult(
            container_id=container.id,
            host_port=host_port,
            token=token,
        )

    def stop_container(self, container_id: str) -> bool:
        try:
            container = self.client.containers.get(container_id)
            container.stop()
            return True
        except Exception:
            return False

    def remove_container(
        self, container_id: str, *, remove_volume: bool = False
    ) -> bool:
        try:
            container = self.client.containers.get(container_id)
            container.remove(v=remove_volume)
            return True
        except Exception:
            return False

    def restart_container(self, container_id: str) -> bool:
        try:
            container = self.client.containers.get(container_id)
            container.restart()
            return True
        except Exception:
            return False

    def get_status(self, container_id: str) -> ContainerStatus:
        try:
            container = self.client.containers.get(container_id)
            container.reload()
            status = container.status
            health = None
            if hasattr(container, "attrs") and "Health" in container.attrs.get(
                "State", {}
            ):
                health = container.attrs["State"]["Health"].get("Status")
            return ContainerStatus(status=status, health=health)
        except Exception:
            return ContainerStatus(status="not_found")

    def get_logs(self, container_id: str, *, tail: int = 100) -> str:
        try:
            container = self.client.containers.get(container_id)
            return container.logs(tail=tail).decode("utf-8", errors="replace")
        except Exception:
            return ""

    def _allocate_port(self) -> int:
        start = int(
            os.environ.get("ZEROCLAW_PORT_RANGE_START", DEFAULT_PORT_RANGE_START)
        )
        end = int(os.environ.get("ZEROCLAW_PORT_RANGE_END", DEFAULT_PORT_RANGE_END))

        used_ports: set[int] = set()
        try:
            containers = self.client.containers.list(
                all=True, filters={"label": f"managed-by={MANAGED_BY_LABEL}"}
            )
            for c in containers:
                ports = c.ports or {}
                for bindings in ports.values():
                    if bindings:
                        for binding in bindings:
                            used_ports.add(int(binding["HostPort"]))
        except Exception:
            pass

        for port in range(start, end):
            if port not in used_ports:
                return port

        raise DockerManagerError(
            f"No available ports in range {start}-{end}. "
            f"{len(used_ports)} ports are in use."
        )

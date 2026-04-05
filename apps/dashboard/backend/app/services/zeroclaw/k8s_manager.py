"""Kubernetes manager for ZeroClaw agent instances.

Creates, manages, and monitors ZeroClaw agent Pods, Services, and
PersistentVolumeClaims in a Kubernetes cluster. Designed to run
in-cluster (ServiceAccount auth).
"""

from __future__ import annotations

import os
import secrets
from dataclasses import dataclass
from typing import Any
from uuid import UUID

DEFAULT_IMAGE = "ghcr.io/zeroclaw-labs/zeroclaw:debian"
DEFAULT_NAMESPACE = "default"
DEFAULT_PROVIDER = "openrouter"
DEFAULT_MEMORY_BACKEND = "sqlite"
ZEROCLAW_PORT = 42617
STORAGE_SIZE = "1Gi"


class K8sManagerError(Exception):
    """Raised for any Kubernetes-manager-level failure."""


@dataclass
class ContainerResult:
    container_id: str  # pod name
    host_port: int  # always ZEROCLAW_PORT for K8s
    token: str


@dataclass
class ContainerStatus:
    status: str  # "running" | "pending" | "stopped" | "failed" | "not_found"
    health: str | None = None


class KubernetesManager:
    """Manages ZeroClaw agent Pods on Kubernetes."""

    def __init__(self, api: Any | None = None, namespace: str = DEFAULT_NAMESPACE) -> None:
        self._api = api
        self._namespace = namespace

    @property
    def api(self) -> Any:
        if self._api is None:
            raise K8sManagerError(
                "Kubernetes API is not available. "
                "Ensure the backend is running in-cluster with proper RBAC."
            )
        return self._api

    def _pod_name(self, gateway_id: UUID) -> str:
        return f"zeroclaw-{str(gateway_id)[:8]}"

    def _pvc_name(self, gateway_id: UUID) -> str:
        return f"zeroclaw-data-{str(gateway_id)[:8]}"

    def _service_name(self, gateway_id: UUID) -> str:
        return f"zeroclaw-{str(gateway_id)[:8]}"

    def _service_url(self, gateway_id: UUID) -> str:
        svc = self._service_name(gateway_id)
        return f"ws://{svc}.{self._namespace}.svc.cluster.local:{ZEROCLAW_PORT}/ws/chat"

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
            raise K8sManagerError(
                "ZEROCLAW_LLM_API_KEY environment variable is required."
            )

        docker_image = image or DEFAULT_IMAGE
        token = secrets.token_urlsafe(32)
        resolved_provider = provider or DEFAULT_PROVIDER
        resolved_memory = memory or DEFAULT_MEMORY_BACKEND

        pod_name = self._pod_name(gateway_id)
        pvc_name = self._pvc_name(gateway_id)
        svc_name = self._service_name(gateway_id)

        # Build boot command
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
            "app": "zeroclaw",
            "managed-by": "openclaw-mission-control",
            "gateway-id": str(gateway_id),
            "org-id": str(org_id),
        }

        # 1. Create PVC
        pvc_manifest = {
            "apiVersion": "v1",
            "kind": "PersistentVolumeClaim",
            "metadata": {"name": pvc_name, "namespace": self._namespace, "labels": labels},
            "spec": {
                "accessModes": ["ReadWriteOnce"],
                "resources": {"requests": {"storage": STORAGE_SIZE}},
            },
        }
        self.api.create_namespaced_persistent_volume_claim(
            namespace=self._namespace, body=pvc_manifest
        )

        # 2. Create Pod
        pod_manifest = {
            "apiVersion": "v1",
            "kind": "Pod",
            "metadata": {"name": pod_name, "namespace": self._namespace, "labels": labels},
            "spec": {
                "restartPolicy": "Always",
                "containers": [
                    {
                        "name": "zeroclaw",
                        "image": docker_image,
                        "command": ["sh", "-c", boot_command],
                        "ports": [{"containerPort": ZEROCLAW_PORT}],
                        "env": [
                            {"name": "API_KEY", "value": api_key},
                            {"name": "PROVIDER", "value": resolved_provider},
                            {"name": "ZEROCLAW_GATEWAY_PORT", "value": str(ZEROCLAW_PORT)},
                            {"name": "ZEROCLAW_ALLOW_PUBLIC_BIND", "value": "true"},
                        ],
                        "resources": {
                            "limits": {"memory": "512Mi", "cpu": "2"},
                            "requests": {"memory": "64Mi", "cpu": "100m"},
                        },
                        "volumeMounts": [
                            {"name": "zeroclaw-data", "mountPath": "/zeroclaw-data"},
                        ],
                    }
                ],
                "volumes": [
                    {
                        "name": "zeroclaw-data",
                        "persistentVolumeClaim": {"claimName": pvc_name},
                    }
                ],
            },
        }
        self.api.create_namespaced_pod(namespace=self._namespace, body=pod_manifest)

        # 3. Create Service
        svc_manifest = {
            "apiVersion": "v1",
            "kind": "Service",
            "metadata": {"name": svc_name, "namespace": self._namespace, "labels": labels},
            "spec": {
                "type": "ClusterIP",
                "selector": {"app": "zeroclaw", "gateway-id": str(gateway_id)},
                "ports": [{"port": ZEROCLAW_PORT, "targetPort": ZEROCLAW_PORT}],
            },
        }
        self.api.create_namespaced_service(namespace=self._namespace, body=svc_manifest)

        return ContainerResult(
            container_id=pod_name,
            host_port=ZEROCLAW_PORT,
            token=token,
        )

    def stop_container(self, container_id: str) -> bool:
        """Delete the Pod (PVC persists for data retention)."""
        try:
            self.api.delete_namespaced_pod(
                name=container_id, namespace=self._namespace
            )
            return True
        except Exception:
            return False

    def remove_container(
        self, container_id: str, *, remove_volume: bool = False
    ) -> bool:
        """Delete Pod, Service, and optionally PVC."""
        try:
            # Delete pod
            try:
                self.api.delete_namespaced_pod(
                    name=container_id, namespace=self._namespace
                )
            except Exception:
                pass

            # Delete service (same name as pod)
            try:
                self.api.delete_namespaced_service(
                    name=container_id, namespace=self._namespace
                )
            except Exception:
                pass

            # Delete PVC if requested
            if remove_volume:
                pvc_name = container_id.replace("zeroclaw-", "zeroclaw-data-", 1)
                try:
                    self.api.delete_namespaced_persistent_volume_claim(
                        name=pvc_name, namespace=self._namespace
                    )
                except Exception:
                    pass

            return True
        except Exception:
            return False

    def restart_container(self, container_id: str) -> bool:
        """Restart by deleting the Pod — K8s restartPolicy: Always recreates it."""
        try:
            self.api.delete_namespaced_pod(
                name=container_id, namespace=self._namespace
            )
            return True
        except Exception:
            return False

    def get_status(self, container_id: str) -> ContainerStatus:
        try:
            pod = self.api.read_namespaced_pod(
                name=container_id, namespace=self._namespace
            )
            phase = pod.status.phase if hasattr(pod, "status") and pod.status else "Unknown"
            status_map = {
                "Running": "running",
                "Pending": "pending",
                "Succeeded": "stopped",
                "Failed": "failed",
                "Unknown": "error",
            }
            return ContainerStatus(status=status_map.get(phase, "error"))
        except Exception:
            return ContainerStatus(status="not_found")

    def get_logs(self, container_id: str, *, tail: int = 100) -> str:
        try:
            return self.api.read_namespaced_pod_log(
                name=container_id,
                namespace=self._namespace,
                tail_lines=tail,
            )
        except Exception:
            return ""

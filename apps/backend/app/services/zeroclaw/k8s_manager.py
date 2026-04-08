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

from app.services.zeroclaw.vault_configmap import (
    agent_vault_configmap_name,
    delete_agent_vault_configmap,
)

DEFAULT_IMAGE = "ghcr.io/zeroclaw-labs/zeroclaw:debian"
DEFAULT_NAMESPACE = "default"
DEFAULT_PROVIDER = "openrouter"
DEFAULT_MEMORY_BACKEND = "sqlite"
ZEROCLAW_PORT = 42617
STORAGE_SIZE = "1Gi"

# Phase 3: per-agent vault ConfigMap mount point inside the pod. The
# zeroclaw daemon reads SOUL.md / IDENTITY.md / AGENTS.md (and the
# optional files) from this directory and fails loudly on missing
# files via load_personality_strict.
VAULT_WORKSPACE_MOUNT_PATH = "/vault-workspace"


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

    def _build_vault_setup_cmd(self) -> str:
        """No-op in Phase 3.

        The old boot sequence was:

            pip install --quiet obsidian-vault-cli
            vault config set vault.host $VAULT_HOST
            ... (five more config lines) ...

        Phase 3 replaces this with a ConfigMap mount: the pod's
        workspace directory is populated by the K8s API before the
        container starts, so there is nothing for the boot script to
        install or configure. This function is kept as a deliberate
        empty stub so a future audit can still find the integration
        point.
        """
        return ""

    def _build_env_vars(
        self,
        *,
        api_key: str,
        provider: str,
        vault_configmap: str | None = None,
    ) -> list[dict[str, str]]:
        """Build the environment variable list for the pod container.

        Phase 3: the old VAULT_HOST/VAULT_DATABASE/VAULT_USERNAME/etc.
        variables are no longer injected. The agent pod never talks to
        CouchDB directly — it reads personality files from the
        ConfigMap mounted at /vault-workspace. The only vault-related
        env var is ZEROCLAW_WORKSPACE, which points the zeroclaw
        daemon at that mount path.
        """
        env = [
            {"name": "API_KEY", "value": api_key},
            {"name": "PROVIDER", "value": provider},
            {"name": "ZEROCLAW_GATEWAY_PORT", "value": str(ZEROCLAW_PORT)},
            {"name": "ZEROCLAW_ALLOW_PUBLIC_BIND", "value": "true"},
        ]

        if vault_configmap:
            env.append(
                {"name": "ZEROCLAW_WORKSPACE", "value": VAULT_WORKSPACE_MOUNT_PATH},
            )

        return env

    def create_container(
        self,
        *,
        gateway_id: UUID,
        name: str,
        org_id: UUID,
        llm_api_key: str,
        image: str | None = None,
        provider: str | None = None,
        model: str | None = None,
        agent_id: UUID | None = None,
    ) -> ContainerResult:
        """Create a Pod + Service (+ optional PVC + ConfigMap) for a ZeroClaw agent.

        Phase 3 notes:

        - If `agent_id` is provided, the pod mounts a per-agent
          ConfigMap `eaos-agent-{agent_id}-vault` at /vault-workspace
          and sets `ZEROCLAW_WORKSPACE=/vault-workspace` so the
          zeroclaw daemon reads its personality files from there. The
          ConfigMap itself is created separately by the lifecycle
          service via app.services.zeroclaw.vault_configmap.
        - If `agent_id` is None (legacy path / tests), the vault mount
          is skipped and the pod behaves as it did before Phase 3.
        - The old `vault_database` / `vault_user_database` params are
          gone; the agent pod no longer shells out to obsctl.
        - The old `zeroclaw onboard --quick` boot step is also gone
          (it relied on the deleted ensure_bootstrap_files). The boot
          command is now simply `zeroclaw daemon`; the strict loader
          in agent boot will fail the pod if the ConfigMap-mounted
          files are missing.
        """
        # llm_api_key is allowed to be empty for providers that don't
        # use one (e.g. Ollama). The API route validates that required
        # providers have a key before getting here.
        api_key = (llm_api_key or "").strip()
        docker_image = image or DEFAULT_IMAGE
        token = secrets.token_urlsafe(32)
        resolved_provider = provider or DEFAULT_PROVIDER
        # Memory backend is hardcoded to the graph-memory implementation;
        # no knob is exposed to the caller.

        pod_name = self._pod_name(gateway_id)
        pvc_name = self._pvc_name(gateway_id)
        svc_name = self._service_name(gateway_id)

        # Phase 3: no pre-boot CLI installation, no onboarding — the
        # ConfigMap mount guarantees the workspace exists before the
        # container starts.
        boot_command = "zeroclaw daemon"
        _ = model  # reserved: may be threaded into env in a future commit

        labels = {
            "app": "zeroclaw",
            "managed-by": "openclaw-mission-control",
            "gateway-id": str(gateway_id),
            "org-id": str(org_id),
        }
        if agent_id is not None:
            labels["agent-id"] = str(agent_id)

        vault_configmap = (
            agent_vault_configmap_name(agent_id) if agent_id is not None else None
        )

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
        volume_mounts: list[dict[str, Any]] = [
            {"name": "zeroclaw-data", "mountPath": "/zeroclaw-data"},
        ]
        volumes: list[dict[str, Any]] = [
            {
                "name": "zeroclaw-data",
                "persistentVolumeClaim": {"claimName": pvc_name},
            },
        ]
        if vault_configmap is not None:
            volume_mounts.append(
                {
                    "name": "vault-workspace",
                    "mountPath": VAULT_WORKSPACE_MOUNT_PATH,
                    "readOnly": True,
                }
            )
            volumes.append(
                {
                    "name": "vault-workspace",
                    "configMap": {"name": vault_configmap},
                }
            )

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
                        "env": self._build_env_vars(
                            api_key=api_key,
                            provider=resolved_provider,
                            vault_configmap=vault_configmap,
                        ),
                        "resources": {
                            "limits": {"memory": "512Mi", "cpu": "2"},
                            "requests": {"memory": "64Mi", "cpu": "100m"},
                        },
                        "livenessProbe": {
                            "httpGet": {"path": "/health", "port": ZEROCLAW_PORT},
                            "initialDelaySeconds": 30,
                            "periodSeconds": 60,
                            "timeoutSeconds": 10,
                            "failureThreshold": 3,
                        },
                        "readinessProbe": {
                            "httpGet": {"path": "/api/health", "port": ZEROCLAW_PORT},
                            "initialDelaySeconds": 15,
                            "periodSeconds": 10,
                            "timeoutSeconds": 5,
                            "failureThreshold": 3,
                        },
                        "volumeMounts": volume_mounts,
                    }
                ],
                "volumes": volumes,
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
        self,
        container_id: str,
        *,
        remove_volume: bool = False,
        agent_id: UUID | None = None,
    ) -> bool:
        """Delete Pod, Service, and optionally PVC + vault ConfigMap."""
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

            # Phase 3: also clean up the agent's vault ConfigMap if the
            # caller provided an agent_id. Legacy callers (before the
            # Phase 3 cut-over) pass None and skip this step.
            if agent_id is not None:
                delete_agent_vault_configmap(
                    api=self.api,
                    namespace=self._namespace,
                    agent_id=agent_id,
                )

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

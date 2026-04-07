"""Per-agent vault ConfigMap management.

Phase 3 of the zeroclaw-core strip-down: the rendered personality
markdown files are mirrored from the per-agent Obsidian vault into a
Kubernetes ConfigMap. That ConfigMap is mounted at the agent pod's
workspace directory (`/vault-workspace`) before the container starts,
so the K8s API itself guarantees the files are present when `zeroclaw
daemon` opens them.

This module is the control-plane side of that mirror:

- `agent_vault_configmap_name(agent_id)` — deterministic name
- `apply_agent_vault_configmap(...)` — create or replace the ConfigMap
  with the rendered file set, server-side merge

The zeroclaw-core runtime is dumb: it just reads files from
`workspace_dir` and fails loudly (via load_personality_strict) if any
required file is missing. The dashboard backend is responsible for
making sure the ConfigMap exists and contains the right files before
the pod is (re-)created.
"""

from __future__ import annotations

import logging
from dataclasses import dataclass
from typing import Any
from uuid import UUID

logger = logging.getLogger(__name__)


@dataclass(frozen=True, slots=True)
class VaultConfigMapResult:
    """Outcome of an apply_agent_vault_configmap call.

    Success case: `name` is set, `keys` lists the ConfigMap data keys
    that were written, `action` is one of {"created", "replaced"}.

    Failure case: `error` is populated; `action` is None.
    """

    name: str
    keys: tuple[str, ...]
    action: str | None = None  # "created" | "replaced" | None
    error: str | None = None


def agent_vault_configmap_name(agent_id: UUID) -> str:
    """Deterministic name for an agent's vault ConfigMap.

    Convention: `eaos-agent-{uuid}-vault`. The prefix distinguishes
    Phase 3 ConfigMaps from unrelated ones in the same namespace.
    """
    return f"eaos-agent-{agent_id}-vault"


def apply_agent_vault_configmap(
    *,
    api: Any,
    namespace: str,
    agent_id: UUID,
    rendered_files: dict[str, str],
    labels: dict[str, str] | None = None,
) -> VaultConfigMapResult:
    """Create or replace the per-agent vault ConfigMap.

    Behaviour:

    - The ConfigMap name is `eaos-agent-{uuid}-vault`.
    - Each non-empty file from `rendered_files` becomes an entry in
      the ConfigMap's `data` map, keyed by its filename.
    - Empty strings are dropped (K8s rejects empty ConfigMap values in
      some versions; stripping avoids the inconsistency).
    - If the ConfigMap already exists, we replace it in-place. This
      is the refresh path — whenever the dashboard re-renders, this
      function is called again to push the new content.
    - On any K8s API error, the function returns a
      VaultConfigMapResult with `error` populated and `action=None`.
      The caller decides whether that's fatal for the broader flow.

    `api` is the same `kubernetes.client.CoreV1Api` object the
    KubernetesManager uses; injected here for testability.
    """
    name = agent_vault_configmap_name(agent_id)

    data = {k: v for k, v in rendered_files.items() if v}
    if not data:
        return VaultConfigMapResult(
            name=name,
            keys=(),
            action=None,
            error="no rendered files to write into ConfigMap",
        )

    effective_labels = {
        "app": "zeroclaw",
        "managed-by": "openclaw-mission-control",
        "agent-id": str(agent_id),
        "eaos-role": "agent-vault",
        **(labels or {}),
    }

    body = {
        "apiVersion": "v1",
        "kind": "ConfigMap",
        "metadata": {
            "name": name,
            "namespace": namespace,
            "labels": effective_labels,
        },
        "data": data,
    }

    # Check whether the ConfigMap already exists. We use a plain try/
    # read rather than relying on a 409 from create, because the kube
    # client surfaces that as an ApiException with status 409 and we
    # want the branch to be explicit.
    exists = False
    try:
        api.read_namespaced_config_map(name=name, namespace=namespace)
        exists = True
    except Exception as exc:
        # The kubernetes client raises ApiException on 404. We can't
        # import its exception type here without pulling it in as a
        # hard dependency; we inspect the status attribute defensively.
        status = getattr(exc, "status", None)
        if status != 404:
            logger.warning(
                "vault_configmap: read failed (non-404) name=%s: %s", name, exc
            )
            return VaultConfigMapResult(
                name=name,
                keys=tuple(sorted(data)),
                error=f"read failed: {exc}",
            )

    try:
        if exists:
            api.replace_namespaced_config_map(
                name=name,
                namespace=namespace,
                body=body,
            )
            action = "replaced"
        else:
            api.create_namespaced_config_map(
                namespace=namespace,
                body=body,
            )
            action = "created"
    except Exception as exc:
        logger.warning(
            "vault_configmap: write failed name=%s action=%s: %s",
            name,
            "replace" if exists else "create",
            exc,
        )
        return VaultConfigMapResult(
            name=name,
            keys=tuple(sorted(data)),
            error=f"{'replace' if exists else 'create'} failed: {exc}",
        )

    return VaultConfigMapResult(
        name=name,
        keys=tuple(sorted(data)),
        action=action,
    )


def delete_agent_vault_configmap(
    *,
    api: Any,
    namespace: str,
    agent_id: UUID,
) -> bool:
    """Delete the per-agent vault ConfigMap. Returns True on success.

    Used during agent teardown. Swallows 404 as success (idempotent).
    """
    name = agent_vault_configmap_name(agent_id)
    try:
        api.delete_namespaced_config_map(name=name, namespace=namespace)
        return True
    except Exception as exc:
        status = getattr(exc, "status", None)
        if status == 404:
            return True
        logger.warning(
            "vault_configmap: delete failed name=%s: %s", name, exc
        )
        return False

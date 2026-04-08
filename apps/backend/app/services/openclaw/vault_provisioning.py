"""Per-agent Obsidian vault provisioning.

Phase 3 of the zeroclaw-core strip-down: each agent gets its own
CouchDB-backed Obsidian vault. The dashboard backend renders the
Jinja2 personality templates (BOARD_SOUL.md.j2, BOARD_IDENTITY.md.j2,
BOARD_AGENTS.md.j2, ...) and writes them into this vault via obsctl's
Python API. A later step also mirrors the rendered files into a
Kubernetes ConfigMap that is mounted at the agent pod's workspace
directory, so the vault is the source of truth and the ConfigMap is
a refresh-on-pod-restart cache.

This module is intentionally thin:

- No HTTP layer
- No logging beyond the minimum
- No database session handling
- No side effects other than the CouchDB writes

The caller (`AgentLifecycleService`) is responsible for threading the
DB session, the Agent row, and the "vault is enabled" gate.
"""

from __future__ import annotations

import logging
import os
from dataclasses import dataclass
from typing import TYPE_CHECKING
from uuid import UUID

from app.services.openclaw.constants import DEFAULT_GATEWAY_FILES

if TYPE_CHECKING:
    from vault_cli.core.client import VaultClient

logger = logging.getLogger(__name__)


@dataclass(frozen=True, slots=True)
class VaultProvisioningResult:
    """Outcome of a single vault-provisioning pass.

    Success case: `database` is set, `files_written` lists the files
    that were PUT into the vault, `created` indicates whether the
    database itself was newly created (vs. already existing).

    Failure case: `error` is populated; all other fields may be
    partially filled. The caller decides whether to abort the broader
    agent creation or continue without the vault.
    """

    database: str
    created: bool
    files_written: tuple[str, ...]
    error: str | None = None


def vault_provisioning_enabled() -> bool:
    """Return True iff the dashboard is configured with vault credentials.

    Phase 3 rolls out progressively: existing test environments and
    dev instances without a CouchDB should continue to work. Vault
    provisioning only engages when all required env vars are present.
    """
    return bool(os.environ.get("ZEROCLAW_VAULT_HOST"))


def _vault_client() -> "VaultClient":
    """Build a VaultClient from the dashboard's environment.

    The same environment variables that k8s_manager.py reads to seed
    the agent pod's vault CLI configuration. Keeping them consistent
    is important: the dashboard and the agent pods are talking to the
    same CouchDB instance with the same credentials.
    """
    from vault_cli.core.client import VaultClient

    return VaultClient(
        host=os.environ.get("ZEROCLAW_VAULT_HOST", "localhost"),
        port=int(os.environ.get("ZEROCLAW_VAULT_PORT", "5984")),
        database="_users",  # unused for ensure_database/write_note targets
        username=os.environ.get("ZEROCLAW_VAULT_USERNAME", ""),
        password=os.environ.get("ZEROCLAW_VAULT_PASSWORD", ""),
        protocol=os.environ.get("ZEROCLAW_VAULT_PROTOCOL", "http"),
    )


def _agent_vault_database(agent_id: UUID) -> str:
    """Deterministic name for an agent's dedicated CouchDB database.

    Phase 3 convention: `agent-{uuid}`. Lowercase, hyphenated, fits
    CouchDB's database naming rules (must start with a lowercase
    letter; may contain a-z 0-9 _ $ ( ) + - /).
    """
    return f"agent-{agent_id}"


def provision_agent_vault(
    *,
    agent_id: UUID,
    rendered_files: dict[str, str],
) -> VaultProvisioningResult:
    """Create (or reuse) the agent's vault database and seed it.

    `rendered_files` is the same dict that the gateway-side provisioner
    produces — see `_render_agent_files()` in
    `app/services/openclaw/provisioning.py`. We DO NOT re-render here;
    the caller MUST pass pre-rendered content so the vault and the
    gateway control plane stay in lockstep.

    Only the personality subset (DEFAULT_GATEWAY_FILES, intersected
    with whatever the caller rendered) is written. Files the caller
    produced but which are not in the personality set are ignored,
    because they belong to the gateway protocol, not the agent's
    persistent identity.

    Returns a VaultProvisioningResult. On error (CouchDB down,
    auth failure, etc.) the error field is set and the caller decides
    whether to surface it as an HTTP error or log-and-continue.
    """
    database = _agent_vault_database(agent_id)

    # Filter to just the personality files. The dict may include other
    # keys (e.g. TOOLS.md or BOOTSTRAP.md, depending on lead vs worker);
    # we write everything the caller produced that is known to be a
    # personality file.
    personality_files = {
        name: content
        for name, content in rendered_files.items()
        if name in DEFAULT_GATEWAY_FILES and content
    }

    if not personality_files:
        return VaultProvisioningResult(
            database=database,
            created=False,
            files_written=(),
            error="no personality files to seed",
        )

    try:
        client = _vault_client()
    except Exception as exc:
        logger.warning("vault_provisioning: failed to build client: %s", exc)
        return VaultProvisioningResult(
            database=database,
            created=False,
            files_written=(),
            error=f"vault client unavailable: {exc}",
        )

    try:
        ensure_result = client.ensure_database(database)
    except Exception as exc:
        logger.warning(
            "vault_provisioning: ensure_database(%s) failed: %s", database, exc
        )
        return VaultProvisioningResult(
            database=database,
            created=False,
            files_written=(),
            error=f"ensure_database failed: {exc}",
        )

    created = bool(ensure_result.get("created", False))

    # Point the client at the per-agent database before writing notes.
    client.database = database
    client.base_url = f"{client.protocol}://{client.host}:{client.port}/{database}"

    written: list[str] = []
    for name in sorted(personality_files):
        content = personality_files[name]
        try:
            client.write_note(name, content)
        except Exception as exc:
            logger.warning(
                "vault_provisioning: write_note(%s) failed: %s", name, exc
            )
            return VaultProvisioningResult(
                database=database,
                created=created,
                files_written=tuple(written),
                error=f"write_note({name}) failed: {exc}",
            )
        written.append(name)

    return VaultProvisioningResult(
        database=database,
        created=created,
        files_written=tuple(written),
        error=None,
    )

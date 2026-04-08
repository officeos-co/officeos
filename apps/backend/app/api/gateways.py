"""Thin API wrappers for gateway CRUD and template synchronization."""

from __future__ import annotations

import logging
from typing import TYPE_CHECKING
from uuid import UUID, uuid4

from fastapi import APIRouter, Depends, HTTPException, Query
from sqlmodel import col

from app.api.deps import require_org_admin
from app.core.auth import AuthContext, get_auth_context
from app.core.time import utcnow
from app.db import crud
from app.db.pagination import paginate
from app.db.session import get_session
from app.models.agents import Agent
from app.models.gateways import Gateway
from app.schemas.common import OkResponse
from app.schemas.gateways import (
    GatewayCreate,
    GatewayRead,
    GatewayTemplatesSyncResult,
    GatewayUpdate,
)
from app.schemas.pagination import DefaultLimitOffsetPage
from app.services.openclaw.admin_service import GatewayAdminLifecycleService
from app.services.openclaw.session_service import GatewayTemplateSyncQuery
from app.services.zeroclaw.k8s_manager import K8sManagerError, KubernetesManager

if TYPE_CHECKING:
    from fastapi_pagination.limit_offset import LimitOffsetPage
    from sqlmodel.ext.asyncio.session import AsyncSession

    from app.services.organizations import OrganizationContext

logger = logging.getLogger(__name__)


router = APIRouter(prefix="/gateways", tags=["gateways"])
SESSION_DEP = Depends(get_session)
AUTH_DEP = Depends(get_auth_context)
ORG_ADMIN_DEP = Depends(require_org_admin)
INCLUDE_MAIN_QUERY = Query(default=True)
RESET_SESSIONS_QUERY = Query(default=False)
ROTATE_TOKENS_QUERY = Query(default=False)
FORCE_BOOTSTRAP_QUERY = Query(default=False)
OVERWRITE_QUERY = Query(default=False)
LEAD_ONLY_QUERY = Query(default=False)
BOARD_ID_QUERY = Query(default=None)
_RUNTIME_TYPE_REFERENCES = (UUID,)


def _template_sync_query(
    *,
    include_main: bool = INCLUDE_MAIN_QUERY,
    lead_only: bool = LEAD_ONLY_QUERY,
    reset_sessions: bool = RESET_SESSIONS_QUERY,
    rotate_tokens: bool = ROTATE_TOKENS_QUERY,
    force_bootstrap: bool = FORCE_BOOTSTRAP_QUERY,
    overwrite: bool = OVERWRITE_QUERY,
    board_id: UUID | None = BOARD_ID_QUERY,
) -> GatewayTemplateSyncQuery:
    return GatewayTemplateSyncQuery(
        include_main=include_main,
        lead_only=lead_only,
        reset_sessions=reset_sessions,
        rotate_tokens=rotate_tokens,
        force_bootstrap=force_bootstrap,
        overwrite=overwrite,
        board_id=board_id,
    )


SYNC_QUERY_DEP = Depends(_template_sync_query)


@router.get("", response_model=DefaultLimitOffsetPage[GatewayRead])
async def list_gateways(
    session: AsyncSession = SESSION_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> LimitOffsetPage[GatewayRead]:
    """List gateways for the caller's organization."""
    statement = (
        Gateway.objects.filter_by(organization_id=ctx.organization.id)
        .order_by(col(Gateway.created_at).desc())
        .statement
    )

    return await paginate(session, statement)


@router.post("", response_model=GatewayRead)
async def create_gateway(
    payload: GatewayCreate,
    session: AsyncSession = SESSION_DEP,
    auth: AuthContext = AUTH_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> Gateway:
    """Create a gateway and provision or refresh its main agent."""
    gateway_id = uuid4()
    data = payload.model_dump()
    data["id"] = gateway_id
    data["organization_id"] = ctx.organization.id

    # OpenClaw (external gateway) support was removed — this endpoint
    # only creates ZeroClaw agents. Reject anything else explicitly so
    # stale clients see a clear error instead of a silent no-op.
    if payload.type != "zeroclaw":
        raise HTTPException(
            status_code=400,
            detail=(
                f"Unsupported gateway type: {payload.type!r}. "
                "Only 'zeroclaw' is supported."
            ),
        )

    # Resolve the API key from the provider_credentials table by
    # provider name. The user configures keys once in Settings →
    # Providers; the form no longer accepts them inline.
    from app.api.providers import PROVIDERS as _PROVIDERS_CATALOG
    from app.api.providers import resolve_provider_api_key

    resolved_provider = (payload.provider or "openrouter").strip() or "openrouter"
    provider_def = next(
        (p for p in _PROVIDERS_CATALOG if p.name == resolved_provider), None
    )
    if provider_def is None:
        raise HTTPException(
            status_code=400,
            detail=(
                f"Unknown provider {resolved_provider!r}. Pick one of: "
                + ", ".join(p.name for p in _PROVIDERS_CATALOG)
            ),
        )

    stored_key = await resolve_provider_api_key(
        provider=resolved_provider,
        organization_id=ctx.organization.id,
        session=session,
    )
    if provider_def.requires_key and not (stored_key or "").strip():
        raise HTTPException(
            status_code=400,
            detail=(
                f"No API key configured for {provider_def.title}. "
                f"Add one in Settings → Providers, then try again."
            ),
        )
    effective_key = (stored_key or "").strip()

    try:
        k8s = _get_k8s_manager()
        result = k8s.create_container(
            gateway_id=gateway_id,
            name=payload.name,
            org_id=ctx.organization.id,
            llm_api_key=effective_key,
            provider=payload.provider,
            model=payload.model,
        )
        data["url"] = k8s._service_url(gateway_id)
        data["token"] = result.token
        data["workspace_root"] = "/zeroclaw-data/workspace"
        data["container_id"] = result.container_id
        data["host_port"] = result.host_port
        data["container_status"] = "running"
    except K8sManagerError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc

    # Remove fields that exist on the schema but not on the Gateway model
    for key in ("provider", "model", "docker_image", "llm_api_key"):
        data.pop(key, None)

    gateway = await crud.create(session, Gateway, **data)
    return gateway


def _get_k8s_manager() -> KubernetesManager:
    """Get a KubernetesManager.

    Resolution order:
      1. `load_incluster_config()` when running as a k8s pod
      2. `load_kube_config()` when running in docker compose / locally
         with `~/.kube/config` available (mounted into the backend
         container in docker-compose.yml)
    """
    try:
        from kubernetes import client, config
    except Exception as exc:
        raise K8sManagerError(
            "The `kubernetes` python package is not installed.",
        ) from exc
    try:
        config.load_incluster_config()
        return KubernetesManager(api=client.CoreV1Api())
    except Exception:
        pass
    try:
        config.load_kube_config()
        return KubernetesManager(api=client.CoreV1Api())
    except Exception as exc:
        raise K8sManagerError(
            "Kubernetes API is not reachable. Either run the backend "
            "in-cluster, or mount a valid ~/.kube/config into the "
            "container (see docker-compose.yml)."
        ) from exc


@router.get("/{gateway_id}", response_model=GatewayRead)
async def get_gateway(
    gateway_id: UUID,
    session: AsyncSession = SESSION_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> Gateway:
    """Return one gateway by id for the caller's organization."""
    service = GatewayAdminLifecycleService(session)
    gateway = await service.require_gateway(
        gateway_id=gateway_id,
        organization_id=ctx.organization.id,
    )
    return gateway


@router.patch("/{gateway_id}", response_model=GatewayRead)
async def update_gateway(
    gateway_id: UUID,
    payload: GatewayUpdate,
    session: AsyncSession = SESSION_DEP,
    auth: AuthContext = AUTH_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> Gateway:
    """Patch a gateway and refresh the main-agent provisioning state."""
    service = GatewayAdminLifecycleService(session)
    gateway = await service.require_gateway(
        gateway_id=gateway_id,
        organization_id=ctx.organization.id,
    )
    updates = payload.model_dump(exclude_unset=True)

    # Block managed-field changes for ZeroClaw gateways
    if gateway.type == "zeroclaw":
        managed_fields = {"url", "token", "workspace_root"}
        blocked = managed_fields & set(updates.keys())
        if blocked:
            raise HTTPException(
                status_code=422,
                detail=f"Cannot modify managed fields on ZeroClaw gateway: {', '.join(sorted(blocked))}",
            )

    if (
        "url" in updates
        or "token" in updates
        or "allow_insecure_tls" in updates
        or "disable_device_pairing" in updates
    ):
        raw_next_url = updates.get("url", gateway.url)
        next_url = raw_next_url.strip() if isinstance(raw_next_url, str) else ""
        next_token = updates.get("token", gateway.token)
        next_allow_insecure_tls = bool(
            updates.get("allow_insecure_tls", gateway.allow_insecure_tls),
        )
        next_disable_device_pairing = bool(
            updates.get("disable_device_pairing", gateway.disable_device_pairing),
        )
        if next_url:
            await service.assert_gateway_runtime_compatible(
                url=next_url,
                token=next_token,
                allow_insecure_tls=next_allow_insecure_tls,
                disable_device_pairing=next_disable_device_pairing,
            )
    await crud.patch(session, gateway, updates)
    if gateway.type != "zeroclaw":
        await service.ensure_main_agent(gateway, auth, action="update")
    return gateway


@router.post("/{gateway_id}/templates/sync", response_model=GatewayTemplatesSyncResult)
async def sync_gateway_templates(
    gateway_id: UUID,
    sync_query: GatewayTemplateSyncQuery = SYNC_QUERY_DEP,
    session: AsyncSession = SESSION_DEP,
    auth: AuthContext = AUTH_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> GatewayTemplatesSyncResult:
    """Sync templates for a gateway and optionally rotate runtime settings."""
    service = GatewayAdminLifecycleService(session)
    gateway = await service.require_gateway(
        gateway_id=gateway_id,
        organization_id=ctx.organization.id,
    )
    return await service.sync_templates(gateway, query=sync_query, auth=auth)


@router.delete("/{gateway_id}", response_model=OkResponse)
async def delete_gateway(
    gateway_id: UUID,
    session: AsyncSession = SESSION_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> OkResponse:
    """Delete a gateway in the caller's organization."""
    service = GatewayAdminLifecycleService(session)
    gateway = await service.require_gateway(
        gateway_id=gateway_id,
        organization_id=ctx.organization.id,
    )

    # Delete K8s Pod + Service for ZeroClaw gateways
    if gateway.type == "zeroclaw" and gateway.container_id:
        try:
            k8s = _get_k8s_manager()
            k8s.remove_container(gateway.container_id)
        except K8sManagerError:
            logger.warning(
                "Failed to clean up K8s resources for pod %s (gateway %s)",
                gateway.container_id,
                gateway.id,
            )

    main_agent = await service.find_main_agent(gateway)
    if main_agent is not None:
        await service.clear_agent_foreign_keys(agent_id=main_agent.id)
        await session.delete(main_agent)

    duplicate_main_agents = await Agent.objects.filter_by(
        gateway_id=gateway.id,
        board_id=None,
    ).all(session)
    for agent in duplicate_main_agents:
        if main_agent is not None and agent.id == main_agent.id:
            continue
        await service.clear_agent_foreign_keys(agent_id=agent.id)
        await session.delete(agent)

    await session.delete(gateway)
    await session.commit()
    return OkResponse()

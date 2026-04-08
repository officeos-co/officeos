"""ZeroClaw container lifecycle API endpoints."""

from __future__ import annotations

from typing import TYPE_CHECKING, Any
from uuid import UUID

from fastapi import APIRouter, Depends, HTTPException, Query

from app.api.deps import require_org_admin
from app.db.session import get_session
from app.services.openclaw.admin_service import GatewayAdminLifecycleService
from app.services.zeroclaw.k8s_manager import K8sManagerError, KubernetesManager

if TYPE_CHECKING:
    from sqlmodel.ext.asyncio.session import AsyncSession

    from app.services.organizations import OrganizationContext

router = APIRouter(prefix="/gateways", tags=["zeroclaw"])
SESSION_DEP = Depends(get_session)
ORG_ADMIN_DEP = Depends(require_org_admin)


def _get_k8s_manager() -> KubernetesManager:
    try:
        from kubernetes import client, config

        config.load_incluster_config()
        api = client.CoreV1Api()
        return KubernetesManager(api=api)
    except Exception as exc:
        raise K8sManagerError(
            "Kubernetes API is not available."
        ) from exc


async def _require_zeroclaw_gateway(
    gateway_id: UUID,
    session: Any,
    ctx: Any,
) -> Any:
    """Load gateway and verify it is a ZeroClaw gateway."""
    service = GatewayAdminLifecycleService(session)
    gateway = await service.require_gateway(
        gateway_id=gateway_id,
        organization_id=ctx.organization.id,
    )
    if gateway.type != "zeroclaw":
        raise HTTPException(
            status_code=422,
            detail="This endpoint is only available for ZeroClaw gateways.",
        )
    return gateway


@router.post("/{gateway_id}/container/start")
async def start_container(
    gateway_id: UUID,
    session: AsyncSession = SESSION_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> dict[str, str]:
    gateway = await _require_zeroclaw_gateway(gateway_id, session, ctx)
    if not gateway.container_id:
        raise HTTPException(status_code=404, detail="No container associated.")
    try:
        k8s = _get_k8s_manager()
        # In K8s, "start" means ensure the pod exists. If it was deleted,
        # restart recreates it via restartPolicy: Always.
        k8s.restart_container(gateway.container_id)
        gateway.container_status = "running"
        session.add(gateway)
        await session.commit()
        return {"status": "started"}
    except K8sManagerError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.post("/{gateway_id}/container/stop")
async def stop_container(
    gateway_id: UUID,
    session: AsyncSession = SESSION_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> dict[str, str]:
    gateway = await _require_zeroclaw_gateway(gateway_id, session, ctx)
    if not gateway.container_id:
        raise HTTPException(status_code=404, detail="No container associated.")
    try:
        k8s = _get_k8s_manager()
        k8s.stop_container(gateway.container_id)
        gateway.container_status = "stopped"
        session.add(gateway)
        await session.commit()
        return {"status": "stopped"}
    except K8sManagerError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.post("/{gateway_id}/container/restart")
async def restart_container(
    gateway_id: UUID,
    session: AsyncSession = SESSION_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> dict[str, str]:
    gateway = await _require_zeroclaw_gateway(gateway_id, session, ctx)
    if not gateway.container_id:
        raise HTTPException(status_code=404, detail="No container associated.")
    try:
        k8s = _get_k8s_manager()
        k8s.restart_container(gateway.container_id)
        gateway.container_status = "running"
        session.add(gateway)
        await session.commit()
        return {"status": "restarted"}
    except K8sManagerError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.get("/{gateway_id}/container/status")
async def get_container_status(
    gateway_id: UUID,
    session: AsyncSession = SESSION_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> dict[str, Any]:
    gateway = await _require_zeroclaw_gateway(gateway_id, session, ctx)
    if not gateway.container_id:
        return {"status": "not_found", "health": None}
    try:
        k8s = _get_k8s_manager()
        status = k8s.get_status(gateway.container_id)
        return {"status": status.status, "health": status.health}
    except K8sManagerError:
        return {"status": "error", "health": None}


@router.get("/{gateway_id}/container/logs")
async def get_container_logs(
    gateway_id: UUID,
    tail: int = Query(default=100, ge=1, le=10000),
    session: AsyncSession = SESSION_DEP,
    ctx: OrganizationContext = ORG_ADMIN_DEP,
) -> dict[str, str]:
    gateway = await _require_zeroclaw_gateway(gateway_id, session, ctx)
    if not gateway.container_id:
        return {"logs": ""}
    try:
        k8s = _get_k8s_manager()
        logs = k8s.get_logs(gateway.container_id, tail=tail)
        return {"logs": logs}
    except K8sManagerError:
        return {"logs": ""}

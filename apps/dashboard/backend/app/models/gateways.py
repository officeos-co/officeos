"""Gateway model storing organization-level gateway integration metadata."""

from __future__ import annotations

from datetime import datetime
from uuid import UUID, uuid4

from sqlmodel import Field

from app.core.time import utcnow
from app.models.base import QueryModel

RUNTIME_ANNOTATION_TYPES = (datetime,)


class Gateway(QueryModel, table=True):
    """Configured external gateway endpoint and authentication settings."""

    __tablename__ = "gateways"  # pyright: ignore[reportAssignmentType]

    id: UUID = Field(default_factory=uuid4, primary_key=True)
    organization_id: UUID = Field(foreign_key="organizations.id", index=True)
    name: str
    url: str
    token: str | None = Field(default=None)
    disable_device_pairing: bool = Field(default=False)
    workspace_root: str
    allow_insecure_tls: bool = Field(default=False)
    created_at: datetime = Field(default_factory=utcnow)
    updated_at: datetime = Field(default_factory=utcnow)

    # ZeroClaw cloud-native fields
    type: str = Field(default="openclaw", index=True)
    container_id: str | None = Field(default=None)
    host_port: int | None = Field(default=None)
    docker_image: str | None = Field(default=None)
    container_status: str | None = Field(default=None)

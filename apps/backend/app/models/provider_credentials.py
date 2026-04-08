"""Per-organization LLM provider credentials.

One row per (organization, provider) — the backend stores a separate
API key for each LLM provider the org has configured. The gateway
creation flow looks the key up by provider name and injects it into
the spawned agent pod as `API_KEY`.
"""

from __future__ import annotations

from datetime import datetime
from uuid import UUID, uuid4

from sqlalchemy import UniqueConstraint
from sqlmodel import Field

from app.core.time import utcnow
from app.models.tenancy import TenantScoped


class ProviderCredential(TenantScoped, table=True):
    """A single provider's API key for one organization."""

    __tablename__ = "provider_credentials"  # pyright: ignore[reportAssignmentType]
    __table_args__ = (
        UniqueConstraint(
            "organization_id",
            "provider",
            name="uq_provider_credentials_org_provider",
        ),
    )

    id: UUID = Field(default_factory=uuid4, primary_key=True)
    organization_id: UUID = Field(foreign_key="organizations.id", index=True)
    provider: str = Field(index=True)
    api_key: str = Field()
    created_at: datetime = Field(default_factory=utcnow)
    updated_at: datetime = Field(default_factory=utcnow)

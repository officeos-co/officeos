"""Per-org credentials/config for installed skills.

Skills themselves are code (see `app.skills`). This table only stores
the auth material needed to call the upstream provider (Notion API key,
GitHub PAT, Google service-account JSON, etc.) plus an `enabled` flag
that acts as the "installed" toggle in the dashboard.

Global/one-per-org: no per-agent, no per-role — that's deliberate for
the v1 demo.
"""

from __future__ import annotations

from datetime import datetime
from uuid import UUID, uuid4

from sqlalchemy import JSON, Column, UniqueConstraint
from sqlmodel import Field

from app.core.time import utcnow
from app.models.tenancy import TenantScoped


class SkillCredential(TenantScoped, table=True):
    """Per-organization credentials + install state for a single skill."""

    __tablename__ = "skill_credentials"  # pyright: ignore[reportAssignmentType]
    __table_args__ = (
        UniqueConstraint(
            "organization_id",
            "skill_name",
            name="uq_skill_credentials_org_skill",
        ),
    )

    id: UUID = Field(default_factory=uuid4, primary_key=True)
    organization_id: UUID = Field(foreign_key="organizations.id", index=True)
    skill_name: str = Field(index=True)
    enabled: bool = Field(default=False)
    credentials: dict[str, object] = Field(
        default_factory=dict,
        sa_column=Column("credentials", JSON, nullable=False),
    )
    created_at: datetime = Field(default_factory=utcnow)
    updated_at: datetime = Field(default_factory=utcnow)

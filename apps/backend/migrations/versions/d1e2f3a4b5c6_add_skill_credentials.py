"""add skill_credentials table

Revision ID: d1e2f3a4b5c6
Revises: c7e1a2b3d4f5, a9b1c2d3e4f7
Create Date: 2026-04-08 12:00:00.000000

Merge migration: joins the two open heads (c7e1a2b3d4f5, a9b1c2d3e4f7)
and adds the new skill_credentials table used by apps.skills.
"""

from __future__ import annotations

import sqlalchemy as sa
import sqlmodel  # noqa: F401  (used implicitly by generated models)
from alembic import op

# revision identifiers, used by Alembic.
revision = "d1e2f3a4b5c6"
down_revision = ("c7e1a2b3d4f5", "a9b1c2d3e4f7")
branch_labels = None
depends_on = None


def _has_table(name: str) -> bool:
    return sa.inspect(op.get_bind()).has_table(name)


def upgrade() -> None:
    if _has_table("skill_credentials"):
        return
    op.create_table(
        "skill_credentials",
        sa.Column("id", sa.Uuid(), primary_key=True, nullable=False),
        sa.Column("organization_id", sa.Uuid(), nullable=False),
        sa.Column("skill_name", sa.String(), nullable=False),
        sa.Column("enabled", sa.Boolean(), nullable=False, server_default=sa.false()),
        sa.Column("credentials", sa.JSON(), nullable=False),
        sa.Column("created_at", sa.DateTime(), nullable=False),
        sa.Column("updated_at", sa.DateTime(), nullable=False),
        sa.ForeignKeyConstraint(["organization_id"], ["organizations.id"]),
        sa.UniqueConstraint(
            "organization_id",
            "skill_name",
            name="uq_skill_credentials_org_skill",
        ),
    )
    op.create_index(
        "ix_skill_credentials_organization_id",
        "skill_credentials",
        ["organization_id"],
    )
    op.create_index(
        "ix_skill_credentials_skill_name",
        "skill_credentials",
        ["skill_name"],
    )


def downgrade() -> None:
    if not _has_table("skill_credentials"):
        return
    op.drop_index("ix_skill_credentials_skill_name", table_name="skill_credentials")
    op.drop_index(
        "ix_skill_credentials_organization_id", table_name="skill_credentials"
    )
    op.drop_table("skill_credentials")

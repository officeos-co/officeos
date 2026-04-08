"""add provider_credentials table, drop organizations.llm_api_key

Revision ID: c4d5e6f7a8b9
Revises: b3c4d5e6f7a8
Create Date: 2026-04-08 19:05:00.000000

Moves the LLM API key from `organizations.llm_api_key` (single key
shared across providers) to a per-provider table so each
organization can configure one key per provider (OpenAI, Anthropic,
OpenRouter, …).
"""

from __future__ import annotations

import sqlalchemy as sa
from alembic import op

# revision identifiers, used by Alembic.
revision = "c4d5e6f7a8b9"
down_revision = "b3c4d5e6f7a8"
branch_labels = None
depends_on = None


def _has_table(name: str) -> bool:
    return sa.inspect(op.get_bind()).has_table(name)


def _has_column(table: str, column: str) -> bool:
    inspector = sa.inspect(op.get_bind())
    if not inspector.has_table(table):
        return False
    return any(c["name"] == column for c in inspector.get_columns(table))


def upgrade() -> None:
    if not _has_table("provider_credentials"):
        op.create_table(
            "provider_credentials",
            sa.Column("id", sa.Uuid(), primary_key=True, nullable=False),
            sa.Column("organization_id", sa.Uuid(), nullable=False),
            sa.Column("provider", sa.String(), nullable=False),
            sa.Column("api_key", sa.String(), nullable=False),
            sa.Column("created_at", sa.DateTime(), nullable=False),
            sa.Column("updated_at", sa.DateTime(), nullable=False),
            sa.ForeignKeyConstraint(["organization_id"], ["organizations.id"]),
            sa.UniqueConstraint(
                "organization_id",
                "provider",
                name="uq_provider_credentials_org_provider",
            ),
        )
        op.create_index(
            "ix_provider_credentials_organization_id",
            "provider_credentials",
            ["organization_id"],
        )
        op.create_index(
            "ix_provider_credentials_provider",
            "provider_credentials",
            ["provider"],
        )

    if _has_column("organizations", "llm_api_key"):
        op.drop_column("organizations", "llm_api_key")


def downgrade() -> None:
    if not _has_column("organizations", "llm_api_key"):
        op.add_column(
            "organizations",
            sa.Column("llm_api_key", sa.String(), nullable=True),
        )
    if _has_table("provider_credentials"):
        op.drop_index(
            "ix_provider_credentials_provider", table_name="provider_credentials"
        )
        op.drop_index(
            "ix_provider_credentials_organization_id",
            table_name="provider_credentials",
        )
        op.drop_table("provider_credentials")

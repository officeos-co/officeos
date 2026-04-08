"""add organizations.llm_api_key column

Revision ID: b3c4d5e6f7a8
Revises: f7a8b9c0d1e2
Create Date: 2026-04-08 18:55:00.000000

Stores the LLM provider API key per-organization. Set via the
dashboard (gateway-create form or future org settings page) and
injected into every zeroclaw pod in that org as `API_KEY`.
"""

from __future__ import annotations

import sqlalchemy as sa
from alembic import op

# revision identifiers, used by Alembic.
revision = "b3c4d5e6f7a8"
down_revision = "f7a8b9c0d1e2"
branch_labels = None
depends_on = None


def _has_column(table: str, column: str) -> bool:
    inspector = sa.inspect(op.get_bind())
    if not inspector.has_table(table):
        return False
    return any(c["name"] == column for c in inspector.get_columns(table))


def upgrade() -> None:
    if not _has_column("organizations", "llm_api_key"):
        op.add_column(
            "organizations",
            sa.Column("llm_api_key", sa.String(), nullable=True),
        )


def downgrade() -> None:
    if _has_column("organizations", "llm_api_key"):
        op.drop_column("organizations", "llm_api_key")

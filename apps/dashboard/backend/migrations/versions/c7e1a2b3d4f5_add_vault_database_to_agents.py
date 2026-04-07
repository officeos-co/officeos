"""Add vault_database column to agents.

Phase 3 of the zeroclaw-core strip-down: each agent gets its own
Obsidian vault (backed by a dedicated CouchDB database). This column
stores the name of that database so the lifecycle service and the K8s
manager can look it up when (a) seeding the vault at agent creation and
(b) building the per-agent ConfigMap mount for the agent pod.

Null for legacy agents created before the Phase 3 cut-over; non-null
for any agent whose vault has been provisioned.

Revision ID: c7e1a2b3d4f5
Revises: b2c3d4e5f6a7
Create Date: 2026-04-08 00:00:00.000000
"""

from __future__ import annotations

import sqlalchemy as sa
from alembic import op

# revision identifiers, used by Alembic.
revision = "c7e1a2b3d4f5"
down_revision = "b2c3d4e5f6a7"
branch_labels = None
depends_on = None


def upgrade() -> None:
    """Add the nullable vault_database column with a covering index."""
    op.add_column(
        "agents",
        sa.Column("vault_database", sa.String(), nullable=True),
    )
    op.create_index(
        "ix_agents_vault_database",
        "agents",
        ["vault_database"],
        unique=False,
    )


def downgrade() -> None:
    """Drop the vault_database column and its index."""
    op.drop_index("ix_agents_vault_database", table_name="agents")
    op.drop_column("agents", "vault_database")

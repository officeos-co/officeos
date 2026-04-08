"""Add gateway type discriminator and Docker container fields.

Revision ID: a1c2d3e4f5b6
Revises: fa6e83f8d9a1
Create Date: 2026-04-05 00:00:00.000000

NOTE: this file previously collided with `a1b2c3d4e5f6_add_webhook_secret.py`
on the same revision ID — Alembic treated it as a phantom head. Renamed
to `a1c2d3e4f5b6` and chained via the `f7a8b9c0d1e2` merge migration.
"""

from __future__ import annotations

import sqlalchemy as sa
from alembic import op

# revision identifiers, used by Alembic.
revision = "a1c2d3e4f5b6"
down_revision = "fa6e83f8d9a1"
branch_labels = None
depends_on = None


def upgrade() -> None:
    """Add ZeroClaw cloud-native fields to gateways table."""
    bind = op.get_bind()
    inspector = sa.inspect(bind)

    existing_columns = {col["name"] for col in inspector.get_columns("gateways")}

    if "type" not in existing_columns:
        op.add_column(
            "gateways",
            sa.Column("type", sa.String(), server_default="openclaw", nullable=False),
        )
    if "container_id" not in existing_columns:
        op.add_column(
            "gateways",
            sa.Column("container_id", sa.String(), nullable=True),
        )
    if "host_port" not in existing_columns:
        op.add_column(
            "gateways",
            sa.Column("host_port", sa.Integer(), nullable=True),
        )
    if "docker_image" not in existing_columns:
        op.add_column(
            "gateways",
            sa.Column("docker_image", sa.String(), nullable=True),
        )
    if "container_status" not in existing_columns:
        op.add_column(
            "gateways",
            sa.Column("container_status", sa.String(), nullable=True),
        )

    # Add index on type column
    index_names = {item["name"] for item in inspector.get_indexes("gateways")}
    if "ix_gateways_type" not in index_names:
        op.create_index("ix_gateways_type", "gateways", ["type"])

    # Backfill existing rows
    op.execute("UPDATE gateways SET type = 'openclaw' WHERE type IS NULL")


def downgrade() -> None:
    """Remove ZeroClaw fields from gateways table."""
    op.drop_index("ix_gateways_type", table_name="gateways")
    op.drop_column("gateways", "container_status")
    op.drop_column("gateways", "docker_image")
    op.drop_column("gateways", "host_port")
    op.drop_column("gateways", "container_id")
    op.drop_column("gateways", "type")

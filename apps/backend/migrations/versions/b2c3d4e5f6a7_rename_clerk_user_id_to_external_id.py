"""Rename clerk_user_id to external_id on users table.

Revision ID: b2c3d4e5f6a7
Revises: a1b2c3d4e5f6
Create Date: 2026-04-05 12:00:00.000000

"""

from __future__ import annotations

import sqlalchemy as sa
from alembic import op

revision = "b2c3d4e5f6a7"
down_revision = "a1b2c3d4e5f6"
branch_labels = None
depends_on = None


def upgrade() -> None:
    """Rename clerk_user_id column to external_id."""
    bind = op.get_bind()
    inspector = sa.inspect(bind)
    columns = {col["name"] for col in inspector.get_columns("users")}

    if "clerk_user_id" in columns and "external_id" not in columns:
        op.alter_column("users", "clerk_user_id", new_column_name="external_id")

    # Update index name if it exists
    index_names = {item["name"] for item in inspector.get_indexes("users")}
    if "ix_users_clerk_user_id" in index_names:
        op.drop_index("ix_users_clerk_user_id", table_name="users")
        op.create_index("ix_users_external_id", "users", ["external_id"], unique=True)


def downgrade() -> None:
    """Rename external_id back to clerk_user_id."""
    op.alter_column("users", "external_id", new_column_name="clerk_user_id")
    op.drop_index("ix_users_external_id", table_name="users")
    op.create_index("ix_users_clerk_user_id", "users", ["clerk_user_id"], unique=True)

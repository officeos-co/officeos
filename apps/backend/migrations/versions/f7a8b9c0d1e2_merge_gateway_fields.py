"""merge gateway_type_and_docker_fields branch into main

Revision ID: f7a8b9c0d1e2
Revises: e2a3b4c5d6e7, a1c2d3e4f5b6
Create Date: 2026-04-08 18:20:00.000000

Joins the renamed `a1c2d3e4f5b6_add_gateway_type_and_docker_fields`
head back into the main migration graph. No schema changes — this
file exists purely to collapse two heads into one.
"""

from __future__ import annotations

# revision identifiers, used by Alembic.
revision = "f7a8b9c0d1e2"
down_revision = ("e2a3b4c5d6e7", "a1c2d3e4f5b6")
branch_labels = None
depends_on = None


def upgrade() -> None:
    pass


def downgrade() -> None:
    pass

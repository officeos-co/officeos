"""drop marketplace skills tables

Revision ID: e2a3b4c5d6e7
Revises: d1e2f3a4b5c6
Create Date: 2026-04-08 14:00:00.000000

The marketplace_skills / skill_packs / gateway_installed_skills tables
come from the old OpenClaw-style git-repo skill catalog, which has been
replaced by backend-resident skills (see `app.skills`) and per-org
`skill_credentials`. Drop the dead tables — nothing reads them anymore.
"""

from __future__ import annotations

import sqlalchemy as sa
from alembic import op

# revision identifiers, used by Alembic.
revision = "e2a3b4c5d6e7"
down_revision = "d1e2f3a4b5c6"
branch_labels = None
depends_on = None


def _has_table(name: str) -> bool:
    return sa.inspect(op.get_bind()).has_table(name)


def upgrade() -> None:
    # Drop the junction first (FK to marketplace_skills).
    if _has_table("gateway_installed_skills"):
        op.drop_table("gateway_installed_skills")
    if _has_table("marketplace_skills"):
        op.drop_table("marketplace_skills")
    if _has_table("skill_packs"):
        op.drop_table("skill_packs")


def downgrade() -> None:
    # Irreversible: the new system does not need these tables, and
    # recreating the old schema would require wiring back a pile of
    # deleted models. Leave as a no-op.
    pass

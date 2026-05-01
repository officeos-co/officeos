from __future__ import annotations

import json
import logging
from pathlib import Path
from typing import Any

logger = logging.getLogger(__name__)

_TEMPLATES: dict[str, dict[str, Any]] = {
    "HIPAA": {
        "require_auth_state_encryption": True,
        "require_operator_id": True,
        "pii_scrub_enabled": True,
        "pii_scrub_screenshot": True,
        "pii_scrub_network": True,
        "pii_scrub_console": True,
        "require_approval_for_uploads": True,
        "witness_enabled": True,
        "auth_state_max_age_hours": 4.0,
        "session_isolation_mode": "docker_ephemeral",
    },
    "SOC2": {
        "require_operator_id": True,
        "pii_scrub_enabled": True,
        "pii_scrub_network": True,
        "require_approval_for_uploads": True,
        "witness_enabled": True,
        "auth_state_max_age_hours": 24.0,
    },
    "GDPR": {
        "pii_scrub_enabled": True,
        "pii_scrub_screenshot": True,
        "pii_scrub_network": True,
        "pii_scrub_console": True,
        "require_approval_for_uploads": True,
        "auth_state_max_age_hours": 24.0,
    },
    "PCI-DSS": {
        "require_auth_state_encryption": True,
        "require_operator_id": True,
        "pii_scrub_enabled": True,
        "pii_scrub_screenshot": True,
        "pii_scrub_network": True,
        "pii_scrub_console": True,
        "require_approval_for_uploads": True,
        "witness_enabled": True,
        "auth_state_max_age_hours": 1.0,
        "session_isolation_mode": "docker_ephemeral",
    },
}

VALID_TEMPLATES = set(_TEMPLATES)


def apply_compliance_template(settings: Any, template_name: str) -> dict[str, Any]:
    name = template_name.upper().strip()
    if name not in _TEMPLATES:
        raise ValueError(f"Unknown compliance template: {name!r}. Valid options: {sorted(VALID_TEMPLATES)}")

    overrides = _TEMPLATES[name]
    applied: dict[str, Any] = {}
    for attribute, value in overrides.items():
        current = getattr(settings, attribute, None)
        if current != value:
            setattr(settings, attribute, value)
            logger.info("compliance[%s]: %s = %r (was %r)", name, attribute, value, current)
        applied[attribute] = value
    return applied


def write_compliance_manifest(*, template_name: str, overrides: dict[str, Any], output_path: Path) -> None:
    output_path.parent.mkdir(parents=True, exist_ok=True)
    manifest = {
        "template": template_name,
        "applied_overrides": overrides,
        "note": (
            "This manifest records the compliance template applied at startup. "
            "Settings were overridden as shown above."
        ),
    }
    tmp_path = output_path.with_suffix(".json.tmp")
    tmp_path.write_text(json.dumps(manifest, indent=2), encoding="utf-8")
    tmp_path.replace(output_path)
    logger.info("compliance manifest written to %s", output_path)

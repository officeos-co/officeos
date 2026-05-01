from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path
from unittest.mock import MagicMock

from app.compliance import VALID_TEMPLATES, apply_compliance_template, write_compliance_manifest


def _settings() -> MagicMock:
    settings = MagicMock()
    settings.require_auth_state_encryption = False
    settings.require_operator_id = False
    settings.pii_scrub_enabled = False
    settings.pii_scrub_screenshot = False
    settings.pii_scrub_network = False
    settings.pii_scrub_console = False
    settings.require_approval_for_uploads = False
    settings.witness_enabled = False
    settings.auth_state_max_age_hours = 72.0
    settings.session_isolation_mode = "shared_browser_node"
    return settings


class ComplianceTests(unittest.TestCase):
    def test_all_templates_apply(self) -> None:
        for template in sorted(VALID_TEMPLATES):
            with self.subTest(template=template):
                settings = _settings()

                overrides = apply_compliance_template(settings, template)

                self.assertIsInstance(overrides, dict)
                self.assertTrue(overrides)

    def test_hipaa_sets_encryption(self) -> None:
        settings = _settings()
        apply_compliance_template(settings, "HIPAA")
        self.assertTrue(settings.require_auth_state_encryption)

    def test_hipaa_sets_isolation(self) -> None:
        settings = _settings()
        apply_compliance_template(settings, "HIPAA")
        self.assertEqual(settings.session_isolation_mode, "docker_ephemeral")

    def test_hipaa_short_max_age(self) -> None:
        settings = _settings()
        apply_compliance_template(settings, "HIPAA")
        self.assertEqual(settings.auth_state_max_age_hours, 4.0)

    def test_invalid_template_raises(self) -> None:
        settings = _settings()
        with self.assertRaisesRegex(ValueError, "Unknown compliance template"):
            apply_compliance_template(settings, "FAKECOMPLIANCE")

    def test_case_insensitive(self) -> None:
        settings = _settings()
        overrides = apply_compliance_template(settings, "hipaa")
        self.assertTrue(overrides)

    def test_write_manifest(self) -> None:
        with tempfile.TemporaryDirectory() as tmpdir:
            manifest_path = Path(tmpdir) / "manifest.json"

            write_compliance_manifest(
                template_name="SOC2",
                overrides={"pii_scrub_enabled": True},
                output_path=manifest_path,
            )

            self.assertTrue(manifest_path.exists())
            data = json.loads(manifest_path.read_text(encoding="utf-8"))
            self.assertEqual(data["template"], "SOC2")
            self.assertIn("applied_overrides", data)

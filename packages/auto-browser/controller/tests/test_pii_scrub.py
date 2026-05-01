from __future__ import annotations

from types import SimpleNamespace

from app.pii_scrub import PiiScrubber, scrub_text


def _settings(**overrides: object) -> SimpleNamespace:
    defaults: dict[str, object] = {
        "pii_scrub_enabled": True,
        "pii_scrub_screenshot": True,
        "pii_scrub_network": True,
        "pii_scrub_console": True,
        "pii_scrub_replacement": "[REDACTED]",
        "pii_scrub_audit_report": True,
        "pii_scrub_patterns": "",
    }
    defaults.update(overrides)
    return SimpleNamespace(**defaults)


def test_phone_us_no_false_positive_version() -> None:
    result = scrub_text("python 3.11.0 installed")
    assert "[REDACTED]" not in result.text


def test_phone_us_matches_real_number() -> None:
    result = scrub_text("call me at 555-867-5309 please")
    assert "[REDACTED]" in result.text


def test_default_settings_disable_generic_hex_token() -> None:
    scrubber = PiiScrubber.from_settings(_settings())

    assert scrubber.enabled_patterns is not None
    assert "generic_hex_token" not in scrubber.enabled_patterns


def test_explicit_patterns_can_reenable_generic_hex_token() -> None:
    scrubber = PiiScrubber.from_settings(_settings(pii_scrub_patterns="generic_hex_token,email"))

    assert scrubber.enabled_patterns == {"generic_hex_token", "email"}

# ruff: noqa: INP001
"""Pytest configuration shared across backend tests."""

import os
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
if str(ROOT) not in sys.path:
    sys.path.insert(0, str(ROOT))

# Provide deterministic defaults for settings initialization during tests.
os.environ["BASE_URL"] = "http://localhost:8000"
os.environ["SESSION_SECRET"] = "test-session-secret-for-unit-tests-only-0123456789"
os.environ["GOOGLE_CLIENT_ID"] = "test-client-id"
os.environ["GOOGLE_CLIENT_SECRET"] = "test-client-secret"

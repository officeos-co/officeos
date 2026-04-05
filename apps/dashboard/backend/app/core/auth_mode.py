"""Shared auth-mode enum values."""

from __future__ import annotations

from enum import Enum


class AuthMode(str, Enum):
    """Supported authentication modes for backend and frontend."""

    LOCAL = "local"
    GOOGLE = "google"

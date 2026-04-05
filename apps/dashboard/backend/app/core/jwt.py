"""Session JWT creation and verification.

Issues short-lived JWTs signed with a server-side secret for authenticating
dashboard sessions after OAuth login.
"""

from __future__ import annotations

import os
from datetime import datetime, timedelta, timezone

import jwt

SESSION_SECRET = os.environ.get("SESSION_SECRET", "")
SESSION_EXPIRY_HOURS = int(os.environ.get("SESSION_EXPIRY_HOURS", "24"))
ALGORITHM = "HS256"


class SessionTokenError(Exception):
    """Raised when session token creation or verification fails."""


def create_session_token(
    *,
    user_id: str,
    email: str | None = None,
    name: str | None = None,
) -> str:
    """Issue a signed session JWT."""
    secret = SESSION_SECRET
    if not secret:
        raise SessionTokenError("SESSION_SECRET environment variable is required.")

    now = datetime.now(timezone.utc)
    payload = {
        "sub": user_id,
        "iat": now,
        "exp": now + timedelta(hours=SESSION_EXPIRY_HOURS),
    }
    if email:
        payload["email"] = email
    if name:
        payload["name"] = name

    return jwt.encode(payload, secret, algorithm=ALGORITHM)


def verify_session_token(token: str) -> dict:
    """Verify and decode a session JWT. Returns the claims dict."""
    secret = SESSION_SECRET
    if not secret:
        raise SessionTokenError("SESSION_SECRET environment variable is required.")

    try:
        return jwt.decode(token, secret, algorithms=[ALGORITHM])
    except jwt.ExpiredSignatureError as exc:
        raise SessionTokenError("Session token has expired.") from exc
    except jwt.InvalidTokenError as exc:
        raise SessionTokenError(f"Invalid session token: {exc}") from exc

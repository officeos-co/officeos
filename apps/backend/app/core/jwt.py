"""Session JWT creation and verification.

Issues short-lived JWTs signed with a server-side secret for authenticating
dashboard sessions after OAuth login.

Secret material (`session_secret`, `session_expiry_hours`) comes from
`app.core.config.settings`, which in turn is loaded from
`apps/backend/.env`. Reading via settings (not `os.environ` directly)
keeps the lookup consistent with every other config value — otherwise
pydantic-settings loads the .env and this module never sees it.
"""

from __future__ import annotations

from datetime import datetime, timedelta, timezone

import jwt

from app.core.config import settings

ALGORITHM = "HS256"


class SessionTokenError(Exception):
    """Raised when session token creation or verification fails."""


def _secret() -> str:
    """Return the configured session secret or raise."""
    secret = settings.session_secret
    if not secret:
        raise SessionTokenError(
            "session_secret must be set in apps/backend/.env "
            "(generate with `openssl rand -hex 32`)."
        )
    return secret


def create_session_token(
    *,
    user_id: str,
    email: str | None = None,
    name: str | None = None,
) -> str:
    """Issue a signed session JWT."""
    secret = _secret()
    now = datetime.now(timezone.utc)
    payload = {
        "sub": user_id,
        "iat": now,
        "exp": now + timedelta(hours=settings.session_expiry_hours),
    }
    if email:
        payload["email"] = email
    if name:
        payload["name"] = name

    return jwt.encode(payload, secret, algorithm=ALGORITHM)


def verify_session_token(token: str) -> dict:
    """Verify and decode a session JWT. Returns the claims dict."""
    secret = _secret()
    try:
        return jwt.decode(token, secret, algorithms=[ALGORITHM])
    except jwt.ExpiredSignatureError as exc:
        raise SessionTokenError("Session token has expired.") from exc
    except jwt.InvalidTokenError as exc:
        raise SessionTokenError(f"Invalid session token: {exc}") from exc

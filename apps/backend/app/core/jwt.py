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


# ─────────────────────────────────────────────────────────────────────
# Gateway UI proxy tokens
#
# Short-lived JWTs that authorize browser access to the agent-dashboard
# reverse-proxy at `/api/gateways/<id>/ui/*`. Scoped to a single
# gateway_id so a leaked token can't reach any other agent. Issued by
# the dashboard user after a normal auth check, passed via `?t=...` on
# the initial page load, then promoted to a `Path`-scoped cookie by
# the proxy so subsequent SPA fetches inherit the same auth.
# ─────────────────────────────────────────────────────────────────────

UI_TOKEN_TYPE = "gw-ui"
UI_TOKEN_TTL_MINUTES = 30


def create_gateway_ui_token(*, gateway_id: str, user_id: str) -> str:
    """Issue a 30-minute JWT authorizing UI access to one gateway."""
    secret = _secret()
    now = datetime.now(timezone.utc)
    payload = {
        "typ": UI_TOKEN_TYPE,
        "sub": user_id,
        "gw": gateway_id,
        "iat": now,
        "exp": now + timedelta(minutes=UI_TOKEN_TTL_MINUTES),
    }
    return jwt.encode(payload, secret, algorithm=ALGORITHM)


def verify_gateway_ui_token(token: str, *, expected_gateway_id: str) -> dict:
    """Verify a gateway-UI token and ensure it was minted for this gateway.

    Rejects tokens of other types, expired tokens, and tokens minted
    for a different `gateway_id`.
    """
    secret = _secret()
    try:
        claims = jwt.decode(token, secret, algorithms=[ALGORITHM])
    except jwt.ExpiredSignatureError as exc:
        raise SessionTokenError("UI token has expired.") from exc
    except jwt.InvalidTokenError as exc:
        raise SessionTokenError(f"Invalid UI token: {exc}") from exc
    if claims.get("typ") != UI_TOKEN_TYPE:
        raise SessionTokenError("Token is not a gateway-UI token.")
    if claims.get("gw") != expected_gateway_id:
        raise SessionTokenError("UI token was issued for a different gateway.")
    return claims

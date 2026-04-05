"""User authentication via Google OAuth session JWTs.

This module resolves an authenticated user from inbound HTTP requests by
verifying session JWTs issued after Google OAuth login.

The public surface area is the `get_auth_context*` dependencies, which return
an `AuthContext` used across API routers.
"""

from __future__ import annotations

from dataclasses import dataclass
from typing import TYPE_CHECKING, Literal

from fastapi import Depends, HTTPException, Request, status
from fastapi.security import HTTPAuthorizationCredentials, HTTPBearer

from app.core.jwt import SessionTokenError, verify_session_token
from app.core.logging import get_logger
from app.db import crud
from app.db.session import get_session
from app.models.users import User

if TYPE_CHECKING:
    from sqlmodel.ext.asyncio.session import AsyncSession

logger = get_logger(__name__)
security = HTTPBearer(auto_error=False)
SECURITY_DEP = Depends(security)
SESSION_DEP = Depends(get_session)


@dataclass
class AuthContext:
    """Authenticated user context resolved from inbound auth headers."""

    actor_type: Literal["user"]
    user: User | None = None


def _extract_bearer_token(authorization: str | None) -> str | None:
    """Extract the bearer token from an `Authorization` header."""
    if not authorization:
        return None
    value = authorization.strip()
    if not value.lower().startswith("bearer "):
        return None
    token = value.split(" ", maxsplit=1)[1].strip()
    return token or None


async def _get_or_create_user(
    session: AsyncSession,
    *,
    external_id: str,
    email: str | None = None,
    name: str | None = None,
) -> User:
    """Find user by external_id or create a new one."""
    defaults: dict[str, object | None] = {
        "email": email,
        "name": name,
    }
    user, created = await crud.get_or_create(
        session,
        User,
        external_id=external_id,
        defaults=defaults,
    )

    changed = False
    if email and user.email != email:
        user.email = email
        changed = True
    if name and not user.name:
        user.name = name
        changed = True
    if changed:
        session.add(user)
        await session.commit()
        await session.refresh(user)
        logger.info(
            "auth.user.sync external_id=%s created=%s",
            external_id[-6:],
            created,
        )

    return user


async def _resolve_session_auth(
    request: Request,
    session: AsyncSession,
    *,
    required: bool,
) -> AuthContext | None:
    """Verify a session JWT and resolve the user."""
    token = _extract_bearer_token(request.headers.get("Authorization"))
    if token is None:
        if required:
            raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED)
        return None

    try:
        claims = verify_session_token(token)
    except SessionTokenError:
        if required:
            raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED)
        return None

    external_id = claims.get("sub")
    if not external_id:
        if required:
            raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED)
        return None

    user = await _get_or_create_user(
        session,
        external_id=external_id,
        email=claims.get("email"),
        name=claims.get("name"),
    )

    from app.services.organizations import ensure_member_for_user

    await ensure_member_for_user(session, user)

    return AuthContext(actor_type="user", user=user)


async def get_auth_context(
    request: Request,
    credentials: HTTPAuthorizationCredentials | None = SECURITY_DEP,
    session: AsyncSession = SESSION_DEP,
) -> AuthContext:
    """Resolve required authenticated user context."""
    auth = await _resolve_session_auth(request, session, required=True)
    if auth is None:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED)
    return auth


async def get_auth_context_optional(
    request: Request,
    credentials: HTTPAuthorizationCredentials | None = SECURITY_DEP,
    session: AsyncSession = SESSION_DEP,
) -> AuthContext | None:
    """Resolve user context if available, otherwise return `None`."""
    if request.headers.get("X-Agent-Token"):
        return None
    return await _resolve_session_auth(request, session, required=False)

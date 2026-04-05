"""Authentication endpoints — Google OAuth callback and user bootstrap."""

from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, status
from pydantic import BaseModel

from app.core.auth import AuthContext, get_auth_context
from app.core.google_auth import (
    GoogleAuthError,
    build_google_auth_url,
    exchange_code_for_tokens,
    get_google_user_info,
)
from app.core.jwt import create_session_token
from app.db import crud
from app.db.session import get_session
from app.models.users import User
from app.schemas.users import UserRead

router = APIRouter(prefix="/auth", tags=["auth"])
AUTH_CONTEXT_DEP = Depends(get_auth_context)
SESSION_DEP = Depends(get_session)


class GoogleCallbackRequest(BaseModel):
    code: str


class GoogleCallbackResponse(BaseModel):
    token: str
    user: UserRead


class GoogleAuthUrlResponse(BaseModel):
    url: str


@router.get("/google/url", response_model=GoogleAuthUrlResponse)
async def get_google_auth_url() -> GoogleAuthUrlResponse:
    """Return the Google OAuth consent URL for the frontend to redirect to."""
    try:
        url = build_google_auth_url()
        return GoogleAuthUrlResponse(url=url)
    except GoogleAuthError as exc:
        raise HTTPException(status_code=503, detail=str(exc)) from exc


@router.post("/google/callback", response_model=GoogleCallbackResponse)
async def google_callback(
    payload: GoogleCallbackRequest,
    session=SESSION_DEP,
) -> GoogleCallbackResponse:
    """Exchange Google auth code for a session JWT.

    1. Exchange the auth code for Google tokens
    2. Fetch user profile from Google
    3. Create or update the local user
    4. Issue a session JWT
    """
    try:
        tokens = await exchange_code_for_tokens(payload.code)
    except GoogleAuthError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    access_token = tokens.get("access_token")
    if not access_token:
        raise HTTPException(status_code=400, detail="No access token received from Google.")

    try:
        user_info = await get_google_user_info(access_token)
    except GoogleAuthError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc

    google_id = user_info.get("sub")
    email = user_info.get("email")
    name = user_info.get("name")

    if not google_id:
        raise HTTPException(status_code=400, detail="Could not resolve Google user ID.")

    # Create or update user in DB
    defaults: dict[str, object | None] = {"email": email, "name": name}
    user, created = await crud.get_or_create(
        session,
        User,
        external_id=google_id,
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

    # Ensure org membership
    from app.services.organizations import ensure_member_for_user

    await ensure_member_for_user(session, user)

    # Issue session JWT
    token = create_session_token(
        user_id=str(user.id),
        email=user.email,
        name=user.name,
    )

    return GoogleCallbackResponse(
        token=token,
        user=UserRead.model_validate(user),
    )


@router.post("/bootstrap", response_model=UserRead)
async def bootstrap_user(auth: AuthContext = AUTH_CONTEXT_DEP) -> UserRead:
    """Return the authenticated user profile from the session token."""
    if auth.actor_type != "user" or auth.user is None:
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED)
    return UserRead.model_validate(auth.user)

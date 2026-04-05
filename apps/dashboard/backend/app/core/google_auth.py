"""Google OAuth token verification and code exchange.

Handles the server-side of the Google OAuth flow:
1. Exchange auth code for tokens (access_token + id_token)
2. Verify and decode the Google ID token
"""

from __future__ import annotations

import os
from typing import Any

import httpx

GOOGLE_TOKEN_URL = "https://oauth2.googleapis.com/token"
GOOGLE_USERINFO_URL = "https://www.googleapis.com/oauth2/v3/userinfo"


class GoogleAuthError(Exception):
    """Raised when Google auth operations fail."""


def _get_google_config() -> tuple[str, str, str]:
    """Return (client_id, client_secret, redirect_uri) from env vars."""
    client_id = os.environ.get("GOOGLE_CLIENT_ID", "")
    client_secret = os.environ.get("GOOGLE_CLIENT_SECRET", "")
    redirect_uri = os.environ.get("GOOGLE_REDIRECT_URI", "")
    if not client_id or not client_secret:
        raise GoogleAuthError(
            "GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET must be set."
        )
    return client_id, client_secret, redirect_uri


async def exchange_code_for_tokens(code: str) -> dict[str, Any]:
    """Exchange a Google auth code for access_token and id_token."""
    client_id, client_secret, redirect_uri = _get_google_config()

    async with httpx.AsyncClient(timeout=10.0) as client:
        resp = await client.post(
            GOOGLE_TOKEN_URL,
            data={
                "code": code,
                "client_id": client_id,
                "client_secret": client_secret,
                "redirect_uri": redirect_uri,
                "grant_type": "authorization_code",
            },
        )
        if resp.status_code != 200:
            raise GoogleAuthError(
                f"Token exchange failed: {resp.status_code} {resp.text}"
            )
        return resp.json()


async def get_google_user_info(access_token: str) -> dict[str, Any]:
    """Fetch user profile from Google using the access token."""
    async with httpx.AsyncClient(timeout=10.0) as client:
        resp = await client.get(
            GOOGLE_USERINFO_URL,
            headers={"Authorization": f"Bearer {access_token}"},
        )
        if resp.status_code != 200:
            raise GoogleAuthError(
                f"Failed to fetch user info: {resp.status_code}"
            )
        return resp.json()


def build_google_auth_url(state: str | None = None) -> str:
    """Build the Google OAuth consent URL for the frontend to redirect to."""
    client_id, _, redirect_uri = _get_google_config()

    params = {
        "client_id": client_id,
        "redirect_uri": redirect_uri,
        "response_type": "code",
        "scope": "openid email profile",
        "access_type": "offline",
        "prompt": "consent",
    }
    if state:
        params["state"] = state

    query = "&".join(f"{k}={v}" for k, v in params.items())
    return f"https://accounts.google.com/o/oauth2/v2/auth?{query}"

"""Application settings and environment configuration loading.

The backend runs in exactly one of three modes, selected by the
`ENVIRONMENT` variable:

    dev         local development, everything on localhost
    staging     pre-prod, points at staging.dashboard.harrokrog.com
    production  live deployment, points at dashboard.harrokrog.com

URLs are **hardcoded** in the `PROFILES` dict below (not read from
env or .env), so switching environments is a single-variable flip in
compose / the k8s deployment. The only thing `.env` or compose
actually sets is `ENVIRONMENT=<mode>`.
"""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from typing import Self
from urllib.parse import urlparse

from pydantic import Field, model_validator
from pydantic_settings import BaseSettings, SettingsConfigDict

from app.core.rate_limit_backend import RateLimitBackend

BACKEND_ROOT = Path(__file__).resolve().parents[2]
DEFAULT_ENV_FILE = BACKEND_ROOT / ".env"


@dataclass(frozen=True)
class EnvironmentProfile:
    """Hardcoded per-environment URL/config bundle.

    Add a new key to `PROFILES` and set `ENVIRONMENT=<key>` to switch
    between them — no `.env` edits required.
    """

    base_url: str
    cors_origins: str


# Canonical profile table. Single source of truth for all env-specific
# URLs. Do NOT move these values into `.env` — the whole point of this
# module is that they're baked into the image and selected by flag.
PROFILES: dict[str, EnvironmentProfile] = {
    "dev": EnvironmentProfile(
        base_url="http://localhost:8000",
        cors_origins="http://localhost:3000",
    ),
    "staging": EnvironmentProfile(
        base_url="https://staging.dashboard.harrokrog.com",
        cors_origins="https://staging.dashboard.harrokrog.com",
    ),
    "production": EnvironmentProfile(
        base_url="https://dashboard.harrokrog.com",
        cors_origins="https://dashboard.harrokrog.com",
    ),
}
class Settings(BaseSettings):
    """Typed runtime configuration sourced from environment variables."""

    model_config = SettingsConfigDict(
        # Load `backend/.env` regardless of current working directory.
        # (Important when running uvicorn from repo root or via a process manager.)
        env_file=[DEFAULT_ENV_FILE, ".env"],
        env_file_encoding="utf-8",
        extra="ignore",
    )

    environment: str = "dev"
    database_url: str = "postgresql+psycopg://eaos:eaos@REDACTED_MINIO_HOST_IP:5432/enterprise_agent_os"

    # Google OAuth
    google_client_id: str = ""
    google_client_secret: str = ""
    google_redirect_uri: str = ""

    # Session JWT
    session_secret: str = ""
    session_expiry_hours: int = 24

    cors_origins: str = ""
    base_url: str = ""

    # Security response headers (set to blank to disable a specific header)
    security_header_x_content_type_options: str = "nosniff"
    security_header_x_frame_options: str = "DENY"
    security_header_referrer_policy: str = "strict-origin-when-cross-origin"
    security_header_permissions_policy: str = ""

    # Webhook payload size limit in bytes (default 1 MB).
    webhook_max_payload_bytes: int = 1_048_576

    # Rate limiting
    rate_limit_backend: RateLimitBackend = RateLimitBackend.MEMORY
    rate_limit_redis_url: str = ""

    # Trusted reverse-proxy IPs/CIDRs for client-IP extraction from
    # Forwarded / X-Forwarded-For headers.  Comma-separated.
    # Leave empty to always use the direct peer address.
    trusted_proxies: str = ""

    # Database lifecycle
    db_auto_migrate: bool = False

    # RQ queueing / dispatch
    rq_redis_url: str = "redis://localhost:6379/0"
    rq_queue_name: str = "default"
    rq_dispatch_throttle_seconds: float = 15.0
    rq_dispatch_max_retries: int = 3
    rq_dispatch_retry_base_seconds: float = 10.0
    rq_dispatch_retry_max_seconds: float = 120.0

    # OpenClaw gateway runtime compatibility
    gateway_min_version: str = "2026.02.9"

    # Logging
    log_level: str = "INFO"
    log_format: str = "text"
    log_use_utc: bool = False
    request_log_slow_ms: int = Field(default=1000, ge=0)
    request_log_include_health: bool = False

    @model_validator(mode="after")
    def _defaults(self) -> Self:
        # Apply the hardcoded per-environment profile. `ENVIRONMENT`
        # is the only variable that needs to change between dev /
        # staging / production — everything else falls out of the
        # `PROFILES` table above.
        profile = PROFILES.get(self.environment.strip().lower())
        if profile is None:
            raise ValueError(
                f"Unknown ENVIRONMENT={self.environment!r}. "
                f"Valid: {sorted(PROFILES)}.",
            )
        self.base_url = profile.base_url
        self.cors_origins = profile.cors_origins

        parsed_base_url = urlparse(self.base_url)
        if parsed_base_url.scheme not in {"http", "https"} or not parsed_base_url.netloc:
            raise ValueError(
                "base_url from profile must be an absolute http(s) URL "
                f"(got {self.base_url!r}).",
            )
        self.base_url = self.base_url.rstrip("/")

        # Rate-limit: fall back to rq_redis_url if using redis backend
        # with no explicit rate-limit URL. If both are blank, fail fast
        # with a clear configuration error.
        if (
            self.rate_limit_backend == RateLimitBackend.REDIS
            and not self.rate_limit_redis_url.strip()
        ):
            fallback_url = self.rq_redis_url.strip()
            if not fallback_url:
                raise ValueError(
                    "RATE_LIMIT_REDIS_URL or RQ_REDIS_URL must be set and non-empty "
                    "when RATE_LIMIT_BACKEND=redis.",
                )
            self.rate_limit_redis_url = fallback_url

        # In dev, default to applying Alembic migrations at startup to avoid
        # schema drift (e.g. missing newly-added columns).
        if "db_auto_migrate" not in self.model_fields_set and self.environment == "dev":
            self.db_auto_migrate = True
        return self


settings = Settings()

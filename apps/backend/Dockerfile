# syntax=docker/dockerfile:1
#
# Build context: repo root (not apps/backend/), because the backend's
# pyproject.toml depends on `../../packages/obsctl` as an editable
# sibling package, and Docker can't COPY anything outside the context.
#
# We mirror the repo layout inside the image (`/repo/apps/backend`
# and `/repo/packages/obsctl`) so that uv's `../../packages/obsctl`
# relative path resolves cleanly. WORKDIR is kept identical across
# the deps and runtime stages so the venv's hardcoded shebangs keep
# working after the `COPY --from=deps`.

FROM python:3.12-slim AS base

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

# System deps (keep minimal)
RUN apt-get update \
  && apt-get install -y --no-install-recommends curl ca-certificates git \
  && rm -rf /var/lib/apt/lists/*

# Install uv (https://github.com/astral-sh/uv)
RUN curl -LsSf https://astral.sh/uv/install.sh | sh
ENV PATH="/root/.local/bin:${PATH}"

# --- deps layer ---
FROM base AS deps

WORKDIR /repo/apps/backend

# obsctl sibling package, positioned so `../../packages/obsctl` from
# pyproject.toml resolves to /repo/packages/obsctl.
COPY packages/obsctl /repo/packages/obsctl

# Backend dependency metadata only — keeps this layer cache-friendly.
COPY apps/backend/pyproject.toml apps/backend/uv.lock ./

# Create venv and sync deps (including runtime)
RUN uv sync --frozen --no-dev

# --- runtime ---
FROM base AS runtime

# Same WORKDIR as the deps stage so venv shebangs stay valid.
WORKDIR /repo/apps/backend

# Create non-root user before COPY so --chown can reference it.
# Using COPY --chown avoids a slow recursive chown on overlay2 (docker/for-linux#388).
RUN groupadd --system appgroup && useradd --system --gid appgroup --create-home appuser \
  && mkdir -p /repo/apps/backend /repo/packages \
  && chown -R appuser:appgroup /repo

# Copy the virtual environment + obsctl source (editable install
# resolves to /repo/packages/obsctl at runtime, so it has to be
# present in the final image).
COPY --from=deps --chown=appuser:appgroup /repo/apps/backend/.venv /repo/apps/backend/.venv
COPY --from=deps --chown=appuser:appgroup /repo/packages/obsctl /repo/packages/obsctl
ENV PATH="/repo/apps/backend/.venv/bin:${PATH}"

# Copy app source
COPY --chown=appuser:appgroup apps/backend/migrations ./migrations
COPY --chown=appuser:appgroup apps/backend/alembic.ini ./alembic.ini
COPY --chown=appuser:appgroup apps/backend/app ./app
COPY --chown=appuser:appgroup apps/backend/templates ./templates
COPY --chown=appuser:appgroup apps/backend/scripts ./scripts

# Bake the .env into the image — services are self-contained, no runtime env injection.
COPY --chown=appuser:appgroup apps/backend/.env ./.env

USER appuser

# Default API port
EXPOSE 8000

# Run the API
CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]

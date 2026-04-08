# syntax=docker/dockerfile:1.7
#
# ZeroClaw agent runtime image — Rust binary compiled from this
# workspace's source. Matches the stripped-down post-refactor layout
# (no crates/robot-kit, no benches, no channel-lark/whatsapp-web
# features) and is what `k8s_manager.py::DEFAULT_IMAGE` now pulls.
#
# Build context MUST be this directory (`packages/zeroclaw-core/`),
# not the repo root — the COPY paths are all relative to here.

# ──────────────────────────────────────────────────────────────
# Stage 0: Web dashboard build
# `rust-embed` at src/gateway/static_files.rs bakes `web/dist/` into
# the binary at compile time. The directory is gitignored, so CI
# must rebuild it from `web/` before the Rust build starts.
# ──────────────────────────────────────────────────────────────
FROM node:22-alpine AS web-builder
WORKDIR /web
COPY web/package.json web/package-lock.json* ./
RUN npm ci --ignore-scripts 2>/dev/null || npm install --ignore-scripts
COPY web/ ./
RUN npm run build

# ──────────────────────────────────────────────────────────────
# Stage 1: Rust build
# ──────────────────────────────────────────────────────────────
FROM rust:1.94-slim-bookworm AS builder

WORKDIR /app

RUN apt-get update \
  && apt-get install -y --no-install-recommends \
     build-essential \
     pkg-config \
     ca-certificates \
     git \
  && rm -rf /var/lib/apt/lists/*

# Copy the Rust workspace. `web/dist/` comes from the web-builder
# stage so rust-embed's compile-time file inclusion resolves.
COPY Cargo.toml Cargo.lock build.rs ./
COPY src ./src
COPY tool_descriptions ./tool_descriptions
COPY skills ./skills
COPY --from=web-builder /web/dist ./web/dist

# Release build with the default feature set (observability-prometheus,
# skill-creation). The workspace has exactly one binary target.
RUN --mount=type=cache,target=/usr/local/cargo/registry \
    --mount=type=cache,target=/app/target,sharing=locked \
    cargo build --release --locked --bin zeroclaw \
 && cp target/release/zeroclaw /usr/local/bin/zeroclaw

# ──────────────────────────────────────────────────────────────
# Stage 2: Runtime
# ──────────────────────────────────────────────────────────────
FROM debian:bookworm-slim AS runtime

RUN apt-get update \
  && apt-get install -y --no-install-recommends \
     ca-certificates \
     curl \
     bash \
     git \
  && rm -rf /var/lib/apt/lists/* \
  && groupadd --system zeroclaw \
  && useradd --system --gid zeroclaw --create-home zeroclaw \
  && mkdir -p /zeroclaw-data /vault-workspace \
  && chown -R zeroclaw:zeroclaw /zeroclaw-data /vault-workspace

COPY --from=builder --chown=zeroclaw:zeroclaw /usr/local/bin/zeroclaw /usr/local/bin/zeroclaw

USER zeroclaw
WORKDIR /home/zeroclaw

EXPOSE 42617

CMD ["zeroclaw", "daemon"]

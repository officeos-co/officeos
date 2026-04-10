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

# Copy the Rust workspace.
COPY Cargo.toml Cargo.lock ./
COPY src ./src
COPY tool_descriptions ./tool_descriptions

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

# Bake gateway defaults so the deployer doesn't need per-pod env vars
# for operational settings. Only ZEROCLAW_AGENT_ID varies per pod.
RUN mkdir -p /home/zeroclaw/.zeroclaw \
  && printf '[gateway]\nport = 42617\nhost = "0.0.0.0"\nrequire_pairing = false\nallow_public_bind = true\n' \
     > /home/zeroclaw/.zeroclaw/config.toml \
  && chown -R zeroclaw:zeroclaw /home/zeroclaw/.zeroclaw

USER zeroclaw
WORKDIR /home/zeroclaw

EXPOSE 42617

CMD ["zeroclaw", "daemon"]

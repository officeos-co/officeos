# syntax=docker/dockerfile:1.7
#
# zeroclaw-agent runtime image — 1.0 rewrite of the pod binary.
#
# Build context MUST be this directory (`packages/zeroclaw-core-2/`).

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
  && rm -rf /var/lib/apt/lists/*

COPY Cargo.toml Cargo.lock* ./
COPY src ./src

RUN --mount=type=cache,target=/usr/local/cargo/registry \
    --mount=type=cache,target=/app/target,sharing=locked \
    cargo build --release --bin zeroclaw-agent \
 && cp target/release/zeroclaw-agent /usr/local/bin/zeroclaw-agent

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
     ripgrep \
  && rm -rf /var/lib/apt/lists/* \
  && groupadd --system zeroclaw \
  && useradd --system --gid zeroclaw --create-home zeroclaw \
  && mkdir -p /zeroclaw-data /zeroclaw-data/memory \
  && chown -R zeroclaw:zeroclaw /zeroclaw-data

COPY --from=builder --chown=zeroclaw:zeroclaw /usr/local/bin/zeroclaw-agent /usr/local/bin/zeroclaw-agent

USER zeroclaw
WORKDIR /home/zeroclaw

EXPOSE 42617

CMD ["zeroclaw-agent"]

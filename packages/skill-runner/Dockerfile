FROM node:22-slim AS builder

WORKDIR /app

# Copy SDK and skill sources
COPY packages/skill-sdk/ packages/skill-sdk/
COPY packages/skills/ packages/skills/
COPY packages/skill-runner/ packages/skill-runner/

# Install and build SDK first (local dependency)
RUN cd packages/skill-sdk && npm ci && npm run build

# Install all skill dependencies (auto-discovers every skill with a package.json)
RUN for dir in packages/skills/*/; do \
      [ -f "$dir/package.json" ] && (cd "$dir" && npm install) || true; \
    done

# Build runner (bundles client + all skills)
RUN cd packages/skill-runner && npm ci && npm run build

# --- Production stage ---
FROM node:22-slim

RUN apt-get update && apt-get install -y unzip && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=builder /app/packages/skill-runner/dist/ ./dist/
# esbuild binary needed at runtime for building synced skills
COPY --from=builder /app/packages/skill-runner/node_modules/esbuild/ ./node_modules/esbuild/
COPY --from=builder /app/packages/skill-runner/node_modules/@esbuild/ ./node_modules/@esbuild/
# SDK source needed for building skills that import @harro/skill-sdk
COPY --from=builder /app/packages/skill-sdk/ ./skill-sdk/

ENV PLATFORM_URL=""
ENV REGISTRATION_TOKEN=""

CMD ["node", "dist/client.js"]

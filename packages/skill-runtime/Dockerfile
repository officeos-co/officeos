FROM node:22-slim AS builder

WORKDIR /app

# Copy SDK and skill sources
COPY packages/skill-sdk/ packages/skill-sdk/
COPY packages/skills/ packages/skills/
COPY packages/skill-runtime/ packages/skill-runtime/

# Install and build SDK first (local dependency)
RUN cd packages/skill-sdk && npm ci && npm run build

# Install all skill dependencies (auto-discovers every skill with a package.json)
RUN for dir in packages/skills/*/; do \
      [ -f "$dir/package.json" ] && (cd "$dir" && npm install) || true; \
    done

# Build runtime (bundles server + all skills)
RUN cd packages/skill-runtime && npm ci && npm run build

# --- Production stage ---
FROM node:22-slim

WORKDIR /app

# Install playwright-core and Chromium (browser skill needs a real browser)
RUN npm init -y && npm install playwright-core && \
    npx playwright-core install --with-deps chromium && \
    rm -rf /root/.cache/ms-playwright/firefox-* /root/.cache/ms-playwright/webkit-*

COPY --from=builder /app/packages/skill-runtime/dist/ ./dist/

ENV PORT=3001
EXPOSE 3001

CMD ["node", "dist/server.js"]

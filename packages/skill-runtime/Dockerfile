FROM node:22-slim AS builder

WORKDIR /app

# Copy SDK and skill sources
COPY packages/skill-sdk/ packages/skill-sdk/
COPY packages/skills/ packages/skills/
COPY packages/skill-runtime/ packages/skill-runtime/

# Install and build SDK first (local dependency)
RUN cd packages/skill-sdk && npm ci && npm run build

# Install skills dependencies
RUN cd packages/skills/notion && npm install
RUN cd packages/skills/github && npm install
RUN cd packages/skills/google && npm install
RUN cd packages/skills/browser && npm install

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

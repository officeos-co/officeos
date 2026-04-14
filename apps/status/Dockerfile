# syntax=docker/dockerfile:1.7
FROM oven/bun:1 AS deps
WORKDIR /app
COPY apps/status/package.json apps/status/bun.lock* ./
RUN bun install

FROM oven/bun:1 AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY apps/status/. ./
ENV NEXT_TELEMETRY_DISABLED=1
RUN bun run build

FROM oven/bun:1-slim AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
EXPOSE 3000
CMD ["bun", "server.js"]

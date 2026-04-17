# syntax=docker/dockerfile:1.7
FROM oven/bun:1 AS deps
WORKDIR /app
COPY apps/dashboard-2/package.json apps/dashboard-2/bun.lock* ./
RUN bun install

FROM oven/bun:1 AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY apps/dashboard-2/. ./
ENV NEXT_TELEMETRY_DISABLED=1
ENV NEXT_PUBLIC_API_URL=https://api.officeos.co
RUN bun run build

FROM oven/bun:1-slim AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000
ENV HOSTNAME=0.0.0.0
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static
COPY --from=build /app/public ./public
EXPOSE 3000
CMD ["bun", "server.js"]
